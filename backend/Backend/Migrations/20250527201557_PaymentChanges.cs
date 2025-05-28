using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class PaymentChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgencyId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BookingCustomerEmail",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingCustomerName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AgencyId",
                table: "Payments",
                column: "AgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AgencyProfiles_AgencyId",
                table: "Payments",
                column: "AgencyId",
                principalTable: "AgencyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AgencyProfiles_AgencyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_AgencyId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BookingCustomerEmail",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BookingCustomerName",
                table: "Payments");
        }
    }
}
