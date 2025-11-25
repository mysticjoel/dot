using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiTemplate.Migrations
{
    public partial class AddPaymentAttemptExpiryTimeAndConfirmedAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConfirmedAmount",
                table: "PaymentAttempts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryTime",
                table: "PaymentAttempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAmount",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ExpiryTime",
                table: "PaymentAttempts");
        }
    }
}
