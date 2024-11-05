namespace HoleBite;

public static class ThreadHelpers
{
    public static void Kill(this Thread thread)
    {
        if (thread.IsAlive)
            thread.Abort();
    }
}