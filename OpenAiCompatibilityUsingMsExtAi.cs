using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace PlayingWithGeminiAI;

internal class OpenAiCompatibilityUsingMsExtAi
{
    public static async Task RunScenarios(string withApiKey)
    {
        using var client = GetClient(withApiKey: withApiKey);

        Console.WriteLine("**********************************************************************");
        Console.WriteLine("Running scenarios with Gemini AI, using the OpenAI compability endpoints it exposes and also Microsoft Extensions AI library ...");
        await RunBasicScenario(client);
        await RunScenarioWithTools(client);
    }

    private static async Task RunBasicScenario(IChatClient client)
    {
        var response = await client.GetResponseAsync(
            options: new() { Instructions = "You are a helpful assistant." },
            chatMessage: "Say 'this is a test, using Google Gemini AI, its OpenAI compatibility endpoint and Microsoft.Extensions.AI library'");

        PrintMessageFromAiAssistant(message: response.Text);
    }

    private static async Task RunScenarioWithTools(IChatClient client)
    {
        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.GetMyName, name: nameof(ToolsThatCanBeInvokedByLLMs.GetMyName)),
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.GetMyAge, name: nameof(ToolsThatCanBeInvokedByLLMs.GetMyAge))
            ]
        };

        List<ChatMessage> messages = [
            new(ChatRole.System, """
                - You are a kind assistant, that knows a lot about geography.
                - You always talk to people, using their proper name, which you **MUST** obtain by using the functions (tools) you are provided with.
                - Please first get the user's name before answering any questions, and then use it to greet them and answer their questions.
            """),

            new(ChatRole.User, "What is the capital of the department of Guajira, in Colombia?")
        ];

        using var loggerFactory = GetLoggerFactory();
        var clientEnhancedForFunctionCalling = EnhanceClientToAllowFunctionCalling(fromClient: client, withLoggerFactory: loggerFactory);

        var response = await clientEnhancedForFunctionCalling.GetResponseAsync(messages, options);

        PrintMessageFromAiAssistant(message: response.Text);
    }

    private static IChatClient GetClient(string withApiKey) =>
        new OpenAI.Chat.ChatClient(
            model: "gemini-2.5-flash",
            credential: new(key: withApiKey),
            options: new() { Endpoint = new(uriString: "https://generativelanguage.googleapis.com/v1beta/openai") })
        .AsIChatClient();

    private static IChatClient EnhanceClientToAllowFunctionCalling(IChatClient fromClient, ILoggerFactory withLoggerFactory) =>
        new ChatClientBuilder(fromClient)
            .UseFunctionInvocation(loggerFactory: withLoggerFactory)
            .Build();

    private static ILoggerFactory GetLoggerFactory() =>
        LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

    private static void PrintMessageFromAiAssistant(string message)
    {
        Console.WriteLine($"[ASSISTANT]: {message}");
    }

}
