using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;

namespace Mcp.Client;

/// <summary>
/// Extension methods for the <see cref="Content"/> class.
/// </summary>
public static class ContentExtensions
{
    /// <summary>
    /// Converts a <see cref="Content"/> object to a <see cref="KernelContent"/> object.
    /// </summary>
    /// <param name="content">The <see cref="Content"/> object to convert.</param>
    /// <returns>The corresponding <see cref="KernelContent"/> object.</returns>
    public static KernelContent ToKernelContent(this Content content)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return content.Type switch
        {
            "text" => new TextContent(content.Text),
            "image" => new ImageContent(Convert.FromBase64String(content.Data!), content.MimeType),
            "audio" => new AudioContent(Convert.FromBase64String(content.Data!), content.MimeType),
            _ => throw new InvalidOperationException($"Unexpected message content type '{content.Type}'"),
        };
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}
