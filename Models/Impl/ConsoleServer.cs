using System.Net.Sockets;

namespace HoleBite;

public class ConsoleServer : Server
{
    protected readonly List<(string identity, string content)> Messages = new();
    protected readonly List<(TcpClient client, string identity, DateTime time)> TypingClients = new();

    public ConsoleServer(int port) : base(port)
    {
    }

    public override void Start()
    {
        Log("server", $"starting server on port {this.Port}");

        base.Start();
    }

    protected override void ClientConnected(TcpClient client)
    {
        Log("connect", $"{ShowClient(client, null)} connected to the server");

        // We don't show a welcome message to the client until they identify themselves, so we just tell them we're loading
        this.Send(client, "message", ColorHelpers.Color("Loading identity...", ConsoleColor.Yellow));
    }

    protected override void ClientDisconnected(TcpClient client, string? identity)
    {
        this.TypingClients.RemoveAll(x => x.client == client);
        this.SendTyping();

        Log("disconnect", $"{ShowClient(client, identity)} disconnected from the server");

        // Tell the other clients that this client has left, if they identified themselves
        if (identity != null)
            this.Broadcast("message", ColorHelpers.Color($"{identity}", ConsoleColor.Cyan) + " has left the chat");
    }

    protected override void IdentityReceived(TcpClient client, string identity)
    {
        // Validate the identity
        const string illegalChars = " \t\n,|";
        if (identity.Trim().Length == 0 || identity.Length > 20 ||
            identity.IndexOfAny(illegalChars.ToCharArray()) != -1)
        {
            this.Send(client, "message", ColorHelpers.Color("Invalid identity", ConsoleColor.Red));
            throw new DisconnectException("Client sent invalid identity");
        }

        // Log the identity change
        Log("identity", $"{ShowClient(client, null)} is now known as {identity}");

        // Send a welcome message and tell the other clients about the new user
        this.Send(client, "message", ColorHelpers.Color($"Welcome {identity}!", ConsoleColor.Cyan));
        this.Broadcast("message", ColorHelpers.Color($"{identity}", ConsoleColor.Green) + " has joined the chat");
    }

    protected override void MessageReceived(TcpClient client, string? identity, string message)
    {
        // Check if the client has identified themselves
        if (identity == null)
            throw new DisconnectException("Client sent message before identifying");

        // Validate the message
        const string illegalChars = "\0\n\t";
        if (message.Trim().Length == 0 || message.IndexOfAny(illegalChars.ToCharArray()) != -1)
            return;

        // Log and broadcast the message
        Log("message", $"[{ShowClient(client, identity)}] {message}");
        this.Messages.Add((identity, message));
        this.Broadcast("message", ColorHelpers.Color($"[{identity}]", ConsoleColor.Cyan) + " " + message);

        // Clear the typing status
        if (this.TypingClients.Any(c => c.client == client))
            this.TypingClients.RemoveAll(c => c.client == client);

        this.SendTyping();
    }

    protected override void TypingReceived(TcpClient client, string? identity)
    {
        // Check if the client has identified themselves
        if (identity == null)
            return; // Don't disconnect, just ignore the typing notification, since it's very possible the client is typing before they've identified themselves

        // Log the typing notification
        Log("typing", $"{ShowClient(client, identity)} is typing...");

        TypingClients.Add((client, identity, DateTime.Now));
        this.SendTyping();
    }

    protected override void ClientHandleError(TcpClient client, string? identity, Exception exception)
    {
        // Close the client connection if it's a disconnect exception, basically an error that's expected, like an invalid identity
        if (exception is DisconnectException)
            client.Close();

        // Log the error
        Log("error",
            $"{ShowClient(client, identity)} threw an exception {exception.GetType().Name}: {exception.Message}");
    }

    private string ShowClient(TcpClient client, string? identity)
    {
        // Show the client's identity if they have one, otherwise show their IP address
        // In the small chance the client doesn't have an IP address, show "unknown"
        return (identity ?? client.Client.RemoteEndPoint?.ToString()) ?? "unknown";
    }

    private void Log(string source, string message)
    {
        // Log a message to the console, padding the source to 10 characters so all the messages line up
        Console.WriteLine($"{source.PadRight(10)} | {message}");
    }

    private void SendTyping()
    {
        TypingClients.RemoveAll(c => (DateTime.Now - c.time).TotalSeconds > 5);

        // Remove duplicates, keeping the most recent typing notification
        var mostRecentClients = TypingClients
            .GroupBy(t => t.client)
            .Select(g => g.OrderByDescending(t => t.time).First())
            .ToList();
        TypingClients.RemoveAll(t => !mostRecentClients.Contains(t));

        if (this.TypingClients.Count == 0)
        {
            this.Broadcast("status", "");
            return;
        }

        foreach (var client in this.Clients)
        {
            string typing = string.Join(", ", TypingClients.Where(c => c.client != client).Select(c => c.identity));
            if (typing.Length == 0) continue;
            this.Send(client, "status", ColorHelpers.Color($"{typing} is typing...", ConsoleColor.DarkGray));
        }
    }
}