using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrowdCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_StaffUsers_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "StaffUsers",
                columns: new[] { "Id", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { new Guid("3a242507-350f-e787-f5cb-0466f1d786cf"), true, "$2a$11$.2ZqXoDJUnTFSypYTSAG1uM4ANZ6jozkP6XyZBBb3JH9/13jxCwoq", "Barista", "barista1" },
                    { new Guid("4548cce5-feeb-af60-e8ed-6973067e0f7e"), true, "$2a$11$6TAyXfMx..MMUHVpUCqr7Oey6cc4SVJ4Lga2nCotjhWdy9PJf4d0C", "Manager", "manager1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_StaffUserId",
                table: "RefreshTokens",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DeleteData(
                table: "StaffUsers",
                keyColumn: "Id",
                keyValue: new Guid("3a242507-350f-e787-f5cb-0466f1d786cf"));

            migrationBuilder.DeleteData(
                table: "StaffUsers",
                keyColumn: "Id",
                keyValue: new Guid("4548cce5-feeb-af60-e8ed-6973067e0f7e"));
        }
    }
}
