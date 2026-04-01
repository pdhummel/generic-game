using GenericGame.Client;

class Program
{
    static void Main(string[] args)
    {
        string serverAddress = "localhost";
        int serverPort = 14000;
        string playerName = "Player";
        bool isObserver = false;
        bool showHelp = false;

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-h" || args[i] == "--host" && i + 1 < args.Length)
            {
                serverAddress = args[++i];
            }
            else if (args[i] == "-p" || args[i] == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out serverPort))
                {
                    Console.WriteLine($"Server port: {serverPort}");
                }
                else
                {
                    Console.WriteLine("Invalid port number. Using default: 14000");
                }
            }
            else if (args[i] == "-n" || args[i] == "--name" && i + 1 < args.Length)
            {
                playerName = args[++i];
            }
            else if (args[i] == "-o" || args[i] == "--observer")
            {
                isObserver = true;
            }
            else if (args[i] == "-h" || args[i] == "--help")
            {
                showHelp = true;
            }
        }

        if (showHelp)
        {
            PrintHelp();
            return;
        }

        var client = new GameClient();

        // Connect to server
        client.Connect(serverAddress, serverPort, playerName, isObserver);

        // Wait for connection
        Console.WriteLine("Press Enter to disconnect...");
        Console.ReadLine();

        client.Disconnect();
    }

    static void PrintHelp()
    {
        Console.WriteLine("Generic Game Client");
        Console.WriteLine("Usage: GenericGame.Client [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --host <address>  Server IP address or hostname (default: localhost)");
        Console.WriteLine("  -p, --port <port>     Server port (default: 14000)");
        Console.WriteLine("  -n, --name <name>     Player name (default: Player)");
        Console.WriteLine("  -o, --observer        Connect as observer (spectator)");
        Console.WriteLine("  -h, --help            Show this help message");
    }
}
