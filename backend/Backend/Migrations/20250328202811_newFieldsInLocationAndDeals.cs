using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class newFieldsInLocationAndDeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Continent",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Deals",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "AgencyId",
                table: "Deals",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyName",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "Availability",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Deals",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingCount",
                table: "Deals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categories",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "Deals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Continent",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedServices",
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
                name: "FeaturedUntil",
                table: "Deals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Highlights",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncludedServices",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Deals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstantBooking",
                table: "Deals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLastMinuteDeal",
                table: "Deals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBooked",
                table: "Deals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastClicked",
                table: "Deals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewed",
                table: "Deals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Deals",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Deals",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxGroupSize",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinGroupSize",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Deals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RefundPolicy",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RelevanceScore",
                table: "Deals",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Restrictions",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reviews",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchKeywords",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seasons",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCharge",
                table: "Deals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
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

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Deals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Deals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Deals_AgencyId",
                table: "Deals",
                column: "AgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_AspNetUsers_AgencyId",
                table: "Deals",
                column: "AgencyId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_AspNetUsers_AgencyId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_AgencyId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Continent",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "AgencyName",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "BookingCount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Continent",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ExcludedServices",
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
                name: "FeaturedUntil",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Highlights",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IncludedServices",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IsInstantBooking",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IsLastMinuteDeal",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastBooked",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastClicked",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastViewed",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "MaxGroupSize",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "MinGroupSize",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RefundPolicy",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RelevanceScore",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Restrictions",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Reviews",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "SearchKeywords",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Seasons",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ServiceCharge",
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

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Deals");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Deals",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
