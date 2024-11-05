using System.Net.Sockets;

namespace HoleBite;

public class ServerWithAccounts : Server
{
    public ServerWithAccounts(int port) : base(port)
    {
    }

    protected override void MessageRecieved(TcpClient client, string? name, string message)
    {
        base.MessageRecieved(client, name, message);
    }

    protected override void ClientAdded(TcpClient client)
    {
        base.ClientAdded(client);
    }
}