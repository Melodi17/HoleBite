using System.Net.Sockets;
using System.Text;

namespace HoleBite;

public class Client
{
    private TcpClient client;
    private NetworkStream stream;
    public string name;

    public event Action<string> MessageReceived;

    public Client(string server, int port, string name)
    {
        this.client = new TcpClient(server, port);
        this.stream = this.client.GetStream();
        this.name = name;
    }

    public void SendMessage(string message)
    {
        this.Send("message", message);
    }

    public void Start()
    {
        this.Send("i'm", this.name);

        while (true)
        {
            try
            {
                string message = this.Receive();
                string[] parts = message.Split(' ', 2);

                if (parts[0] == "message")
                    this.MessageReceived?.Invoke(parts[1]);

                if (parts[0] == "rich")
                {
                    Console.ForegroundColor = Enum.Parse<ConsoleColor>(parts[1], true);
                }

                if (parts[0] == "bye")
                    break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }
        }
    }

    private void Send(string code, string? message = null)
    {
        byte[] data = Encoding.ASCII.GetBytes(code + (message == null ? "" : " " + message));
        byte[] size = BitConverter.GetBytes(data.Length);
        this.stream.Write(size, 0, size.Length);
        this.stream.Write(data, 0, data.Length);
    }

    private string Receive()
    {
        byte[] size = new byte[4];
        this.stream.Read(size, 0, size.Length);
        byte[] data = new byte[BitConverter.ToInt32(size, 0)];

        this.stream.Read(data, 0, data.Length);
        return Encoding.ASCII.GetString(data);
    }

    public void Close()
    {
        this.stream.Close();
        this.client.Close();
    }
}