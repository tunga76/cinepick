namespace CinePick.Domain.Users;

public sealed class UserMovieState
{
    private UserMovieState() { }

    public UserMovieState(Guid userId, Guid movieId, DateTimeOffset updatedAt)
    {
        UserId = userId;
        MovieId = movieId;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; private set; }
    public Guid MovieId { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsWatched { get; private set; }
    public int? Rating { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(bool isFavorite, bool isWatched, int? rating, DateTimeOffset updatedAt)
    {
        if (rating is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(rating));
        IsFavorite = isFavorite;
        IsWatched = isWatched;
        Rating = rating;
        UpdatedAt = updatedAt;
    }
}
