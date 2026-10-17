using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Wib.Domain.Chores;

namespace Wib.Api.Data;

// TEMPORARY: remove (together with its call in DatabaseMigrator) after all environments
// have migrated past MakeCompletionOutcomeRequired. See ADR 0005.
//
// Fills DaysSincePrevious and UrgencyAtCompletion for completions recorded before outcomes
// were snapshotted, judging them by each chore's current cadence. Works on raw SQL because
// the entity model declares both columns NOT NULL while the database still holds NULLs.
public static class CompletionOutcomeBackfill
{
    public const string RunAfterMigration = "20261015130424_AddCompletionOutcome";

    public static int Run(WibDbContext context)
    {
        var rows = ReadRows(context);
        var updated = 0;

        foreach (var chore in rows.GroupBy(r => r.ChoreId))
        {
            DateTime? previousCompletedAt = null;

            foreach (var row in chore.OrderBy(r => r.CompletedAt).ThenBy(r => r.Id))
            {
                if (row.NeedsBackfill)
                {
                    var outcome = FreshnessCalculator.JudgeCompletion(
                        previousCompletedAt ?? row.ChoreCreatedAt, row.CompletedAt, row.CadenceDays);

                    updated += context.Database.ExecuteSql(
                        $"UPDATE chore_completions SET DaysSincePrevious = {outcome.DaysSincePrevious}, UrgencyAtCompletion = {outcome.Urgency.ToString()} WHERE Id = {row.Id}");
                }

                previousCompletedAt = row.CompletedAt;
            }
        }

        return updated;
    }

    private static List<BackfillRow> ReadRows(WibDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT cc.Id, cc.ChoreId, cc.CompletedAt, c.CreatedAt, c.CadenceDays, cc.DaysSincePrevious IS NULL
                FROM chore_completions cc
                JOIN chores c ON c.Id = cc.ChoreId
                WHERE cc.ChoreId IN (SELECT ChoreId FROM chore_completions WHERE DaysSincePrevious IS NULL)
                """;

            using var reader = command.ExecuteReader();
            var rows = new List<BackfillRow>();
            while (reader.Read())
            {
                rows.Add(new BackfillRow(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    AsUtc(reader, 2),
                    AsUtc(reader, 3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Convert.ToBoolean(reader.GetValue(5))));
            }
            return rows;
        }
        finally
        {
            if (shouldClose) connection.Close();
        }
    }

    private static DateTime AsUtc(DbDataReader reader, int ordinal) =>
        DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);

    private sealed record BackfillRow(
        int Id, int ChoreId, DateTime CompletedAt, DateTime ChoreCreatedAt, int? CadenceDays, bool NeedsBackfill);
}
