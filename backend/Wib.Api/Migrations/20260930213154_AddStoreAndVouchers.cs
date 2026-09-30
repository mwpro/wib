using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wib.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAndVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCost = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedByMemberId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reward_items_members_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RewardItemId = table.Column<int>(type: "int", nullable: false),
                    TitleSnapshot = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCostSnapshot = table.Column<int>(type: "int", nullable: false),
                    OwnedByMemberId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PurchasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vouchers_members_OwnedByMemberId",
                        column: x => x.OwnedByMemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vouchers_reward_items_RewardItemId",
                        column: x => x.RewardItemId,
                        principalTable: "reward_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reward_items_CreatedByMemberId",
                table: "reward_items",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_OwnedByMemberId",
                table: "vouchers",
                column: "OwnedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_RewardItemId",
                table: "vouchers",
                column: "RewardItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropTable(
                name: "reward_items");
        }
    }
}
