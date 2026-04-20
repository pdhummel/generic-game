using GenericGame.Client;
using GenericGame.Server;
using TicTacToe.AI;
using TicTacToe.Game;
using TicTacToe.UI;

namespace TicTacToe;

/// <summary>
/// Tic-Tac-Toe game launcher that supports all three modes:
/// 1. Stand-alone server only
/// 2. Server and client
/// 3. Client only
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        string mode = "client"; // Default mode
        string serverAddress = "localhost";
        int serverPort = 14000;
        string playerName = "Player";
        bool isObserver = false;
        bool showHelp = false;

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-m" || args[i] == "--mode" && i + 1 < args.Length)
            {
                mode = args[++i].ToLower();
            }
            else if (args[i] == "-h" || args[i] == "--host" && i + 1 < args.Length)
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

        switch (mode)
        {
            case "server":
                RunServerOnly(serverPort);
                break;
            case "client":
                RunClientOnly(serverAddress, serverPort, playerName, isObserver);
                break;
            case "both":
                RunServerAndClient(serverPort, playerName);
                break;
            default:
                Console.WriteLine($"Unknown mode: {mode}");
                PrintHelp();
                break;
        }
    }

    static void RunServerOnly(int port)
    {
        Console.WriteLine("Starting server in stand-alone mode...");
        var server = new GameServer(port);
        server.Run();
    }

    static void RunClientOnly(string address, int port, string playerName, bool isObserver)
    {
        Console.WriteLine($"Starting client in stand-alone mode...");
        var client = new GameClient();
        var lobby = new LobbyScreen(client);

        // Connect to server
        client.Connect(address, port, playerName, isObserver);

        // Poll for connection events
        for (int i = 0; i < 50; i++)
        {
            client.Update();
            Thread.Sleep(10);
        }

        // Show lobby screen first
        lobby.Run();

        client.Disconnect();
    }

    static void RunServerAndClient(int port, string playerName)
    {
        Console.WriteLine("Starting server and client...");

        // Start server in a background thread
        var server = new GameServer(port);
        var serverThread = new Thread(() => server.Run())
        {
            IsBackground = true
        };
        serverThread.Start();

        // Give server time to start
        Thread.Sleep(1000);

        // Create client and lobby screen
        var client = new GameClient();
        var lobby = new LobbyScreen(client);

        // Connect to server
        client.Connect("localhost", port, playerName, false);

        // Poll for connection events
        for (int i = 0; i < 50; i++)
        {
            client.Update();
            Thread.Sleep(10);
        }

        // Show lobby screen
        lobby.Run();

        client.Disconnect();
    }

    static void PrintHelp()
    {
        Console.WriteLine("Tic-Tac-Toe Game");
        Console.WriteLine("Usage: TicTacToe [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --mode <mode>     Run mode: server, client, or both (default: client)");
        Console.WriteLine("  -h, --host <address>  Server IP address or hostname (default: localhost)");
        Console.WriteLine("  -p, --port <port>     Server port (default: 14000)");
        Console.WriteLine("  -n, --name <name>     Player name (default: Player)");
        Console.WriteLine("  -o, --observer        Connect as observer (spectator)");
        Console.WriteLine("  -h, --help            Show this help message");
        Console.WriteLine();
        Console.WriteLine("Modes:");
        Console.WriteLine("  server    Run only the game server");
        Console.WriteLine("  client    Connect to a remote server as a client");
        Console.WriteLine("  both      Run both server and client on the same machine");
    }
}
