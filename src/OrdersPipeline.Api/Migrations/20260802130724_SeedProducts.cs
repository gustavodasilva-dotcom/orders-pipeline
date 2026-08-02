using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrdersPipeline.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "Name", "Price", "Stock", "Supplier" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Computers", "Laptop Pro 14", 1499.99m, 12, "Tech Imports" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Accessories", "Wireless Keyboard", 79.90m, 35, "Office Supply Co." },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Accessories", "USB-C Dock", 189.50m, 18, "Tech Imports" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Displays", "27-inch Monitor", 329.00m, 9, "Vision Devices" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Audio", "Noise-Cancelling Headphones", 249.99m, 22, "Sound World" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
