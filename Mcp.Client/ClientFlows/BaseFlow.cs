using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;

namespace Mcp.Client.ClientFlows;

internal abstract class BaseFlow
{
    protected static Task<IMcpClient> CreateMcpClientAsync(
        Kernel? kernel = null,
        Func<Kernel, CreateMessageRequestParams?, IProgress<ProgressNotificationValue>, CancellationToken, Task<CreateMessageResult>>? samplingRequestHandler = null)
    {
        KernelFunction? skSamplingHandler = null;

        return McpClientFactory.CreateAsync(
            clientTransport: new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "MCPServer",
                Command = GetMCPServerPath(), // Path to the MCPServer executable
            }),
            clientOptions: samplingRequestHandler != null ? new McpClientOptions()
            {
                Capabilities = new ClientCapabilities
                {
                    Sampling = new SamplingCapability
                    {
                        SamplingHandler = InvokeHandlerAsync
                    },
                },
            } : null
         );

        async ValueTask<CreateMessageResult> InvokeHandlerAsync(CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            skSamplingHandler ??= KernelFunctionFactory.CreateFromMethod(
                (CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken ct) =>
                {
                    return samplingRequestHandler(kernel!, request, progress, ct);
                },
                "MCPSamplingHandler"
            );

            // The argument names must match the parameter names of the delegate the SK Function is created from
            KernelArguments kernelArguments = new()
            {
                ["request"] = request,
                ["progress"] = progress
            };

            FunctionResult functionResult = await skSamplingHandler.InvokeAsync(kernel!, kernelArguments, cancellationToken);

            return functionResult.GetValue<CreateMessageResult>()!;
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="Kernel"/> with the OpenAI chat completion service registered.
    /// </summary>
    /// <returns>An instance of <see cref="Kernel"/>.</returns>
    protected static Kernel CreateKernelWithChatCompletionService()
    {
        // Load and validate configuration
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        if (config["OPENAI_API_KEY"] is not { } apiKey)
        {
            const string Message = "Please provide a valid OPENAI_API_KEY.";
            Console.Error.WriteLine(Message);
            throw new InvalidOperationException(Message);
        }

        string modelId = config["OPENAI_CHAT_MODEL"] ?? "gpt-4o-mini";

        // Create kernel
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddOpenAIChatCompletion(modelId: modelId, apiKey: apiKey);

        return kernelBuilder.Build();
    }

    /// <summary>
    /// Displays the list of available MCP tools.
    /// </summary>
    /// <param name="tools">The list of the tools to display.</param>
    protected static void DisplayTools(IList<McpClientTool> tools)
    {
        Console.WriteLine("Available MCP tools:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"- Name: {tool.Name}, Description: {tool.Description}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Returns the path to the MCPServer server executable.
    /// </summary>
    /// <returns>The path to the MCPServer server executable.</returns>
    private static string GetMCPServerPath()
    {
        // Determine the configuration (Debug or Release)  
        string configuration;

#if DEBUG
        configuration = "Debug";
#else
        configuration = "Release";
#endif

        return Path.Combine("..", "..", "..", "..", "Mcp.Server", "bin", configuration, "net8.0", "Mcp.Server.exe");
    }
}
