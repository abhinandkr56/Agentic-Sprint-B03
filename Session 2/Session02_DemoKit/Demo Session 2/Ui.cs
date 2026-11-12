namespace PromptLab;

/// <summary>Tiny console helpers so the demos read well on a projector.</summary>
public static class Ui
{
    public static void Title(string text)
    {
        Console.WriteLine();
        Write(ConsoleColor.Magenta, "══ " + text + " " + new string('═', Math.Max(3, 60 - text.Length)));
        Console.WriteLine();
    }

    public static void Label(string text) => Write(ConsoleColor.Cyan, "▸ " + text);
    public static void Dim(string text) => Write(ConsoleColor.Blue, text);
    public static void Good(string text) => Write(ConsoleColor.Green, "✓ " + text);
    public static void Bad(string text) => Write(ConsoleColor.Red, "✗ " + text);
    public static void Warn(string text) => Write(ConsoleColor.Yellow, "! " + text);

    public static void Box(string text, ConsoleColor color = ConsoleColor.White)
    {
        foreach (var line in text.Split('\n'))
            Write(color, "  │ " + line.TrimEnd('\r'));
    }

    public static void Inline(ConsoleColor color, string text)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = old;
    }

    public static void Write(ConsoleColor color, string text)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = old;
    }

    /// <summary>Pause so the presenter can talk. Skipped when output is redirected (CI, piping).</summary>
    public static void Pause(string msg = "press Enter to continue")
    {
        if (Console.IsInputRedirected) return;
        Dim($"   … {msg}");
        Console.ReadLine();
    }

    /// <summary>Fixed-width truncation for tables.</summary>
    public static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    public static string Clip(string s, int max = 400) =>
        s.Length <= max ? s : s[..max] + $" … [+{s.Length - max} chars]";
}
