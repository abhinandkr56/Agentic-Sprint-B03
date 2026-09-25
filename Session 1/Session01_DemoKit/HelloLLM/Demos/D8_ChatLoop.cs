using System.Text;

namespace HelloLLM.Demos;

/// DEMO 8 (finale) — Everything together: system prompt + history + streaming = a chatbot.
/// ~30 lines of C#. No SDK. This is what "ChatGPT in your app" really is.
public static class D8_ChatLoop
{
    public static async Task Run(Llm llm)
    {
        var history = new List<ChatMessage>
        {
            ChatMessage.System("""
                You are DotNetBuddy, a friendly senior .NET mentor.
                Answer in at most 80 words. Use a tiny C# snippet when it helps.
                If the question is not about software, gently steer back to .NET.
                """)
        };

        Ui.Good("DotNetBuddy is online. Ask anything about .NET.  (type 'exit' to leave, 'reset' to clear memory)\n");

        while (true)
        {
            Ui.Write(ConsoleColor.Cyan, "you ▸ ");
            var input = Console.ReadLine();
            if (input is null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                history.RemoveRange(1, history.Count - 1);
                Ui.Dim("  (memory wiped — only the system prompt remains)\n");
                continue;
            }

            history.Add(ChatMessage.User(input));

            Ui.Write(ConsoleColor.Magenta, "bot ▸ ");
            var reply = new StringBuilder();
            await foreach (var piece in llm.StreamAsync(history, temperature: 0.5))
            {
                reply.Append(piece);
                Console.Write(piece);
            }
            Console.WriteLine();

            history.Add(ChatMessage.Assistant(reply.ToString()));
            var chars = history.Sum(m => m.Content.Length);
            Ui.Dim($"  [history: {history.Count} messages · ~{chars / 4} tokens resent next turn]\n");
        }
    }
}
