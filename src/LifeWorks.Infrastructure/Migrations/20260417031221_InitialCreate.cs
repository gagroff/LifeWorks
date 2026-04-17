using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LifeWorks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSeeded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomeImprovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DetailedNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCompleted = table.Column<DateOnly>(type: "date", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WarrantyExpiration = table.Column<DateOnly>(type: "date", nullable: true),
                    ManufacturerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerWarrantyExpiration = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeImprovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeImprovements_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeImprovements_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HomeImprovements_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsSeeded", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), true, "Appliances", 1 },
                    { new Guid("22222222-0000-0000-0000-000000000002"), true, "Electrical", 2 },
                    { new Guid("22222222-0000-0000-0000-000000000003"), true, "Exterior / Siding", 3 },
                    { new Guid("22222222-0000-0000-0000-000000000004"), true, "Flooring", 4 },
                    { new Guid("22222222-0000-0000-0000-000000000005"), true, "HVAC", 5 },
                    { new Guid("22222222-0000-0000-0000-000000000006"), true, "Landscaping", 6 },
                    { new Guid("22222222-0000-0000-0000-000000000007"), true, "Painting", 7 },
                    { new Guid("22222222-0000-0000-0000-000000000008"), true, "Plumbing", 8 },
                    { new Guid("22222222-0000-0000-0000-000000000009"), true, "Roofing", 9 },
                    { new Guid("22222222-0000-0000-0000-000000000010"), true, "Structural", 10 },
                    { new Guid("22222222-0000-0000-0000-000000000011"), true, "Windows & Doors", 11 },
                    { new Guid("22222222-0000-0000-0000-000000000012"), true, "Other", 12 }
                });

            migrationBuilder.InsertData(
                table: "Properties",
                columns: new[] { "Id", "Address", "CreatedAt", "Name", "Notes", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Primary Home", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000002"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lake House", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeImprovements_CategoryId",
                table: "HomeImprovements",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeImprovements_ContractorId",
                table: "HomeImprovements",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeImprovements_PropertyId",
                table: "HomeImprovements",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeImprovements");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Contractors");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
