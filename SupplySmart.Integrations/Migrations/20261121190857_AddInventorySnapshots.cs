using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplySmart.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    InventoryItemId = table.Column<int>(type: "int", nullable: false),
                    ContainerIdAtSnapshot = table.Column<int>(type: "int", nullable: false),
                    SnapshotDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    MinimumThresholdAtSnapshot = table.Column<int>(type: "int", nullable: false),
                    UnitCostAtSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpirationDateAtSnapshot = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusAtSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySnapshots_Containers_ContainerIdAtSnapshot",
                        column: x => x.ContainerIdAtSnapshot,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshots_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshots_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_ContainerIdAtSnapshot",
                table: "InventorySnapshots",
                column: "ContainerIdAtSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_InventoryItemId",
                table: "InventorySnapshots",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_OrganizationId",
                table: "InventorySnapshots",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_OrganizationId_InventoryItemId_SnapshotDateUtc",
                table: "InventorySnapshots",
                columns: new[] { "OrganizationId", "InventoryItemId", "SnapshotDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_SnapshotDateUtc",
                table: "InventorySnapshots",
                column: "SnapshotDateUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventorySnapshots");
        }
    }
}
