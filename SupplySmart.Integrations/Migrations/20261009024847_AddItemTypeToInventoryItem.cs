using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplySmart.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemTypeToInventoryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InventoryItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "InventoryItems");
        }
    }
}
