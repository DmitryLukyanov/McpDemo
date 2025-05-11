using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace Mcp.Server.Prompts;

public sealed class PromptDefinition
{
    public required Prompt Prompt { get; init; }
    public required Func<RequestContext<GetPromptRequestParams>, CancellationToken, Task<GetPromptResult>> Handler { get; init; }
    public static PromptDefinition Create(string jsonPrompt, Kernel? kernel = null)
    {
        PromptTemplateConfig promptTemplateConfig = PromptTemplateConfig.FromJson(jsonPrompt);

        IPromptTemplate promptTemplate = new HandlebarsPromptTemplateFactory().Create(promptTemplateConfig);

        return new PromptDefinition()
        {
            Prompt = GetPrompt(promptTemplateConfig),
            Handler = (context, cancellationToken) =>
            {
                return GetPromptHandlerAsync(context, promptTemplateConfig, promptTemplate, kernel, cancellationToken);
            }
        };
    }

    private static Prompt GetPrompt(PromptTemplateConfig promptTemplateConfig)
    {
        List<PromptArgument>? arguments = null;

        foreach (var inputVariable in promptTemplateConfig.InputVariables)
        {
            (arguments ??= []).Add(new()
            {
                Name = inputVariable.Name,
                Description = inputVariable.Description,
                Required = inputVariable.IsRequired
            });
        }

        return new Prompt
        {
            Name = promptTemplateConfig.Name!,
            Description = promptTemplateConfig.Description,
            Arguments = arguments
        };
    }

    private static async Task<GetPromptResult> GetPromptHandlerAsync(RequestContext<GetPromptRequestParams> context, PromptTemplateConfig promptTemplateConfig, IPromptTemplate promptTemplate, Kernel? kernel, CancellationToken cancellationToken)
    {
        kernel ??= context.Server.Services?.GetRequiredService<Kernel>() ?? throw new InvalidOperationException("Kernel is not available.");

        string renderedPrompt = await promptTemplate.RenderAsync(
            kernel: kernel,
            arguments: context.Params?.Arguments is { } args ? new KernelArguments(args.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)) : null,
            cancellationToken: cancellationToken);

        return new GetPromptResult()
        {
            Description = promptTemplateConfig.Description,
            Messages =
            [
                new PromptMessage()
                {
                    Content = new Content()
                    {
                        Type = "text",
                        Text = renderedPrompt
                    },
                    Role = Role.Assistant
                }
            ]
        };
    }
}
