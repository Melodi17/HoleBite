using System.Net.Sockets;

namespace HoleBite;

public class SmartServer : Server
{
    public SmartServer(int port) : base(port)
    {
    }

    protected override void MessageRecieved(TcpClient client, string? identity, string message)
    {
        base.MessageRecieved(client, identity, message);
    }

    protected override void IdentityReceived(TcpClient client, string name)
    {
        base.IdentityReceived(client, name);

        const string cyan = "\x1b[36m";
        const string reset = "\x1b[0m";
        Send(client, "message", $"{cyan}Welcome, {name}!{reset}");
        Broadcast("message", $"{cyan}{name} has joined the server!{reset}");
    }
}