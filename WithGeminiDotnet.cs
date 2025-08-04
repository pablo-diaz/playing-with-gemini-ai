using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using GeminiDotnet;
using GeminiDotnet.Extensions.AI;

namespace PlayingWithGeminiAI;

internal class PlayingWithGeminiAI
{
    private sealed record ArticleInfo(string Title, string ShortDescription)
    {
        public void Print()
        {
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Description: {ShortDescription}");
        }
    }

    private sealed record ArticleIdeas(string TopicOfAllArticles, ArticleInfo[] Articles)
    {
        public void Print()
        {
            Console.WriteLine($"Here are {Articles.Length} ideas of articles you could write about {TopicOfAllArticles}:");
            foreach (var article in Articles)
            {
                article.Print();
                Console.WriteLine();
            }
        }
    }

    public static async Task RunScenarios(string withApiKey)
    {
        using var client = GetClient(withApiKey: withApiKey);

        Console.WriteLine("**********************************************************************");
        Console.WriteLine("Running scenarios with Gemini AI, using GeminiDotnet which implements Microsoft.Extensions.AI ...");
        
        await RunBasicScenario(client);
        await RunScenarioWithTools(client);
        await RunScenarioAboutStructuredOutput(client);
        await RunScenarioForWorkflow(client);
    }

    private static async Task RunBasicScenario(IChatClient client)
    {
        var response = await client.GetResponseAsync(
            options: new() { Instructions = "You are a helpful assistant." },
            chatMessage: "Say 'this is a test, using Google Gemini AI and GeminiDotnet nuget'");

        PrintMessageFromAiAssistant(message: response.Text);
    }

    private static async Task RunScenarioWithTools(IChatClient client)
    {
        var options = new ChatOptions {
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

            new(ChatRole.User, "What is the capital of the department of Vaupés, in Colombia?")
        ];

        using var loggerFactory = GetLoggerFactory();
        var clientEnhancedForFunctionCalling = EnhanceClientToAllowFunctionCalling(fromClient: client, withLoggerFactory: loggerFactory);

        var response = await clientEnhancedForFunctionCalling.GetResponseAsync(messages, options);

        PrintMessageFromAiAssistant(message: response.Text);
    }

    private static async Task RunScenarioAboutStructuredOutput(IChatClient client)
    {
        List<ChatMessage> messages = [
            new(ChatRole.System, """
                You are a helpful assistant that can provide people you talk to, with ideas about how writing good articles about DotNet Core and C sharp only.
            """),

            new(ChatRole.User, """
                - First think about a hot trending topic I could write some articles about.
                - Then please provide 3 ideas of articles I could write for that topic you chose in the previous step, with their respective short descriptions (of no more than 15 words)
            """)
        ];

        var response = await client.GetResponseAsync<ArticleIdeas>(messages);
        if (response.TryGetResult(out var ideasAboutArticle))
            ideasAboutArticle.Print();
    }

    private static async Task RunScenarioForWorkflow(IChatClient client)
    {
        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.GetOrderInformation, name: nameof(ToolsThatCanBeInvokedByLLMs.GetOrderInformation)),
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.SendOrderToBeReviewedByManager, name: nameof(ToolsThatCanBeInvokedByLLMs.SendOrderToBeReviewedByManager)),
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.PrintOrder, name: nameof(ToolsThatCanBeInvokedByLLMs.PrintOrder)),
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.LogReasonWhyStepCannotBePerformed, name: nameof(ToolsThatCanBeInvokedByLLMs.LogReasonWhyStepCannotBePerformed)),
                AIFunctionFactory.Create(method: ToolsThatCanBeInvokedByLLMs.CancelOrder, name: nameof(ToolsThatCanBeInvokedByLLMs.CancelOrder))
            ]
        };

        var agentInstructions = """
                You are a helpful assistant that helps people to get their input, enhance its data with the tools (functions) you are provided with, and then return a structured output.
                
                These are the steps you **must** follow:
                - First you take an input from the user, which is a request in Spanish.
                - Then you use the tools (functions) provided to you, to get more context regarding what the user requested.
                - You should also use the tools (functions) provided to you, to perform the actions the user requested.
                - Try to avoid skipping any of the steps the user requested you to perform.
                - Do not answer the user until you have all the information you need to provide a complete answer.
                - If any of the steps you are asked to perform cannot be done, you must log the technical reason why it cannot be done, using the `LogReasonWhyStepCannotBePerformed` function. Please try to be as specific as possible with this reason.

                Please respond with a very natural language answer in Spanish:
                - Use very informal expressions please, that summarizes the steps you took to get the information you are providing to the user
                - If possible, please use Colombian slangs, such as Listo, Pillar, Parce, Chévere, Bacano, etc.
                - Also, if possible, please use emojis to make the answer more friendly and engaging
            """;

        string[] userMessagesToTriggerEachWorkflow = [
            """
                Hola, me ayudas porfa con lo siguiente:
                1. Quisiera saber cómo va mi Orden XYZ123
                2. Luego envíala a ser revisada por mi jefe
                3. Y por último imprímela

                Muchas gracias
            """,

            """
                Por favor averigua el estado actual de mi orden ABC00987, luego porfa la Cancelas y por último, **solo luego que la canceles**, porfa imprímela
            """
        ];

        var clientEnhancedForFunctionCalling = EnhanceClientToAllowFunctionCalling(fromClient: client);
        foreach(var userMessage in userMessagesToTriggerEachWorkflow)
        {
            List<ChatMessage> messages = [
                new(ChatRole.System, agentInstructions),
                new(ChatRole.User, userMessage)
            ];

            var response = await clientEnhancedForFunctionCalling.GetResponseAsync(messages, options);

            PrintMessageFromAiAssistant(message: response.Text);
            Console.WriteLine("------------------------------------------------------");
        }
    }

    private static IChatClient GetClient(string withApiKey) =>
        new GeminiChatClient(options: new() {
            ApiKey = withApiKey,
            ApiVersion = GeminiApiVersions.V1Beta,
            ModelId = "gemini-2.5-flash",
        });

    private static IChatClient EnhanceClientToAllowFunctionCalling(IChatClient fromClient, ILoggerFactory withLoggerFactory = null) =>
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
