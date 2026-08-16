using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShowtimeQueryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_IsCancelled_StartsAt",
                table: "Showtimes",
                columns: new[] { "IsCancelled", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_IsCancelled_StartsAt",
                table: "Showtimes");
        }
    }
}
