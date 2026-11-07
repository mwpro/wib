namespace Wib.Domain.Chores;

/// <summary>
/// What removing a chore from the backlog amounts to, as decided by <see cref="Chore.Remove"/>.
/// </summary>
public enum ChoreRemoval
{
    /// <summary>The chore was never completed, so nothing refers to it: the caller deletes it outright.</summary>
    Delete,

    /// <summary>The chore has completions, so it was archived to keep its history and review entries.</summary>
    Archived
}
