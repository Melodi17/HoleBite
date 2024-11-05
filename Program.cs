using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HoleBite;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: holebite.exe <server>");
            return;
        }

        string server = args[0];
        int port = 13577;

        if (server == "server")
            RunServer(port);
        else
            RunClient(args, server, port);
    }

    private static void RunServer(int port)
    {
        Server s = new(port);
        s.Start();
    }

    private static void RunClient(string[] args, string server, int port)
    {
        List<string> messages = new();
        int maxMessages = Console.WindowHeight - 2;
        int inputRow = Console.WindowHeight - 1;
        int width = Console.WindowWidth - 1;

        object consoleLockObject = new();
        
        Client c = new(server, port, args.Length > 1 ? args[1] : Environment.UserName);
        c.MessageReceived += (name, message) =>
        {
            messages.Add($"{name}: {message}");
            
            StringBuilder screen = new();
            for (int i = Math.Max(0, messages.Count - maxMessages); i < messages.Count; i++)
                screen.AppendLine(messages[i] + new string(' ', width - messages[i].Length));

            lock (consoleLockObject)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(screen);
            }
        };
        new Thread(c.Start).Start();

        while (true)
        {
            lock (consoleLockObject)
            {
                Console.SetCursorPosition(0, inputRow);
                Console.Write(new string(' ', width));
                Console.SetCursorPosition(0, inputRow);
                Console.Write($" : ");
            }
            string message = Console.ReadLine()!;
            c.SendMessage(message);
        }
    }
}

public class Client
{
    private TcpClient client;
    private NetworkStream stream;
    public string name;

    public event Action<string, string> MessageReceived;

    public Client(string server, int port, string name)
    {
        client = new TcpClient(server, port);
        stream = client.GetStream();
        this.name = name;
    }

    public void SendMessage(string message)
    {
        Send("message", message);
    }

    public void Start()
    {
        Send("i'm", name);

        while (true)
        {
            try
            {
                string message = Receive();
                string[] parts = message.Split(' ', 2);

                if (parts[0] == "message")
                    MessageReceived?.Invoke(parts[1], parts[2]);

                if (parts[0] == "rich")
                {
                    Console.ForegroundColor = Enum.Parse<ConsoleColor>(parts[1], true);
                }

                if (parts[0] == "bye")
                    break;   
            }
            catch (Exception e) { }
        }
    }

    private void Send(string code, string? message = null)
    {
        byte[] data = Encoding.ASCII.GetBytes(code + (message == null ? "" : " " + message));
        byte[] size = BitConverter.GetBytes(data.Length);
        stream.Write(size, 0, size.Length);
        stream.Write(data, 0, data.Length);
    }

    private string Receive()
    {
        byte[] size = new byte[4];
        stream.Read(size, 0, size.Length);
        byte[] data = new byte[BitConverter.ToInt32(size, 0)];

        stream.Read(data, 0, data.Length);
        return Encoding.ASCII.GetString(data);
    }

    public void Close()
    {
        stream.Close();
        client.Close();
    }
}

public class Server
{
    protected TcpListener listener;
    protected List<TcpClient> clients = new();

    public Server(int port)
    {
        listener = new(IPAddress.Any, port);
    }

    public virtual void Start()
    {
        listener.Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            new Thread(() =>
            {
                try
                {
                    HandleClient(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }).Start();
            ClientAdded(client);
            clients.Add(client);
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
            }
            else if (parts[0] == "message")
            {
                this.MessageRecieved(name, parts[1]);
            }
            else if (parts[0] == "bye")
            {
                Console.WriteLine(parts[1] + " disconnected");
                break;
            }

            else throw new("Invalid message code");
        }
    }

    protected virtual void MessageRecieved(string? name, string message)
    {
        if (name == null)
            throw new("Client sent message before identifying");
        Console.WriteLine($"[{name}] {message}");
        this.Broadcast("message", $"[{name}] {message}");
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
    }

    protected void Broadcast(string code, string? message = null)
    {
        foreach (TcpClient client in clients)
            Send(client, code, message);
    }

    public void Stop()
    {
        foreach (TcpClient client in clients)
            client.Close();

        listener.Stop();
    }
}

public class ServerWithAccounts : Server
{
    public ServerWithAccounts(int port) : base(port)
    {
    }

    protected override void MessageRecieved(string? name, string message)
    {
        base.MessageRecieved(name, message);
    }

    protected override void ClientAdded(TcpClient client)
    {
        base.ClientAdded(client);
    }
}