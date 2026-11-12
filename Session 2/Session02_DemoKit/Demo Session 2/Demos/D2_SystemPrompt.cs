namespace PromptLab.Demos;

/// <summary>
/// Demo 2 · The system prompt is the steering wheel.
/// One user question, three system prompts, three very different answers.
/// </summary>
public static class D2_SystemPrompt
{
    const string Question = "Is `async void` ever OK in C#? answer below 1000 words";

    static readonly (string Name, string? System)[] Personas =
    [
        ("No system prompt", null),
        ("Strict reviewer", "You are a strict senior .NET code reviewer. Answer in at most 2 sentences. Start with Yes or No."),
        ("Mentor for a fresher", "You mentor a developer in their first month. Explain with one everyday Indian analogy (cricket, trains, chai). Max 3 sentences. No code."),
    ];

    public static async Task Run()
    {
        Ui.Title("Demo 2 · Same question, three system prompts");
        Ui.Label($"User asks: {Question}");
        Ui.Pause();

        foreach (var (name, system) in Personas)
        {
            Ui.Label(name);
            if (system is not null) Ui.Dim("   system: " + system);
            var r = await Llm.AskAsync(system, Question, temperature: 0.5, maxTokens:2000);
            Ui.Box(r.Content, system is null ? ConsoleColor.Gray : ConsoleColor.Green);
            Ui.Dim("   " + r.Stats);
            Ui.Pause();
        }
        Ui.Good("Same model, same question. The system prompt decided length, tone and audience.");
    }
}
