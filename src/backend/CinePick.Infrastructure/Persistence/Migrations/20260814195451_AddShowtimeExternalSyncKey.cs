using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShowtimeExternalSyncKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalSyncKey",
                table: "Showtimes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_ExternalSyncKey",
                table: "Showtimes",
                column: "ExternalSyncKey",
                unique: true,
                filter: "[ExternalSyncKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_ExternalSyncKey",
                table: "Showtimes");

            migrationBuilder.DropColumn(
                name: "ExternalSyncKey",
                table: "Showtimes");
        }
    }
}
