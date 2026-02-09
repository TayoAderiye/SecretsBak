using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Helpers;


public static class DotnetUserSecrets
{
    public static string GetUserSecretsId(string csprojPath)
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException("Project file not found.", csprojPath);

        var xdoc = XDocument.Load(csprojPath);
        var id = xdoc.Descendants()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "UserSecretsId", StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();

        return string.IsNullOrWhiteSpace(id)
            ? throw new InvalidOperationException("UserSecretsId not found in the project file. Add <UserSecretsId>...</UserSecretsId> to the .csproj.")
            : id;
    }

    public static void SetUserSecretsId(string csprojPath, string secretsId)
    {
        var xdoc = XDocument.Load(csprojPath);

        var userSecrets = xdoc.Descendants()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "UserSecretsId", StringComparison.OrdinalIgnoreCase));

        if (userSecrets is not null)
        {
            userSecrets.Value = secretsId;
        }
        else
        {
            var pg = xdoc.Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase));

            if (pg is null)
            {
                var project = xdoc.Descendants()
                    .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Project", StringComparison.OrdinalIgnoreCase));

                if (project is null)
                    throw new InvalidOperationException("Invalid .csproj structure: missing <Project> root.");

                pg = new XElement(project.Name.Namespace + "PropertyGroup");
                project.Add(pg);
            }

            pg.Add(new XElement(pg.Name.Namespace + "UserSecretsId", secretsId));
        }

        xdoc.Save(csprojPath);
    }

    public static string GetSecretsJsonPath(string secretsId)
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Microsoft", "UserSecrets", secretsId, "secrets.json");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".microsoft", "usersecrets", secretsId, "secrets.json");
    }
}