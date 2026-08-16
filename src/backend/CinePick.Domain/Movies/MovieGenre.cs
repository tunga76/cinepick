namespace CinePick.Domain.Movies;

public sealed class MovieGenre
{
    private MovieGenre()
    {
    }

    public MovieGenre(Guid movieId, Guid genreId)
    {
        MovieId = movieId;
        GenreId = genreId;
    }

    public Guid MovieId { get; private set; }

    public Movie Movie { get; private set; } = null!;

    public Guid GenreId { get; private set; }

    public Genre Genre { get; private set; } = null!;
}
