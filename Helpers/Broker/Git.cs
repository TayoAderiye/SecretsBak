namespace Helpers.Broker;

public static class Git
{
    public static void Ensure()
    {
        Proc.Run("git", "--version");
    }

    public static void Clone(string repoUrl, string folder)
        => Proc.Run("git", $"clone {repoUrl} \"{folder}\"");

    public static void CheckoutNewBranch(string dir, string branch)
        => Proc.Run("git", $"checkout -b {branch}", dir);

    public static void AddCommit(string dir, string message)
    {
        Proc.Run("git", "add -A", dir);
        Proc.Run("git", $"commit -m \"{message}\"", dir);
    }

    public static void PushSetUpstream(string dir, string remote, string branch)
        => Proc.Run("git", $"push -u {remote} {branch}", dir);
}