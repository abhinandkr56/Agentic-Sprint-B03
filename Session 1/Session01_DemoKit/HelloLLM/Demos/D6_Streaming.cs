using System.Diagnostics;

namespace HelloLLM.Demos;

/// DEMO 6 — Streaming with Server-Sent Events. Same endpoint, "stream": true.
/// Teaching point: users feel speed from time-to-first-token, not total time.
public static class D6_Streaming
{
    public static async Task Run(Llm llm)
    {
        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are a storyteller for software developers."),
            ChatMessage.User("Tell a 120-word story about a C# developer whose code review was done by an AI.")
        };

        Ui.Step("Streaming... (watch the words arrive)");
        var sw = Stopwatch.StartNew();
        TimeSpan? firstToken = null;
        int chunks = 0;

        await foreach (var piece in llm.StreamAsync(messages, temperature: 0.8))
        {
            firstToken ??= sw.Elapsed;
            chunks++;
            Console.Write(piece);
        }
        sw.Stop();

        Console.WriteLine();
        Ui.Dim($"\n  time to first token: {firstToken?.TotalMilliseconds:N0} ms   total: {sw.ElapsedMilliseconds:N0} ms   chunks: {chunks}");
        Ui.Good("Each chunk arrived as a line:  data: {\"choices\":[{\"delta\":{\"content\":\"...\"}}]}");
        Ui.Good("The stream ends with:          data: [DONE]");
    }
}
