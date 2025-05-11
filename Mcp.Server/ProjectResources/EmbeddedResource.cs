using System.Reflection;

namespace Mcp.Server.ProjectResources;

public static class EmbeddedResource
{
    private static readonly string? s_namespace = typeof(EmbeddedResource).Namespace;

    public static string ReadAsString(string resourcePath)
    {
        Stream stream = ReadAsStream(resourcePath);

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public static byte[] ReadAsBytes(string resourcePath)
    {
        Stream stream = ReadAsStream(resourcePath);

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static Stream ReadAsStream(string resourcePath)
    {
        // Get the current assembly. Note: this class is in the same assembly where the embedded resources are stored.
        Assembly assembly =
            typeof(EmbeddedResource).GetTypeInfo().Assembly ??
            throw new InvalidOperationException($"[{s_namespace}] {resourcePath} assembly not found");

        // Resources are mapped like types, using the namespace and appending "." (dot) and the file name
        string resourceName = $"{s_namespace}.{resourcePath}";

        return
            assembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException($"{resourceName} resource not found");
    }
}
