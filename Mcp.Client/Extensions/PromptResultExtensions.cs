using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Protocol.Types;

namespace Mcp.Client.Extensions;

/// <summary>
/// Extension methods for <see cref="GetPromptResult"/>.
/// </summary>
internal static class PromptResultExtensions
{
    public static IList<ChatMessageContent> ToChatMessageContents(this GetPromptResult result)
    {
        return [.. result.Messages.Select(ToChatMessageContent)];
    }

    public static ChatMessageContent ToChatMessageContent(this PromptMessage message)
    {
        return new ChatMessageContent(role: message.Role.ToAuthorRole(), items: [message.Content.ToKernelContent()]);
    }
}
