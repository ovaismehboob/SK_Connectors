using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

var kernelSettings = KernelSettings.LoadSettings();

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Information).AddDebug());
builder.Services.AddChatCompletionService(kernelSettings);
builder.Services.AddHttpClient<HuggingFaceConnector>();

Kernel kernel = builder.Build();

//Adding Bing Connector
#region key
string bingKey = "enter your Bing key here";
#endregion

var bingConnector = new BingConnector(bingKey);
var plugin = new WebSearchEnginePlugin(bingConnector);
kernel.ImportPluginFromObject(plugin, "BingPlugin");

// Create the chat history
ChatHistory chatMessages = [];

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var huggingFaceConnector = kernel.GetRequiredService<HuggingFaceConnector>();

// Loop till we are cancelled
while (true)
{
    // Get user input
    System.Console.Write("Enter Model Name > ");
    string modelName = Console.ReadLine();
    string query = "{\"modelName\": \"\", \"modelDescription\": \"\", \"History\": \"\", \"Features\": \"\",  \"Year Released\": \"\", \"VideoLink\": \"\", \"NewReleases\": \"\"}";
    chatMessages.AddUserMessage("Provide the info about " + modelName + " LLM in following format: " + query);

    // Get the chat completions
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // Get the response from the AI
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        chatMessages,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Get the response from the custom connector
    var modelInfo = await huggingFaceConnector.SearchAsync(modelName);
    System.Console.WriteLine(modelInfo);

    // Print the chat completions
    ChatMessageContent? chatMessageContent = null;
    await foreach (var content in result)
    {
        System.Console.Write(content);
        if (chatMessageContent == null)
        {
            System.Console.Write("Assistant > ");
            chatMessageContent = new ChatMessageContent(
                content.Role ?? AuthorRole.Assistant,
                content.ModelId!,
                content.Content!,
                content.InnerContent,
                content.Encoding,
                content.Metadata);
        }
        else
        {
            chatMessageContent.Content += content;
        }
    }
    System.Console.WriteLine();

}
