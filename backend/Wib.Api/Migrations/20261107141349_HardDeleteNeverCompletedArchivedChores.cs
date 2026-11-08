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
            migrationBuilder.Sql("""
                DELETE FROM `chores`
                WHERE `IsArchived` = 1
                  AND NOT EXISTS (SELECT 1 FROM `chore_completions` cc WHERE cc.`ChoreId` = `chores`.`Id`);
                """);

            migrationBuilder.Sql("""
                DELETE FROM `tags`
                WHERE NOT EXISTS (SELECT 1 FROM `chore_tags` ct WHERE ct.`TagId` = `tags`.`Id`);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
