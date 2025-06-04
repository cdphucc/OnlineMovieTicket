using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineMovieTicket.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForSingleCinema13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Format",
                table: "ShowTimes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "ShowTimes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
