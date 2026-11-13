using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplySmart.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingInterval",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxItemsQuota",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxRoomsQuota",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalDateUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingInterval",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "MaxItemsQuota",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "MaxRoomsQuota",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "RenewalDateUtc",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Organizations");
        }
    }
}
