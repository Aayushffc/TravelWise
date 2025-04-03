using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class removingStatusThingsFromDeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ExpirationReason",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ExpiredBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "SuspendedBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "Deals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpirationReason",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpiredBy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedBy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
