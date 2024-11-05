using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HoleBite;

public abstract class Server
{
    private readonly TcpListener _listener;
    protected readonly List<TcpClient> Clients = new();
    protected readonly int Port = 0;

    protected Server(int port)
    {
        this._listener = new(IPAddress.Any, port);
        this.Port = port;
    }
    
    public virtual void Start()
    {
        this.StartListening();
    }
    
    protected void StartListening()
    {
        this._listener.Start();

        while (true)
        {
            TcpClient client = this._listener.AcceptTcpClient();
            new Thread(() =>
            {
                try
                {
                    this.HandleClient(client);
                }
                // catch any connection errors
                catch (Exception e) when (e is SocketException or IOException)
                {
                    client.Close();
                    this.Clients.Remove(client);
                }
            }).Start();
            this.Clients.Add(client);
        }
    }

    private void HandleClient(TcpClient client)
    {
        string? identity = null;
        
        try
        {
            this.ClientConnected(client);

            NetworkStream stream = client.GetStream();

            while (true)
            {
                string[] parts = Read(stream).Split(' ', 2);

                if (parts[0] == "i'm")
                {
                    identity = parts[1];
                    IdentityReceived(client, identity);
                }
                else if (parts[0] == "message")
                {
                    this.MessageReceived(client, identity, parts[1]);
                }

                else throw new("Invalid message code");
            }
        }
        catch (Exception e) when (e is SocketException or IOException or InvalidOperationException)
        {
            this.Clients.Remove(client);
            ClientDisconnected(client, identity);
            client.Close();
        }
        catch (Exception e)
        {
            ClientHandleError(client, identity, e);
        }
    }

    private static string Read(NetworkStream stream)
    {
        byte[] size = new byte[4];
        stream.Read(size, 0, size.Length);
        byte[] data = new byte[BitConverter.ToInt32(size, 0)];

        stream.Read(data, 0, data.Length);
        string message = Encoding.ASCII.GetString(data);
        return message;
    }

    protected abstract void ClientConnected(TcpClient client);
    protected abstract void ClientDisconnected(TcpClient client, string? identity);
    protected abstract void IdentityReceived(TcpClient client, string identity);
    protected abstract void MessageReceived(TcpClient client, string? identity, string message);
    protected abstract void ClientHandleError(TcpClient client, string? identity, Exception exception);

    protected void Send(TcpClient client, string code, string? message = null)
    {
        byte[] data = Encoding.ASCII.GetBytes(code + (message == null ? "" : " " + message));
        byte[] size = BitConverter.GetBytes(data.Length);

        client.GetStream().Write(size, 0, size.Length);
        client.GetStream().Write(data, 0, data.Length);
    }

    protected void Broadcast(string code, string? message = null, TcpClient? except = null)
    {
        foreach (TcpClient client in this.Clients
                     .Where(c => c != except))
        {
            try
            {
                this.Send(client, code, message);
            }
            catch (Exception e) when (e is SocketException or IOException or InvalidOperationException) { }
        }
            
    }

    public void Stop()
    {
        foreach (TcpClient client in this.Clients)
            client.Close();

        this._listener.Stop();
    }
}