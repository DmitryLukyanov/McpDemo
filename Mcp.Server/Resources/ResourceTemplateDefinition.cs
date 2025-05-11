using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace Mcp.Server.Resources;

public sealed class ResourceTemplateDefinition
{
    private Regex? _regex = null;
    private KernelFunction? _kernelFunction = null;
    public required ResourceTemplate ResourceTemplate { get; init; }
    public required Delegate Handler { get; init; }
    public Kernel? Kernel { get; set; }
    public bool IsMatch(string uri) => this.GetRegex().IsMatch(uri);
    public async ValueTask<ReadResourceResult> InvokeHandlerAsync(RequestContext<ReadResourceRequestParams> context, CancellationToken cancellationToken)
    {
        this._kernelFunction ??= KernelFunctionFactory.CreateFromMethod(this.Handler);

        this.Kernel
            ??= context.Server.Services?.GetRequiredService<Kernel>()
            ?? throw new InvalidOperationException("Kernel is not available.");

        KernelArguments args = new(source: this.GetArguments(context.Params!.Uri!))
        {
            { "context", context },
        };

        FunctionResult result = await this._kernelFunction.InvokeAsync(kernel: this.Kernel, arguments: args, cancellationToken: cancellationToken);

        return result.GetValue<ReadResourceResult>() ?? throw new InvalidOperationException("The handler did not return a valid result.");
    }

    private Regex GetRegex()
    {
        if (this._regex != null)
        {
            return this._regex;
        }

        var pattern = "^" +
                      Regex.Escape(this.ResourceTemplate.UriTemplate)
                           .Replace("\\{", "(?<")
                           .Replace("}", ">[^/]+)") +
                      "$";

        return this._regex = new(pattern, RegexOptions.Compiled);
    }

    private Dictionary<string, object?> GetArguments(string uri)
    {
        var match = this.GetRegex().Match(uri);
        if (!match.Success)
        {
            throw new ArgumentException($"The uri '{uri}' does not match the template '{this.ResourceTemplate.UriTemplate}'.");
        }

        return match.Groups.Cast<Group>().Where(g => g.Name != "0").ToDictionary(g => g.Name, g => (object?)g.Value);
    }
}
