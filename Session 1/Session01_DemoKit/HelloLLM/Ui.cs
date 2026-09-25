namespace HelloLLM;

/// Small console helpers so the demos look good on a projector.
public static class Ui
{
    public static void Banner(LlmConfig c)
    {
        Console.Clear();
        Write(ConsoleColor.Magenta, """
          ╔══════════════════════════════════════════════╗
          ║   HELLO, LLM  ·  Session 01  ·  .NET + AI    ║
          ╚══════════════════════════════════════════════╝
        """);
        Dim($"  model: {c.Model}   endpoint: {c.BaseUrl}\n");
    }

    public static void Title(string t) { Write(ConsoleColor.Cyan, $"▶ {t}\n{new string('─', Math.Min(t.Length + 2, 70))}\n"); }
    public static void Step(string t) => Write(ConsoleColor.Yellow, $"\n● {t}\n");
    public static void Dim(string t) => Write(ConsoleColor.Gray, t + "\n");
    public static void Good(string t) => Write(ConsoleColor.Green, t + "\n");
    public static void Error(string t) => Write(ConsoleColor.Red, "✖ " + t + "\n");
    public static void Answer(string t) => Write(ConsoleColor.White, t + "\n");

    public static void Write(ConsoleColor color, string text)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = old;
    }

    public static void Pause(string msg = "Press Enter to continue...")
    {
        if (Console.IsInputRedirected) return;
        Dim(msg);
        Console.ReadLine();
    }

    public static void Usage(Usage? u, TimeSpan? elapsed = null)
    {
        if (u is null) return;
        var t = elapsed is null ? "" : $"  ·  {elapsed.Value.TotalMilliseconds:N0} ms round-trip";
        Dim($"  tokens → prompt: {u.PromptTokens}  completion: {u.CompletionTokens}  total: {u.TotalTokens}{t}");
    }
}
