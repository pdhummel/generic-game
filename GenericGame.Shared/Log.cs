using System.Runtime.CompilerServices;

namespace GenericGame;

public static class Log
{
    // Filter list for log messages - only messages containing these strings will be logged
    // Uncomment and add strings to filter specific log messages
    //static List<string> logTextMatches = ["outputDataStructureUse()", "sendJsonString()", "processGameEventQueue()"];
    // Useful for debugging just the AI planning
    //static List<string> logTextMatches = ["Ai ", "AiGoal"];
    //static List<string> logTextMatches = ["HasResourceInRange", "CheckIfHasRequiredResources", "placeResource"];
    static List<string> logTextMatches = new List<string>();

    public static void Write(string message, [CallerFilePath] string sourceFilePath = "")
    {
        string className = Path.GetFileNameWithoutExtension(sourceFilePath);
        string output = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {className} {message}";
        if (logTextMatches != null && logTextMatches.Count > 0)
        {
            bool matchFound = false;
            foreach (string logText in logTextMatches)
            {
                if (output.Contains(logText))
                {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound)
                return;
        }
        Console.WriteLine(output);
    }
}
