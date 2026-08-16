using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalMovieId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OriginalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Overview = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RuntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    OriginalLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AgeRating = table.Column<int>(type: "int", nullable: false),
                    PosterPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BackdropPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoteAverage = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Popularity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsNowPlaying = table.Column<bool>(type: "bit", nullable: false),
                    IsUpcoming = table.Column<bool>(type: "bit", nullable: false),
                    LastSynchronizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    MovieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => new { x.MovieId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MovieGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieGenres_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Slug",
                table: "Genres",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ExternalProviderId_ExternalMovieId",
                table: "Movies",
                columns: new[] { "ExternalProviderId", "ExternalMovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsNowPlaying_Popularity",
                table: "Movies",
                columns: new[] { "IsNowPlaying", "Popularity" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsUpcoming_ReleaseDate",
                table: "Movies",
                columns: new[] { "IsUpcoming", "ReleaseDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Movies");
        }
    }
}
