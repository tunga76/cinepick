using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinePick.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CinePickDbContext))]
[Migration("20260829081500_EnsureCoreGenres")]
public sealed class EnsureCoreGenres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'aksiyon')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000001', N'Aksiyon', N'aksiyon');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'animasyon')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000002', N'Animasyon', N'animasyon');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'komedi')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000003', N'Komedi', N'komedi');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'dram')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000004', N'Dram', N'dram');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'aile')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000005', N'Aile', N'aile');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'bilim-kurgu')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000006', N'Bilim Kurgu', N'bilim-kurgu');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'gerilim')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000007', N'Gerilim', N'gerilim');
            IF NOT EXISTS (SELECT 1 FROM [Genres] WHERE [Slug] = N'romantik')
                INSERT INTO [Genres] ([Id], [Name], [Slug]) VALUES ('10000000-0000-0000-0000-000000000008', N'Romantik', N'romantik');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE genre
            FROM [Genres] AS genre
            WHERE genre.[Id] IN (
                '10000000-0000-0000-0000-000000000001',
                '10000000-0000-0000-0000-000000000002',
                '10000000-0000-0000-0000-000000000003',
                '10000000-0000-0000-0000-000000000004',
                '10000000-0000-0000-0000-000000000005',
                '10000000-0000-0000-0000-000000000006',
                '10000000-0000-0000-0000-000000000007',
                '10000000-0000-0000-0000-000000000008')
              AND NOT EXISTS (
                  SELECT 1 FROM [MovieGenres] AS link WHERE link.[GenreId] = genre.[Id]);
            """);
    }
}
