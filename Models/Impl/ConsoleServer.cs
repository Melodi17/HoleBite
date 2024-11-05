using System.Net.Sockets;

namespace HoleBite;

public class ConsoleServer : Server
{
    public ConsoleServer(int port) : base(port)
    {
    }

    public override void Start()
    {
        Log("server", $"starting server on port {this.Port}");
        
        base.Start();
    }

    protected override void ClientConnected(TcpClient client)
    {
        Log("connect", $"{ShowClient(client, null)} connected to the server");

        this.Send(client, "message", ColorHelpers.Color("Loading identity...", ConsoleColor.Yellow));
    }

    protected override void ClientDisconnected(TcpClient client, string? identity)
    {
        Log("disconnect", $"{ShowClient(client, identity)} disconnected from the server");
        
        if (identity != null)
            this.Broadcast("message", ColorHelpers.Color($"{identity}", ConsoleColor.Cyan) + " has left the chat");
    }

    protected override void IdentityReceived(TcpClient client, string identity)
    {
        Log("identity", $"{ShowClient(client, null)} is now known as {identity}");
        
        this.Send(client, "message", ColorHelpers.Color($"Welcome {identity}!", ConsoleColor.Cyan));
        this.Broadcast("message", ColorHelpers.Color($"{identity}", ConsoleColor.Green) + " has joined the chat");
    }

    protected override void MessageReceived(TcpClient client, string? identity, string message)
    {
        if (identity == null)
            throw new("Client sent message before identifying");
        
        if (message.Trim().Length == 0)
            return;

        Log("message", $"[{ShowClient(client, identity)}] {message}");
        this.Broadcast("message", ColorHelpers.Color($"[{identity}]", ConsoleColor.Cyan) + " " + message);
    }

    protected override void ClientHandleError(TcpClient client, string? identity, Exception exception)
    {
        Log("error", $"{ShowClient(client, identity)} threw an exception: {exception.Message}");
    }

    private string ShowClient(TcpClient client, string? identity)
    {
        return (identity == null
            ? client.Client.RemoteEndPoint.ToString()
            : identity) ?? string.Empty;
    }
    
    private void Log(string source, string message)
    {
        Console.WriteLine($"{source.PadRight(10)} | {message}");
    }
}