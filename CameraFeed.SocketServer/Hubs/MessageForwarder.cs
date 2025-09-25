//using Microsoft.AspNetCore.SignalR;

//namespace CameraFeed.SocketServer.Hubs;

//public interface IMessageForwarder
//{
//    Task Apply(byte[] message, string groupName);
//}

///// <summary>
///// Provides functionality to forward messages to a specific group of clients connected to a SignalR hub.
///// </summary>
///// <remarks>This class is designed to work with a SignalR hub context to send messages to a group of clients.</remarks>
//public class MessageForwarder(IHubContext<FrontendClientHub> context) : IMessageForwarder
//{
//    private readonly IHubContext<FrontendClientHub>? _forwarderHubContext = context;

//    public async Task Apply(byte[] message, string groupName)
//    {
//        if (_forwarderHubContext != null)
//            await _forwarderHubContext.Clients.Group(groupName).SendAsync("ReceiveForwardedMessage", message);
//    }
//}

//public class MessageForwarderDecorator(IMessageForwarder forwarder) : IMessageForwarder
//{
//    private readonly IMessageForwarder _forwarder = forwarder;

//    public async Task Apply(byte[] message, string groupName)
//    {
//        // Add any additional behavior here (e.g., logging, validation, etc.)
//        await _forwarder.Apply(message, groupName);
//    }
//}
