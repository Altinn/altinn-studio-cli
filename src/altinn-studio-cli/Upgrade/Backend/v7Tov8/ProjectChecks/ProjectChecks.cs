using System.Xml.Linq;

namespace Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.ProjectChecks;

/// <summary>
/// Checks the project file for unsupported versions
/// </summary>
public class ProjectChecks
{
    private XDocument doc;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectChecks"/> class.
    /// </summary>
    /// <param name="projectFilePath"></param>
    public ProjectChecks(string projectFilePath)
    {
        var xmlString = File.ReadAllText(projectFilePath);
        doc = XDocument.Parse(xmlString);
    }

    /// <summary>
    /// Verifies that the project is using supported versions of Altinn.App.Api and Altinn.App.Core
    /// </summary>
    /// <returns></returns>
    public bool SupportedSourceVersion()
    {
        var altinnAppCoreElements = GetAltinnAppCoreElement();
        var altinnAppApiElements = GetAltinnAppApiElement();
        if (altinnAppCoreElements == null || altinnAppApiElements == null)
        {
            return false;
        }

        if (altinnAppApiElements.Select(apiElement => apiElement.Attribute("Version")?.Value).Any(altinnAppApiVersion => !SupportedSourceVersion(altinnAppApiVersion)))
        {
            return false;
        }

        return altinnAppCoreElements.Select(coreElement => coreElement.Attribute("Version")?.Value).All(altinnAppCoreVersion => SupportedSourceVersion(altinnAppCoreVersion));

    }
    
    private List<XElement>? GetAltinnAppCoreElement()
    {
        return doc.Root?.Elements("ItemGroup").Elements("PackageReference").Where(x => x.Attribute("Include")?.Value == "Altinn.App.Core").ToList();
    }

    private List<XElement>? GetAltinnAppApiElement()
    {
        return doc.Root?.Elements("ItemGroup").Elements("PackageReference").Where(x => x.Attribute("Include")?.Value == "Altinn.App.Api").ToList();
    }

    /// <summary>
    /// Check that version is >=7.0.0
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    private bool SupportedSourceVersion(string? version)
    {
        if (version == null)
        {
            return false;
        }

        var versionParts = version.Split('.');
        if (versionParts.Length < 3)
        {
            return false;
        }

        if (int.TryParse(versionParts[0], out int major))
        {
            if (major >= 7)
            {
                return true;
            }
        }

        return false;
    }
}
