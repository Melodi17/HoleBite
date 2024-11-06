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
        Server s = new ConsoleServer(port);
        s.Start();
    }

    private static void RunClient(string[] args, string server, int port)
    {
        Console.CursorVisible = false;
        Console.CancelKeyPress += (s, e) => Console.CursorVisible = true;
        Console.Clear();
        
        // Enable ANSI escape sequences on Windows
        ConsoleHelpers.Initialize();

        string identity = args.Length > 1 ? args[1] : Environment.UserName;
        Client c = new ConsoleClient(server, port, identity);
        c.Start();
       
        Console.CursorVisible = true;
    }
}