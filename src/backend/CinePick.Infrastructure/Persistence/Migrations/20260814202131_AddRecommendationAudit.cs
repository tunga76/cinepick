using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartsFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartsBefore = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MaximumRuntimeMinutes = table.Column<int>(type: "int", nullable: true),
                    GenreSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CitySlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DistrictSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaximumPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationCandidateSnapshots",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowtimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationCandidateSnapshots", x => new { x.SessionId, x.MovieId, x.ShowtimeId });
                    table.ForeignKey(
                        name: "FK_RecommendationCandidateSnapshots_RecommendationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "RecommendationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationResults",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    MovieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowtimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationResults", x => new { x.SessionId, x.Rank });
                    table.ForeignKey(
                        name: "FK_RecommendationResults_RecommendationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "RecommendationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationSessions_CreatedAt",
                table: "RecommendationSessions",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationCandidateSnapshots");

            migrationBuilder.DropTable(
                name: "RecommendationResults");

            migrationBuilder.DropTable(
                name: "RecommendationSessions");
        }
    }
}
