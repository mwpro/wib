using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wib.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardDeleteNeverCompletedArchivedChores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deleting a never-completed chore now removes it outright (see ADR 0002), so drop the ones archived
            // before that rule. Their chore_tags rows cascade.
            migrationBuilder.Sql("""
                DELETE FROM `chores`
                WHERE `IsArchived` = 1
                  AND NOT EXISTS (SELECT 1 FROM `chore_completions` cc WHERE cc.`ChoreId` = `chores`.`Id`);
                """);

            // Then the tags no chore uses any more, including those left behind by earlier edits.
            migrationBuilder.Sql("""
                DELETE FROM `tags`
                WHERE NOT EXISTS (SELECT 1 FROM `chore_tags` ct WHERE ct.`TagId` = `tags`.`Id`);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted rows cannot be brought back; rolling back leaves the data as it is.
        }
    }
}
