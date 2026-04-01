using GenericGame.Server;

class Program
{
    static void Main(string[] args)
    {
        int port = 14000;

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-p" || args[i] == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out port))
                {
                    Console.WriteLine($"Server port: {port}");
                }
                else
                {
                    Console.WriteLine("Invalid port number. Using default: 14000");
                }
            }
            else if (args[i] == "-h" || args[i] == "--help")
            {
                PrintHelp();
                return;
            }
        }

        var server = new GameServer(port);

        // Handle console input for commands
        Console.WriteLine("Server running. Type 'help' for commands.");
        Console.WriteLine("Commands: list, start <gameid>, stop, exit");

        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            switch (command)
            {
                case "help":
                case "?":
                    PrintHelp();
                    break;
                case "list":
                    var games = server.GetGames();
                    Console.WriteLine($"Active games: {games.Count}");
                    foreach (var game in games)
                    {
                        Console.WriteLine($"  - {game.Name} (ID: {game.GameId}, Players: {game.State.Players.Count})");
                    }
                    break;
                case "start":
                    if (parts.Length > 1 && Guid.TryParse(parts[1], out var gameId))
                    {
                        var game = server.GetGame(gameId);
                        if (game != null)
                        {
                            game.StartGame();
                            Console.WriteLine($"Game {gameId} started");
                        }
                        else
                        {
                            Console.WriteLine($"Game {gameId} not found");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usage: start <gameid>");
                    }
                    break;
                case "stop":
                    server.Run(); // This will block, so we need to handle this differently
                    break;
                case "exit":
                case "quit":
                    return;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("Generic Game Server");
        Console.WriteLine("Usage: GenericGame.Server [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -p, --port <port>     Port to listen on (default: 14000)");
        Console.WriteLine("  -h, --help            Show this help message");
        Console.WriteLine();
        Console.WriteLine("Commands (during runtime):");
        Console.WriteLine("  help, ?               Show this help message");
        Console.WriteLine("  list                  List active games");
        Console.WriteLine("  start <gameid>        Start a game");
        Console.WriteLine("  stop                  Stop the server");
        Console.WriteLine("  exit, quit            Exit the server");
    }
}
