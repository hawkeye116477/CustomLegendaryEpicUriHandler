using System;
using System.Text.Json;

namespace CustomLegendaryEpicUriHandler;

public class Serialization
{
    private static readonly JsonSerializerOptions JsonSerializerSettings = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
    };


    public static bool TryFromJson<T>(string json, out T? deserialized, bool writeToLog = true) where T : class
    {
        try
        {
            deserialized = JsonSerializer.Deserialize<T>(json, JsonSerializerSettings);
            return true;
        }
        catch (Exception e)
        {
            deserialized = null;
            if (writeToLog)
            {
                Console.Error.WriteLine($"An error occured during reading json: {e}");
            }

            return false;
        }
    }
}