using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace Mcp.Server.Resources;

public sealed class ResourceDefinition
{
    private KernelFunction? _kernelFunction = null;
    public required Resource Resource { get; init; }
    public required Delegate Handler { get; init; }
    public Kernel? Kernel { get; set; }
    public static ResourceDefinition CreateBlobResource(string uri, string name, byte[] content, string mimeType, string? description = null, Kernel? kernel = null)
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        return new()
        {
            Kernel = kernel,
            Resource = new() { Uri = uri, Name = name, Description = description },
            Handler = async (RequestContext<ReadResourceRequestParams> context, CancellationToken cancellationToken) =>
            {
                return new ReadResourceResult()
                {
                    Contents =
                    [
                        new BlobResourceContents()
                        {
                            Blob = Convert.ToBase64String(content),
                            Uri = uri,
                            MimeType = mimeType,
                        }
                    ],
                };
            }
        };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    public async ValueTask<ReadResourceResult> InvokeHandlerAsync(RequestContext<ReadResourceRequestParams> context, CancellationToken cancellationToken)
    {
        this._kernelFunction ??= KernelFunctionFactory.CreateFromMethod(this.Handler);

        this.Kernel
            ??= context.Server.Services?.GetRequiredService<Kernel>()
            ?? throw new InvalidOperationException("Kernel is not available.");

        KernelArguments args = new()
        {
            { "context", context },
        };

        FunctionResult result = await this._kernelFunction.InvokeAsync(kernel: this.Kernel, arguments: args, cancellationToken: cancellationToken);

        return result.GetValue<ReadResourceResult>() ?? throw new InvalidOperationException("The handler did not return a valid result.");
    }
}
