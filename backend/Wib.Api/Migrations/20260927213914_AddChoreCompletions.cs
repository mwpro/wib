using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wib.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChoreCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chore_completions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChoreId = table.Column<int>(type: "int", nullable: false),
                    CompletedByMemberId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chore_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chore_completions_chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chore_completions_members_CompletedByMemberId",
                        column: x => x.CompletedByMemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_chore_completions_ChoreId",
                table: "chore_completions",
                column: "ChoreId");

            migrationBuilder.CreateIndex(
                name: "IX_chore_completions_CompletedByMemberId",
                table: "chore_completions",
                column: "CompletedByMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chore_completions");
        }
    }
}
