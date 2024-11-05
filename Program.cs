using System.Text;
using Konsole;

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

        var consoles = Window.HostConsole.SplitRows(
            new Split(0),
            new Split(1)
        );

        var chat = consoles[0];
        var input = consoles[1];

        ConcurrentWriter chatWriter = new(chat);
        
        Client c = new(server, port, args.Length > 1 ? args[1] : Environment.UserName);
        c.MessageReceived += (message) =>
        {
            messages.Add(message);
            
            StringBuilder screen = new();
            for (int i = Math.Max(0, messages.Count - maxMessages); i < messages.Count; i++)
                screen.AppendLine(messages[i] + new string(' ', width - messages[i].Length));
            for (int i = messages.Count; i < maxMessages; i++)
                screen.AppendLine(new string(' ', width));

            lock (consoleLockObject)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(screen.ToString().TrimEnd('\n'));
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
            Console.SetCursorPosition(3, inputRow);
            string message = ReadLine()!;
            c.SendMessage(message);
        }
    }
}