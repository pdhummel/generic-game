using System.Text.Json;

namespace GenericGame.Server;

/// <summary>
/// Helper class for serializing and deserializing network messages
/// </summary>
public static class NetMessageSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an object to a byte array
    /// </summary>
    public static byte[] Serialize(object obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, _jsonOptions);
    }

    /// <summary>
    /// Deserializes a byte array to an object of the specified type
    /// </summary>
    public static T? Deserialize<T>(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }
}
