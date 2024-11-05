using System.Net.Sockets;
using System.Text;

namespace HoleBite;

public abstract class Client
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    public readonly string Identity;
    
    private DateTime _lastTyping = DateTime.Now;

    protected Client(string server, int port, string identity)
    {
        this._client = new(server, port);
        this._stream = this._client.GetStream();
        this.Identity = identity;
    }

    public void SendMessage(string message)
    {
        this.Send("message", message);
    }

    protected void SendIdentity()
    {
        this.Send("i'm", this.Identity);
    }
    
    protected void SendTyping()
    {
        // Don't send typing notifications too often
        if ((DateTime.Now - this._lastTyping).TotalSeconds < 1)
            return;
        
        this.Send("typing");
        
        this._lastTyping = DateTime.Now;
    }

    protected void Listen()
    {
        string? message = this.Receive();
        if (message == null) return;
        
        string[] parts = message.Split(' ', 2);

        if (parts[0] == "message")
            MessageReceived(parts[1]);

        if (parts[0] == "clear")
            ClearRequested();
        
        if (parts[0] == "status")
            StatusBar(parts[1]);
    }

    public virtual void Start()
    {
        while (true)
        {
            try
            {
                this.Listen();
            }
            catch (Exception e) when (e is SocketException or IOException)
            {
                ConnectionLost();
                break;
            }
        }
    }
    public virtual void Stop()
    {
        this.Close();
    }
    protected abstract void MessageReceived(string message);
    protected abstract void StatusBar(string message);
    protected abstract void ClearRequested();
    protected abstract void ConnectionLost();

    private void Send(string code, string? message = null)
    {
        byte[] data = Encoding.ASCII.GetBytes(code + (message == null ? "" : " " + message));
        byte[] size = BitConverter.GetBytes(data.Length);
        this._stream.Write(size, 0, size.Length);
        this._stream.Write(data, 0, data.Length);
    }

    private string? Receive()
    {
        if (this._stream.DataAvailable == false)
            return null;
        
        byte[] size = new byte[4];
        this._stream.Read(size, 0, size.Length);
        byte[] data = new byte[BitConverter.ToInt32(size, 0)];

        this._stream.Read(data, 0, data.Length);
        return Encoding.ASCII.GetString(data);
    }

    protected void Close()
    {
        this._stream.Close();
        this._client.Close();
    }
}