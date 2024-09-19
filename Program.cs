// Import packages
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;

string apiKey = Environment.GetEnvironmentVariable("OPENAPI_API_KEY");
// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-3.5-turbo", apiKey);

// Add enterprise components
// builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Add a plugin (the LightsPlugin class is defined below)
kernel.Plugins.AddFromType<ZshPlugin>("zsh");
 #pragma warning disable SKEXP0001
// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
 #pragma warning disable SKEXP0001
// Create a history store the conversation
var history = new ChatHistory();

// Initiate a back-and-forth chat
string? userInput;
do {
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    history.AddUserMessage(userInput);

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);


public class ZshPlugin
{
    [KernelFunction("execute_zsh_command")]
    [Description("executes a command on zsh like ls, git, grep")]
    [return: Description("the output of the command that executed")]
    public async Task<string> ExecuteZshCommand(string command, List<string> args)
    {
 // Combine the command and arguments into a single shell command
        string fullCommand = command + " " + string.Join(" ", args);

        try
        {
            // Execute the command using a system process
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/zsh", // Adjust based on your environment (bash, cmd, etc.)
                Arguments = $"-c \"{fullCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            Console.WriteLine($"Executing {fullCommand}");

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                // Return output or error message
                if (!string.IsNullOrEmpty(error))
                {
                    return $"Error: {error}";
                }

                return output;
            }
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }

}



// public async Task<string> ExecuteCommand(string command, List<string> args)
//     {
       
//     }