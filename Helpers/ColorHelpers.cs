namespace HoleBite;

public class ColorHelpers
{
    public static string Color(string text, int r, int g, int b)
    {
        const string esc = "\x1b";
        return $"{esc}[38;2;{r};{g};{b}m{text}{esc}[0m";
    }

    public static string Color(string text, ConsoleColor color)
    {
        const string esc = "\x1b";
        
        int c = color switch
        {
            ConsoleColor.Black => 30,
            ConsoleColor.Red => 31,
            ConsoleColor.Green => 32,
            ConsoleColor.Yellow => 33,
            ConsoleColor.Blue => 34,
            ConsoleColor.Magenta => 35,
            ConsoleColor.Cyan => 36,
            ConsoleColor.White => 37,
            _ => 37
        };
        
        return $"{esc}[{c}m{text}{esc}[0m";
    }
}