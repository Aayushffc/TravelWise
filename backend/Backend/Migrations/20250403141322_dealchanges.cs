using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class dealchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_AspNetUsers_AgencyId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_AgencyId",
                table: "Deals");

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
                name: "AverageRating",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Categories",
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
                name: "Duration",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "EndPoint",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ExcludedServices",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Highlights",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IncludedServices",
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
                name: "Region",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Reviews",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ServiceCharge",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Deals");

            migrationBuilder.RenameColumn(
                name: "StartPoint",
                table: "Deals",
                newName: "Headlines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Headlines",
                table: "Deals",
                newName: "StartPoint");

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

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Deals",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categories",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "Duration",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndPoint",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedServices",
                table: "Deals",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "Region",
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

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCharge",
                table: "Deals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Deals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

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
    }
}
