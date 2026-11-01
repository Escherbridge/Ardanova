namespace ArdaNova.Domain.Models.Entities;

/// <summary>Canonical values for <see cref="Project.mediaPaths"/>.</summary>
public static class ProjectMediaPaths
{
    /// <summary>A project with no uploaded media; the column is never null so readers never branch on it.</summary>
    public const string Empty = "[]";
}
