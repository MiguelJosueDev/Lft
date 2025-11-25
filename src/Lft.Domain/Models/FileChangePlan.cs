namespace Lft.Domain.Models;

public enum ChangeType
{
    Create,
    Modify,
    Skip // For when file exists and we don't overwrite/modify
}

public record FileChangePlan(
    string Path,
    string OldContent,
    string NewContent,
    ChangeType Type
);
