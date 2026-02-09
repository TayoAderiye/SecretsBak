namespace Helpers.Broker;

public static class Gh
{
    public static void Ensure()
    {
        try
        {
            Proc.Run("gh", "--version");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "GitHub CLI 'gh' is required but was not found on your PATH.\n" +
                "Install:\n" +
                "  macOS: brew install gh\n" +
                "  Windows: winget install GitHub.cli\n" +
                "  Linux: https://github.com/cli/cli#installation\n" +
                "Then run:\n" +
                "  gh auth login\n",
                ex);
        }

        // Confirm auth
        try
        {
            Proc.Run("gh", "auth status");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "You are not authenticated with GitHub CLI.\n" +
                "Run:\n" +
                "  gh auth login\n",
                ex);
        }
    }

    public static void WorkflowDispatch(string ownerRepo, string workflowFile, string @ref, Dictionary<string,string> inputs)
    {
        // gh api -X POST repos/{ownerRepo}/actions/workflows/{workflowFile}/dispatches -f ref=main -f inputs[action]=pull ...
        var args = $"api -X POST repos/{ownerRepo}/actions/workflows/{workflowFile}/dispatches -f ref={@ref}";
        foreach (var kv in inputs)
            args += $" -f inputs[{kv.Key}]={Escape(kv.Value)}";

        Proc.Run("gh", args);
    }

    public static string Api(string args, string? workDir = null) => Proc.Run("gh", "api " + args, workDir);

    private static string Escape(string s) => s.Replace("\"", "\\\"");
}