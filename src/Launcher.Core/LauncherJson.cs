using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace FloxStudios.Launcher.Core;

public static class LauncherJson
{
    private const string INDENT = "  ";

    public static string Serialize<T>(T value)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream();
        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, INDENT))
        {
            serializer.WriteObject(writer, value);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static T Deserialize<T>(string json) where T : class
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        object? value;
        try
        {
            value = serializer.ReadObject(stream);
        }
        catch (Exception exception) when (exception is SerializationException || exception is XmlException)
        {
            throw new InvalidDataException("Malformed JSON: " + exception.Message, exception);
        }
        if (value is T typed)
            return typed;
        throw new InvalidDataException("JSON document is empty.");
    }
}
