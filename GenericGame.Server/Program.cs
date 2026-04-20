using GenericGame;
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
                    Log.Write($"Server port: {port}");
                }
                else
                {
                    Log.Write("Invalid port number. Using default: 14000");
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
        Log.Write("Server running. Type 'help' for commands.");
        Log.Write("Commands: list, start <gameid>, stop, exit");

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
                    Log.Write($"Active games: {games.Count}");
                    foreach (var game in games)
                    {
                        Log.Write($"  - {game.Name} (ID: {game.GameId}, Players: {game.State.Players.Count})");
                    }
                    break;
                case "start":
                    if (parts.Length > 1 && Guid.TryParse(parts[1], out var gameId))
                    {
                        var game = server.GetGame(gameId);
                        if (game != null)
                        {
                            game.StartGame();
                            Log.Write($"Game {gameId} started");
                        }
                        else
                        {
                            Log.Write($"Game {gameId} not found");
                        }
                    }
                    else
                    {
                        Log.Write("Usage: start <gameid>");
                    }
                    break;
                case "stop":
                    server.Run(); // This will block, so we need to handle this differently
                    break;
                case "exit":
                case "quit":
                    return;
                default:
                    Log.Write($"Unknown command: {command}");
                    break;
            }
        }
    }

    static void PrintHelp()
    {
        Log.Write("Generic Game Server");
        Log.Write("Usage: GenericGame.Server [options]");
        Log.Write("");
        Log.Write("Options:");
        Log.Write("  -p, --port <port>     Port to listen on (default: 14000)");
        Log.Write("  -h, --help            Show this help message");
        Log.Write("");
        Log.Write("Commands (during runtime):");
        Log.Write("  help, ?               Show this help message");
        Log.Write("  list                  List active games");
        Log.Write("  start <gameid>        Start a game");
        Log.Write("  stop                  Stop the server");
        Log.Write("  exit, quit            Exit the server");
    }
}
