using System.Text;

namespace HoleBite;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Client usage: holebite.exe <server>");
            Console.WriteLine("Server usage: holebite.exe --server");
            return;
        }

        string server = args[0];
        int port = 13577;

        if (server == "--server")
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
        Console.CursorVisible = false;
        Console.CancelKeyPress += (s, e) => Console.CursorVisible = true;
        
        // Enable ANSI escape sequences on Windows
        ConsoleAnsiUtils.Initialize();
        
        List<string> messages = new();
        string input = "";
        bool dirty = true;

        string identity = args.Length > 1 ? args[1] : Environment.UserName;
        Client c = new(server, port, identity);

        void MessageReceive(string m)
        {
            messages.Add(m);
            dirty = true;
        }

        c.MessageReceived += MessageReceive;
        c.ClearRequested += () =>
        {
            messages.Clear();
            dirty = true;
        };
        c.ConnectionLost += () =>
        {
            Console.Clear();
            Console.WriteLine("Connection lost.");
            Environment.Exit(1);
        };

        void RenderThread()
        {
            while (true)
            {
                if (dirty)
                {
                    dirty = false;
                    Render(messages, input);
                }
                else
                    Thread.Sleep(10);
            }
        }

        new Thread(c.StartListening).Start();
        new Thread(RenderThread).Start();

        while (true)
        {
            try
            {
                ReadLine(ref input, ref dirty);
            }
            catch
            {
                break;
            }
            c.SendMessage(input);
            input = "";
        }
        
        Console.CursorVisible = true;
    }

    private static void ReadLine(ref string input, ref bool dirty)
    {
        while (true)
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Enter && input.Length > 0)
                return;
            else if (cki.Key == ConsoleKey.Escape)
                input = "";
            else if (cki.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                    input = input.Substring(0, input.Length - 1);
            }
            else if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}\\|;:'\",.<>/? ".Contains(cki.KeyChar))
                input += cki.KeyChar;
            
            dirty = true;
        }
    }

    private static void Render(List<string> messages, string input)
    {
        int maxMessages = Console.WindowHeight - 2;
        int width = Console.WindowWidth - 1;
        
        StringBuilder screen = new();
        for (int i = Math.Max(0, messages.Count - maxMessages); i < messages.Count; i++)
            screen.AppendLine(messages[i] + new string(' ', width - messages[i].Length));
        for (int i = messages.Count; i < maxMessages; i++)
            screen.AppendLine(new string(' ', width));
        
        screen.Append(" : " + input);
        screen.Append(new string(' ', width - input.Length - 3));
        
        Console.SetCursorPosition(0, 0);
        Console.Write(screen);
    }
}