using Mcp.Server.Extensions;
using Mcp.Server.ProjectResources;
using Mcp.Server.Prompts;
using Mcp.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

(string embeddingModelId, string chatModelId, string apiKey) = GetConfiguration();

IKernelBuilder kernelBuilder = builder.Services.AddKernel();
kernelBuilder.Plugins.AddFromType<WeatherUtils>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    // Add all functions from the kernel plugins to the MCP server as tools
    .WithTools()
    .WithPrompt(PromptDefinition.Create(EmbeddedResource.ReadAsString("getCurrentWeatherForCity.json")));
await builder.Build().RunAsync();

static (string EmbeddingModelId, string ChatModelId, string ApiKey) GetConfiguration()
{
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

    string embeddingModelId = config["OPENAI_EMBEDDING_MODEL"] ?? "text-embedding-ada-002";

    string chatModelId = config["OPENAI_CHAT_MODEL"] ?? "gpt-4o-mini";

    return (embeddingModelId, chatModelId, apiKey);
}
