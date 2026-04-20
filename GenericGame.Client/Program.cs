using GenericGame;
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
                    Log.Write($"Server port: {serverPort}");
                }
                else
                {
                    Log.Write("Invalid port number. Using default: 14000");
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
        Log.Write("Press Enter to disconnect...");
        Console.ReadLine();

        client.Disconnect();
    }

    static void PrintHelp()
    {
        Log.Write("Generic Game Client");
        Log.Write("Usage: GenericGame.Client [options]");
        Log.Write("");
        Log.Write("Options:");
        Log.Write("  -h, --host <address>  Server IP address or hostname (default: localhost)");
        Log.Write("  -p, --port <port>     Server port (default: 14000)");
        Log.Write("  -n, --name <name>     Player name (default: Player)");
        Log.Write("  -o, --observer        Connect as observer (spectator)");
        Log.Write("  -h, --help            Show this help message");
    }
}
