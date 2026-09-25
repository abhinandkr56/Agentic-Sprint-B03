namespace HelloLLM.Demos;

/// DEMO 5 — The API is stateless. "Memory" = you resending the conversation every time.
/// This is WHY context windows and token costs matter.
public static class D5_Memory
{
    public static async Task Run(Llm llm)
    {
        const string intro = "Hi! My name is Abhi and my favourite language is C#.";
        const string question = "What's my name and my favourite language?";

        Ui.Step("Attempt 1 — two separate requests (no history)");
        Console.WriteLine($"  request A: {intro}");
        var a = await llm.AskAsync(intro, maxTokens: 60);
        Ui.Dim($"  reply   A: {a.Choices[0].Message.Content.Trim()}");
        Console.WriteLine($"  request B: {question}");
        var b = await llm.AskAsync(question, maxTokens: 60);
        Ui.Write(ConsoleColor.Red, $"  reply   B: {b.Choices[0].Message.Content.Trim()}\n");

        Ui.Pause();

        Ui.Step("Attempt 2 — we send the whole conversation in ONE request");
        var history = new List<ChatMessage>
        {
            ChatMessage.User(intro),
            ChatMessage.Assistant(a.Choices[0].Message.Content),
            ChatMessage.User(question)
        };
        foreach (var m in history) Ui.Dim($"  [{m.Role}] {Trim(m.Content)}");

        var (c, _) = await llm.ChatAsync(new ChatRequest(llm.Model, history) { MaxTokens = 60 });
        Ui.Write(ConsoleColor.Green, $"  reply: {c.Choices[0].Message.Content.Trim()}\n");
        Ui.Usage(c.Usage);

        Ui.Good("\nThe model has no memory. The context window IS the memory — and you pay for it every turn.");
    }

    private static string Trim(string s) => s.Length > 70 ? s[..70].ReplaceLineEndings(" ") + "…" : s.ReplaceLineEndings(" ");
}
