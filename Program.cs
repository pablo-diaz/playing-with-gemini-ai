using System;
using System.Text;
using System.Threading.Tasks;

namespace PlayingWithGeminiAI;

internal class Program
{
    static async Task Main(string[] args)
    {
        var apiKey = GetApiKey(args);
        
        await DirectVersionUsingCompatibilityWithOpenAI.RunScenarios(withApiKey: apiKey);
        await PlayingWithGeminiAI.RunScenarios(withApiKey: apiKey);
        await OpenAiCompatibilityUsingMsExtAi.RunScenarios(withApiKey: apiKey);
    }

    private static string GetApiKey(string[] args)
    {
        if(args.Length == 1) return args[0];

        var maybeApiKeySetAsEnvironmentVariable = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if(!string.IsNullOrEmpty(maybeApiKeySetAsEnvironmentVariable)) return maybeApiKeySetAsEnvironmentVariable;

        return PromptForUserInput(
            withPromptingUserMessage: "Please enter your Gemini API key",
            shouldHideWhatUserTypesIn: true);
    }

    private static string PromptForUserInput(string withPromptingUserMessage, bool shouldHideWhatUserTypesIn)
    {
        Console.Write($"{withPromptingUserMessage}: ");
        StringBuilder apiKeyTypingBuilder = new();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: shouldHideWhatUserTypesIn);  // true hides the key pressed
            if (key.Key != ConsoleKey.Enter)
            {
                apiKeyTypingBuilder.Append(key.KeyChar);

                if(shouldHideWhatUserTypesIn)
                    Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();

        return apiKeyTypingBuilder.ToString();
    }

}
