using System.Runtime.InteropServices;

namespace HoleBite;

// https://stackoverflow.com/questions/61779942/ansi-colors-and-writing-directly-to-console-output-c-sharp
public static class ConsoleAnsiUtils 
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static void Initialize() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            EnableAnsiEscapeSequencesOnWindows();
        }
    }

    private static void EnableAnsiEscapeSequencesOnWindows() {
        IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (handle == IntPtr.Zero) {
            throw new Exception("Cannot get standard output handle");
        }

        if (!GetConsoleMode(handle, out uint mode)) {
            throw new Exception("Cannot get console mode");
        }

        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        if (!SetConsoleMode(handle, mode)) {
            throw new Exception("Cannot set console mode");
        }
    }
}