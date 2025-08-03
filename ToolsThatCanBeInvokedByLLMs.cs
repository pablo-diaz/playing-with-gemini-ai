using System;
using System.ComponentModel;

namespace PlayingWithGeminiAI;

internal static class ToolsThatCanBeInvokedByLLMs
{
    [Description("Retrieve the proper name of the user you are talking to")]
    public static string GetMyName()
    {
        Console.WriteLine(" ------> Calling GetMyName function");
        return "Carlos Estefano Garcia";
    }

    [Description("Retrieve the age of the user you are talking to")]
    public static string GetMyAge()
    {
        Console.WriteLine(" ------> Calling GetMyAge function");
        return "42";
    }

}
