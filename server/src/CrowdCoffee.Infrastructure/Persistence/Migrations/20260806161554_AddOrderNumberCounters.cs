using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrowdCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderNumberCounters",
                columns: table => new
                {
                    DateKey = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNumberCounters", x => x.DateKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderNumberCounters");
        }
    }
}
