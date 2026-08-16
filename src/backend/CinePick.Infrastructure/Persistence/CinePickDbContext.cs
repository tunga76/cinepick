using CinePick.Domain.Movies;
using CinePick.Domain.ExternalProviders;
using CinePick.Domain.Cinemas;
using CinePick.Domain.Recommendations;
using CinePick.Infrastructure.Identity;
using CinePick.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Persistence;

public sealed class CinePickDbContext(DbContextOptions<CinePickDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<ExternalSyncLog> ExternalSyncLogs => Set<ExternalSyncLog>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Auditorium> Auditoriums => Set<Auditorium>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<RecommendationSession> RecommendationSessions => Set<RecommendationSession>();
    public DbSet<RecommendationCandidateSnapshot> RecommendationCandidateSnapshots => Set<RecommendationCandidateSnapshot>();
    public DbSet<RecommendationResultRecord> RecommendationResults => Set<RecommendationResultRecord>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserMovieState> UserMovieStates => Set<UserMovieState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CinePickDbContext).Assembly);
    }
}
