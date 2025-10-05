using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CameraFeed.Processor.Clients.SignalR;

public interface ICameraSignalRclient
{
    Task CreateConnectionsAsync(string camName, CancellationToken token);
    Task SendFrameAsync(byte[] frameData, string camName, CancellationToken token);
    Task StopAndDisposeConnectionsAsync(string camName, CancellationToken token);
}

public class CameraSignalRClient(IHubConnectionFactory hubConnectionFactory, ILogger<CameraSignalRClient> logger) : ICameraSignalRclient
{
    private readonly IHubConnectionFactory _hubConnectionFactory = hubConnectionFactory;
    private readonly ILogger<CameraSignalRClient> _logger = logger;

    //The registry (_actors) is thread-safe for adding/removing actors, but the actor for each camera ensures that all camera-specific operations are handled sequentially,
    //so we don’t have concurrent access to a single camera’s state. This is a key benefit of the actor model for lifecycle management.
    private readonly ConcurrentDictionary<string, IMessageActor> _actors = new();

    public async Task CreateConnectionsAsync(string camName, CancellationToken token)
    {
        var actor = _actors.GetOrAdd(camName, name => new MessageActor(name, _hubConnectionFactory, _logger));
        await actor.PostAsync(new ChannelMessage.CreateConnections(token));
    }

    public async Task SendFrameAsync(byte[] frameData, string camName, CancellationToken token)
    {
        if (!_actors.TryGetValue(camName, out var actor)) return;
        await actor.PostAsync(new ChannelMessage.SendFrame(frameData, token));

    }

    public async Task StopAndDisposeConnectionsAsync(string camName, CancellationToken token)
    {
        if (!_actors.TryRemove(camName, out var actor)) return;
        await actor.PostAsync(new ChannelMessage.StopAndDispose(token));
        actor.Complete();
    }

    #region Actor and message definitions
    //Note: we need an Actor or ConcurrentDictionary to ensure that each camera’s connections and state are managed safely and correctly,
    //even when multiple parts of the application interact with them at the same time (CameraSignalRClient is injected as singleton).
    //This is essential for robust lifecycle management in a concurrent system.

    private interface IMessageActor
    {
        Task PostAsync(ChannelMessage message);
        void Complete();
    }

    /// <summary>
    /// Represents an actor responsible for managing camera-related operations, including establishing SignalR
    /// connections, handling messages, and streaming frames to remote and local hubs.
    /// </summary>
    /// <remarks>The <see cref="MessageActor"/> class processes messages asynchronously using an internal
    /// mailbox. It manages SignalR connections to both remote and local hubs, enabling streaming and communication for
    /// a specific camera. The actor ensures proper lifecycle management of connections and handles operations such as
    /// starting, stopping, and sending frames. <para> This class is designed to operate in an asynchronous environment
    /// and should be used with proper cancellation tokens to ensure graceful shutdowns. </para></remarks>
    private sealed class MessageActor : IMessageActor
    {
        private readonly Channel<ChannelMessage> _messageChannel = Channel.CreateUnbounded<ChannelMessage>();
        private readonly string _camName;
        private readonly IHubConnectionFactory _hubConnectionFactory;
        private readonly ILogger<CameraSignalRClient> _logger;
        private HubConnection? _remoteHubConnection;
        private HubConnection? _localHubConnection;
        private bool _remoteStreamingEnabled;

        public MessageActor(string camName, IHubConnectionFactory hubConnectionFactory, ILogger<CameraSignalRClient> logger)
        {
            _camName = camName;
            _hubConnectionFactory = hubConnectionFactory;
            _logger = logger;
            _ = RunAsync();
        }

        public async Task PostAsync(ChannelMessage message)
        {
            await _messageChannel.Writer.WriteAsync(message);
        }

        public void Complete() => _messageChannel.Writer.Complete();

        private async Task RunAsync()
        {
            await foreach (var msg in _messageChannel.Reader.ReadAllAsync())
            {
                switch (msg)
                {
                    case ChannelMessage.CreateConnections create:
                        await HandleCreateConnectionsAsync(create.Token);
                        break;
                    case ChannelMessage.SendFrame send:
                        await HandleSendFrameAsync(send.FrameData, send.Token);
                        break;
                    case ChannelMessage.StopAndDispose stop:
                        await HandleStopAndDisposeAsync(stop.Token);
                        break;
                }
            }
        }

        private async Task HandleCreateConnectionsAsync(CancellationToken token)
        {
            await HandleStopAndDisposeAsync(token);

            _remoteHubConnection = _hubConnectionFactory.CreateRemoteConnection(
                onStreamingEnabled: () =>
                {
                    _remoteStreamingEnabled = true;
                    _logger.LogInformation("Remote streaming enabled for {camName}", _camName);
                },
                onStreamingDisabled: () =>
                {
                    _remoteStreamingEnabled = false;
                    _logger.LogInformation("Remote streaming disabled for {camName}", _camName);
                });

            _localHubConnection = _hubConnectionFactory.CreateLocalConnection(
                onStreamingEnabled: () =>
                {
                    _logger.LogInformation("Local streaming enabled for {camName}", _camName);
                },
                onStreamingDisabled: () =>
                {
                    _logger.LogInformation("Local streaming disabled for {camName}", _camName);
                });

            _remoteStreamingEnabled = false;

            await Task.WhenAll(
                StartAndJoinAsync(_localHubConnection, "Local", token),
                StartAndJoinAsync(_remoteHubConnection, "Remote", token)
            );
        }

        private async Task StartAndJoinAsync(HubConnection connection, string hubType, CancellationToken token)
        {
            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                {
                    await connection.StartAsync(token);
                    await connection.InvokeAsync("JoinGroup", _camName, token);
                    _logger.LogInformation("{HubType} HubConnection initialized for {camName}", hubType, _camName);
                }
                else
                {
                    _logger.LogWarning("{HubType} HubConnection for {camName} is not in Disconnected state (current: {State})", hubType, _camName, connection.State);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{HubType} HubConnection initialization cancelled for {camName}", hubType, _camName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error initializing {HubType} HubConnection for {camName}: {Message}", hubType, _camName, ex.Message);
            }
        }

        private async Task HandleSendFrameAsync(byte[] frameData, CancellationToken token)
        {
            try
            {
                if (_remoteStreamingEnabled && _remoteHubConnection?.State == HubConnectionState.Connected)
                {
                    await _remoteHubConnection.InvokeAsync("SendMessage", frameData, _camName, token);
                }

                if (_localHubConnection?.State == HubConnectionState.Connected)
                {
                    await _localHubConnection.InvokeAsync("SendMessage", frameData, _camName, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send frame to hub for camera {camName}: {Message}", _camName, ex.Message);
            }
        }

        private async Task HandleStopAndDisposeAsync(CancellationToken token)
        {
            if (_localHubConnection != null)
            {
                try
                {
                    await _localHubConnection.InvokeAsync("LeaveGroup", _camName, token);
                    await _localHubConnection.StopAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping Local HubConnection for {camName}: {message}", _camName, ex.Message);
                }
                finally
                {
                    await _localHubConnection.DisposeAsync();
                    _localHubConnection = null;
                }
            }

            if (_remoteHubConnection != null)
            {
                try
                {
                    await _remoteHubConnection.InvokeAsync("LeaveGroup", _camName, token);
                    await _remoteHubConnection.StopAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping Remote HubConnection for {camName}: {message}", _camName, ex.Message);
                }
                finally
                {
                    await _remoteHubConnection.DisposeAsync();
                    _remoteHubConnection = null;
                }
            }

            _remoteStreamingEnabled = false;
        }
    }

    private abstract record ChannelMessage
    {
        public sealed record CreateConnections(CancellationToken Token) : ChannelMessage;
        public sealed record SendFrame(byte[] FrameData, CancellationToken Token) : ChannelMessage;
        public sealed record StopAndDispose(CancellationToken Token) : ChannelMessage;
    }
    #endregion
}
