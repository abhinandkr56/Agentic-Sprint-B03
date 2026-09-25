namespace HelloLLM.Demos;

/// DEMO 4 — An LLM predicts the next token from a probability distribution.
/// Temperature controls how adventurous the sampling is.
public static class D4_Temperature
{
    private const string Prompt =
        "Suggest a name for a startup that builds AI agents in .NET. Reply with ONLY the name, nothing else.";

    public static async Task Run(Llm llm)
    {
        Ui.Dim($"Prompt: \"{Prompt}\"");

        foreach (var temperature in new[] { 0.7, 1.5 })
        {
            Ui.Step($"temperature = {temperature}  (3 runs)");
            for (int i = 1; i <= 3; i++)
            {
                var r = await llm.AskAsync(Prompt, temperature: temperature, maxTokens: 20);
                
                Console.WriteLine(r.Choices[0]);
                Console.WriteLine($"  run {i}: {r.Choices[0].Message.Content.Trim()}");
            }
        }

        Ui.Good("\nLow temperature → consistent (good for JSON, classification, code).");
        Ui.Good("High temperature → creative (good for brainstorming, names, copy).");
        Ui.Dim("Even at 0 it isn't a guarantee — never unit-test for an exact string.");
    }
}
