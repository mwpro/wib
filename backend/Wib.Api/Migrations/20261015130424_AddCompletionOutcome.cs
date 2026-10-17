using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wib.Api.Migrations
{
    /// <summary>
    /// Adds the completion outcome columns as nullable so that <see cref="Data.CompletionOutcomeBackfill"/>
    /// can fill existing rows before <see cref="MakeCompletionOutcomeRequired"/> makes them NOT NULL.
    /// </summary>
    public partial class AddCompletionOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DaysSincePrevious",
                table: "chore_completions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrgencyAtCompletion",
                table: "chore_completions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Create the composite index first: MySQL needs an index covering the ChoreId foreign key at all times.
            migrationBuilder.CreateIndex(
                name: "IX_chore_completions_ChoreId_CompletedAt",
                table: "chore_completions",
                columns: new[] { "ChoreId", "CompletedAt" });

            migrationBuilder.DropIndex(
                name: "IX_chore_completions_ChoreId",
                table: "chore_completions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_chore_completions_ChoreId",
                table: "chore_completions",
                column: "ChoreId");

            migrationBuilder.DropIndex(
                name: "IX_chore_completions_ChoreId_CompletedAt",
                table: "chore_completions");

            migrationBuilder.DropColumn(
                name: "DaysSincePrevious",
                table: "chore_completions");

            migrationBuilder.DropColumn(
                name: "UrgencyAtCompletion",
                table: "chore_completions");
        }
    }
}
