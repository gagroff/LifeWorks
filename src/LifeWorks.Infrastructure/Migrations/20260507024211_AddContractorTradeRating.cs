using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeWorks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorTradeRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Contractors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Contractors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Trade",
                table: "Contractors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "Trade",
                table: "Contractors");
        }
    }
}
