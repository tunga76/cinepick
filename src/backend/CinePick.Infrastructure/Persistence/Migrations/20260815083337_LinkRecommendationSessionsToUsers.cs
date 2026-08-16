using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkRecommendationSessionsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "RecommendationSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationSessions_UserId_CreatedAt",
                table: "RecommendationSessions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecommendationSessions_AspNetUsers_UserId",
                table: "RecommendationSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecommendationSessions_AspNetUsers_UserId",
                table: "RecommendationSessions");

            migrationBuilder.DropIndex(
                name: "IX_RecommendationSessions_UserId_CreatedAt",
                table: "RecommendationSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RecommendationSessions");
        }
    }
}
