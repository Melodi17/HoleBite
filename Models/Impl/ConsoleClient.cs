using System.Net.Sockets;
using System.Text;

namespace HoleBite;

public class ConsoleClient : Client
{
    private readonly List<string> _messages = new();
    private string _input = "";
    private bool _dirty = true;
    private bool _running = true;
    private string? _status = null;
    private DateTime _statusTime = DateTime.Now;
    
    private readonly Thread _renderThread;
    private readonly Thread _inputThread;
    private readonly Thread _listenThread;
    
    public ConsoleClient(string server, int port, string identity) : base(server, port, identity)
    {
        this._renderThread = new(this.RenderLoop);
        this._inputThread = new(this.InputLoop);
        this._listenThread = new(this.ListenLoop);
    }

    public override void Start()
    {
        this._running = true;
        
        this._renderThread.Start();
        this._inputThread.Start();
        this._listenThread.Start();
        
        this.SendIdentity();
        
        while (this._running)
            Thread.Sleep(10);
    }

    public override void Stop()
    {
        this._running = false;
        
        this._renderThread.Join();
        this._inputThread.Join();
        this._listenThread.Join();
    }

    protected override void MessageReceived(string message)
    {
        this._messages.Add(message);
        this._dirty = true;
    }

    protected override void StatusBar(string message)
    {
        this._status = message;
        this._statusTime = DateTime.Now;
        this._dirty = true;
    }

    protected override void ClearRequested()
    {
        this._messages.Clear();
        this._dirty = true;
    }

    protected override void ConnectionLost()
    {
        this.Stop();
        Console.Clear();
        Console.WriteLine("Connection lost.");
    }
    
    private void InputLoop()
    {
        while (this._running)
        {
            this.ReadLine();
            this.SendMessage(this._input);
            this._input = "";
            this._dirty = true;
        }
    }
    
    private void ReadLine()
    {
        while (this._running)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(10);
                continue;
            }
            
            this.SendTyping();
            
            ConsoleKeyInfo cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Enter && this._input.Length > 0)
                return;
            else if (cki.Key == ConsoleKey.Escape)
                this._input = "";
            else if (cki.Key == ConsoleKey.Backspace)
            {
                if (this._input.Length > 0)
                    this._input = this._input[..^1];
            }
            else if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}\\|;:'\",.<>/? ".Contains(cki.KeyChar))
                this._input += cki.KeyChar;
            
            this._dirty = true;
        }
    }
    
    private void RenderLoop()
    {
        while (this._running)
        {
            if (this._statusTime + TimeSpan.FromSeconds(5) < DateTime.Now)
            {
                this._status = null;
                this._dirty = true;
            }
            
            if (this._dirty)
            {
                this._dirty = false;

                this.Render();
            }
            else
                Thread.Sleep(10);
        }
    }

    private void Render()
    {
        int maxMessages = Console.WindowHeight - 2;
        int width = Console.WindowWidth - 1;
        
        if (this._status != null)
            maxMessages--;
        
        StringBuilder screen = new();
        for (int i = Math.Max(0, this._messages.Count - maxMessages); i < this._messages.Count; i++)
            screen.AppendLine(this._messages[i] + new string(' ', width - this._messages[i].Length));
        for (int i = this._messages.Count; i < maxMessages; i++)
            screen.AppendLine(new(' ', width));

        if (this._status != null)
        {
            screen.AppendLine(this._status + new string(' ', width - this._status.Length));
        }
        
        screen.Append(" : " + this._input);
        screen.Append(new string(' ', width - this._input.Length - 3));
        
        Console.SetCursorPosition(0, 0);
        Console.Write(screen);
    }
    
    private void ListenLoop()
    {
        while (this._running)
        {
            try
            {
                this.Listen();
            }
            catch (Exception e) when (e is SocketException or IOException)
            {
                this.ConnectionLost();
                break;
            }
        }
    }
}