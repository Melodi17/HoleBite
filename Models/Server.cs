using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HoleBite;

public class Server
{
    protected TcpListener listener;
    protected List<TcpClient> clients = new();

    public Server(int port)
    {
        this.listener = new(IPAddress.Any, port);
    }

    public virtual void Start()
    {
        this.listener.Start();

        while (true)
        {
            TcpClient client = this.listener.AcceptTcpClient();
            new Thread(() =>
            {
                try
                {
                    this.HandleClient(client);
                }
                // catch any connection errors
                catch (SocketException)
                {
                    client.Close();
                    this.clients.Remove(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }).Start();
            this.ClientAdded(client);
            this.clients.Add(client);
        }
    }

    protected virtual void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        string? name = null;

        while (true)
        {
            byte[] size = new byte[4];
            stream.Read(size, 0, size.Length);
            byte[] data = new byte[BitConverter.ToInt32(size, 0)];

            stream.Read(data, 0, data.Length);
            string message = Encoding.ASCII.GetString(data);
            string[] parts = message.Split(' ', 2);

            if (parts[0] == "i'm")
            {
                name = parts[1];
                Console.WriteLine(name + " connected");
                IdentityReceived(client, name);
            }
            else if (parts[0] == "message")
            {
                this.MessageRecieved(client, name, parts[1]);
            }

            else throw new("Invalid message code");
        }
    }

    protected virtual void IdentityReceived(TcpClient client, string name)
    {
        
    }

    protected virtual void MessageRecieved(TcpClient client, string? identity, string message)
    {
        if (identity == null)
            throw new("Client sent message before identifying");
        Console.WriteLine($"[{identity}] {message}");
        
        this.Broadcast("message", $"[{identity}] {message}");
    }
    
    protected virtual void ClientAdded(TcpClient client)
    {
        Console.WriteLine("Client connected");
    }

    protected void Send(TcpClient client, string code, string? message = null)
    {
        byte[] data = Encoding.ASCII.GetBytes(code + (message == null ? "" : " " + message));
        byte[] size = BitConverter.GetBytes(data.Length);

        client.GetStream().Write(size, 0, size.Length);
        client.GetStream().Write(data, 0, data.Length);
    }

    protected void Broadcast(string code, string? message = null, TcpClient? except = null)
    {
        foreach (TcpClient client in this.clients
                     .Where(c => c != except))
        {
            try
            {
                this.Send(client, code, message);
            }
            catch (SocketException)
            {
                client.Close();
                this.clients.Remove(client);
            }
        }
            
    }

    public void Stop()
    {
        foreach (TcpClient client in this.clients)
            client.Close();

        this.listener.Stop();
    }
}