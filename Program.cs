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

        Client client = new(server, port);
        Protocol protocol = new(client, Environment.UserName);

        protocol.OnMessage += message => { Console.WriteLine($"{protocol.OtherIdentity}: {message}"); };
        protocol.OnMessageUnauthorized += message => { Console.WriteLine($"<unauthorized>: {message}"); };

        protocol.OnHandshakeEstablished += () =>
        {
            Console.WriteLine("Handshake established!");
        };

        protocol.OnTimeout += () => { Console.WriteLine("Timeout!"); };

        protocol.OnIdentify += identity => { Console.WriteLine($"Other identity: {identity}"); };
        
        protocol.StartListeningAndConnect();
        
        while (true)
        {
            string message = Console.ReadLine();
            protocol.SendMessage(message);
        }
    }
}

public class Protocol
{
    public Client Client { get; private set; }
    public bool HandshakeEstablished { get; private set; }
    public string Identity { get; set; }
    public string? OtherIdentity { get; private set; }

    public event Action<string> OnMessage;
    public event Action<string> OnMessageUnauthorized;
    public event Action OnHandshakeEstablished;
    public event Action OnTimeout;
    public event Action<string> OnIdentify;

    public int Timeout { get; set; } = 5000;

    public Protocol(Client client, string identity)
    {
        Client = client;
        Identity = identity;
    }

    public void StartListeningAndConnect()
    {
        new Thread(() =>
        {
            while (true)
            {
                string message = Client.Receive();

                if (message.StartsWith("message "))
                {
                    (!HandshakeEstablished || OtherIdentity == null ? OnMessageUnauthorized : OnMessage)?
                        .Invoke(message.Substring(8));
                }
                else if (message == "ping")
                {
                    SendPong();
                    HandshakeEstablished = true;
                    OnHandshakeEstablished?.Invoke();
                }
                else if (message == "who are you?")
                {
                    SendIdentity();
                }
                else if (message.StartsWith("i'm "))
                {
                    OtherIdentity = message.Substring(4);
                    OnIdentify?.Invoke(OtherIdentity);
                }
                else if (message == "pong")
                {
                    HandshakeEstablished = true;
                    OnHandshakeEstablished?.Invoke();
                }
            }
        }).Start();

        int lapse = 0;
        while (!HandshakeEstablished)
        {
            SendPing();
            Thread.Sleep(1000);
            lapse += 1000;

            if (lapse >= Timeout)
            {
                OnTimeout?.Invoke();
                return;
            }
        }

        this.SendIdentify();
    }

    public void SendMessage(string message)
    {
        Client.Send("message " + message);
    }

    private void SendPing()
    {
        Client.Send("ping");
    }

    private void SendPong()
    {
        Client.Send("pong");
    }

    private void SendIdentify()
    {
        Client.Send("who are you?");
    }

    private void SendIdentity()
    {
        Client.Send($"i'm {Identity}");
    }

    private void Receive()
    {
        Client.Receive();
    }
}

public class Client
{
    private UdpClient _client;
    private string _server;
    private int _port;

    public Client(string server, int port)
    {
        _client = new UdpClient();
        _server = server;
        _port = port;
    }

    public void Send(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        byte[] size = BitConverter.GetBytes(data.Length);

        _client.Send(size, size.Length, _server, _port);
        _client.Send(data, data.Length, _server, _port);
    }

    public string Receive()
    {
        IPEndPoint endpoint = new(IPAddress.Parse(_server), _port);
        byte[] data = _client.Receive(ref endpoint);

        return Encoding.ASCII.GetString(data);
    }

    public void Close()
    {
        _client.Close();
    }
}