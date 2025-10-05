using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Clients.SignalR;

public interface ICameraSignalRclient
{
    Task CreateConnectionsAsync(string camName, CancellationToken token);
    Task StartAndJoinAsync(HubConnection connection, string hubType, string camName, CancellationToken token);
    Task SendFrame(byte[] frameData, string camName, CancellationToken token);
    Task StopAndDisposeConnectionsAsync(string camName, CancellationToken token);
}

public class CameraSignalRClient(IHubConnectionFactory hubConnectionFactory, ILogger<CameraSignalRClient> logger) : ICameraSignalRclient
{
    private readonly IHubConnectionFactory _hubConnectionFactory = hubConnectionFactory;
    private readonly ILogger<CameraSignalRClient> _logger = logger;

    private readonly ConcurrentDictionary<string, HubConnection> _remoteHubConnections = new();
    private readonly ConcurrentDictionary<string, HubConnection> _localHubConnections = new();
    private readonly ConcurrentDictionary<string, bool> _remoteStreamingEnabled = new();

    public async Task CreateConnectionsAsync(string camName, CancellationToken token)
    {
        // Dispose previous connections if they exist
        await StopAndDisposeConnectionsAsync(camName, token);

        var remoteHubConnection = _hubConnectionFactory.CreateRemoteConnection(
            onStreamingEnabled: () =>
            {
                _remoteStreamingEnabled[camName] = true;
                _logger.LogInformation("Remote streaming enabled for {camName}", camName);
            },
            onStreamingDisabled: () =>
            {
                _remoteStreamingEnabled[camName] = false;
                _logger.LogInformation("Remote streaming disabled for {camName}", camName);
            });

        var localHubConnection = _hubConnectionFactory.CreateLocalConnection(
            onStreamingEnabled: () =>
            {
                _logger.LogInformation("Local streaming enabled for {camName}", camName);
            },
            onStreamingDisabled: () =>
            {
                _logger.LogInformation("Local streaming disabled for {camName}", camName);
            });

        _remoteHubConnections[camName] = remoteHubConnection;
        _localHubConnections[camName] = localHubConnection;
        _remoteStreamingEnabled[camName] = false;

        await Task.WhenAll([
            StartAndJoinAsync(localHubConnection, "Local", camName, token),
            StartAndJoinAsync(remoteHubConnection, "Remote", camName, token)
        ]);
    }

    public async Task StartAndJoinAsync(HubConnection connection, string hubType, string camName, CancellationToken token)
    {
        try
        {
            if (connection.State == HubConnectionState.Disconnected)
            {
                await connection.StartAsync(token);
                await connection.InvokeAsync("JoinGroup", camName, token);
                _logger.LogInformation("{HubType} HubConnection initialized for {camName}", hubType, camName);
            }
            else
            {
                _logger.LogWarning("{HubType} HubConnection for {camName} is not in Disconnected state (current: {State})", hubType, camName, connection.State);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{HubType} HubConnection initialization cancelled for {camName}", hubType, camName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error initializing {HubType} HubConnection for {camName}: {Message}", hubType, camName, ex.Message);
        }
    }

    public virtual async Task SendFrame(byte[] frameData, string camName, CancellationToken token)
    {
        try
        {
            // Via SignalR Cloud Hub
            if (_remoteStreamingEnabled.TryGetValue(camName, out var remoteEnabled) && remoteEnabled)
            {
                if (_remoteHubConnections.TryGetValue(camName, out var remoteConn) && remoteConn.State == HubConnectionState.Connected)
                    await remoteConn.InvokeAsync("SendMessage", frameData, camName, token);
            }

            // Via SignalR Local Hub
            if (_localHubConnections.TryGetValue(camName, out var localConn) && localConn.State == HubConnectionState.Connected)
                await localConn.InvokeAsync("SendMessage", frameData, camName, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send frame to hub for camera {camName}: {Message}", camName, ex.Message);
        }
    }

    public async Task StopAndDisposeConnectionsAsync(string camName, CancellationToken token)
    {
        if (_localHubConnections.TryRemove(camName, out var localConn))
        {
            try
            {
                await localConn.InvokeAsync("LeaveGroup", camName, token);
                await localConn.StopAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Local HubConnection for {camName}: {message}", camName, ex.Message);
            }
            finally
            {
                await localConn.DisposeAsync();
            }
        }

        if (_remoteHubConnections.TryRemove(camName, out var remoteConn))
        {
            try
            {
                await remoteConn.InvokeAsync("LeaveGroup", camName, token);
                await remoteConn.StopAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Remote HubConnection for {camName}: {message}", camName, ex.Message);
            }
            finally
            {
                await remoteConn.DisposeAsync();
            }
        }

        _remoteStreamingEnabled.TryRemove(camName, out _);
    }
}
