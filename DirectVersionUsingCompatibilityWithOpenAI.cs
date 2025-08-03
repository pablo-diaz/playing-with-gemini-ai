using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using OpenAI.Chat;
using System.Linq;

namespace PlayingWithGeminiAI;

internal class DirectVersionUsingCompatibilityWithOpenAI
{
    public static async Task RunScenarios(string withApiKey)
    {
        var clientWithCompatibilityWithOpenAI = GetGeminiClient(withApiKey);

        Console.WriteLine("**********************************************************************");
        Console.WriteLine("Running scenarios with Gemini AI, calling directly the OpenAI endpoints that it exposes...");
        await RunBasicScenario(withClient: clientWithCompatibilityWithOpenAI);
        await RunScenarioWithTools(withClient: clientWithCompatibilityWithOpenAI);
    }

    private static ChatClient GetGeminiClient(string apiKey) =>
        new(
            model: "gemini-2.5-flash",
            credential: new(key: apiKey),
            options: new() { Endpoint = new(uriString: "https://generativelanguage.googleapis.com/v1beta/openai") });

    private static async Task RunBasicScenario(ChatClient withClient)
    {
        ChatMessage[] messages = [
            ChatMessage.CreateSystemMessage(content: "You are a helpful assistant."),
            ChatMessage.CreateUserMessage(content: "Say 'this is a test, using Google Gemini AI and its compatibility with OpenAI endpoints'"),
        ];

        ChatCompletion completion = await withClient.CompleteChatAsync(messages);

        PrintMessageFromAiAssistant(message: completion.Content[0].Text);
    }

    private static async Task RunScenarioWithTools(ChatClient withClient)
    {
        // https://github.com/openai/openai-dotnet#how-to-use-chat-completions-with-tools-and-function-calling

        var getNameTool = ChatTool.CreateFunctionTool(
            functionName: nameof(ToolsThatCanBeInvokedByLLMs.GetMyName),
            functionDescription: "Call this function when you need to know the name of someone you are talking to");

        var getAgeTool = ChatTool.CreateFunctionTool(
            functionName: nameof(ToolsThatCanBeInvokedByLLMs.GetMyAge),
            functionDescription: "Call this function when you need to know the age of someone you are talking to");

        List<ChatMessage> messages = [
            ChatMessage.CreateSystemMessage(content: "You are a kind assistant, that knows about geography. You always talk to people, referring to them by their proper names, which you can always get using the function (tools) you are provided with."),
            ChatMessage.CreateUserMessage(content: "Hi, what is the capital of France?"),
        ];

        ChatCompletionOptions options = new() { Tools = { getNameTool, getAgeTool } };

        var shouldContinue = true;
        do
        {
            ChatCompletion completion = await withClient.CompleteChatAsync(messages, options);

            switch (completion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    var messageContent = completion.Content[0].Text;
                    messages.Add(ChatMessage.CreateAssistantMessage(content: messageContent));
                    PrintMessageFromAiAssistant(message: messageContent);
                    shouldContinue = false;
                    break;

                case ChatFinishReason.ToolCalls:
                    if (completion.Content.Any())
                        messages.Add(ChatMessage.CreateAssistantMessage(contentParts: completion.Content));

                    foreach (var toolCall in completion.ToolCalls)
                    {
                        ProcessToolCall(completion, toolCall, messages);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Finish reason '{completion.FinishReason}' is not supported.");
            }

        } while (shouldContinue);
    }

    private static void ProcessToolCall(ChatCompletion completion, ChatToolCall toolCall, List<ChatMessage> messages)
    {
        switch (toolCall.FunctionName)
        {
            case nameof(ToolsThatCanBeInvokedByLLMs.GetMyName):
                messages.Add(ChatMessage.CreateFunctionMessage(functionName: toolCall.FunctionName, content: ToolsThatCanBeInvokedByLLMs.GetMyName()));
                break;

            case nameof(ToolsThatCanBeInvokedByLLMs.GetMyAge):
                messages.Add(ChatMessage.CreateFunctionMessage(functionName: toolCall.FunctionName, content: ToolsThatCanBeInvokedByLLMs.GetMyAge()));
                break;

            default:
                throw new NotSupportedException($"Tool '{toolCall.FunctionName}' is not supported.");
        }
    }

    private static void PrintMessageFromAiAssistant(string message)
    {
        Console.WriteLine($"[ASSISTANT]: {message}");
    }

}
