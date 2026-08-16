namespace CinePick.Infrastructure.Identity;

internal sealed class IdentityBootstrapOptions
{
    public bool Enabled { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "CinePick Admin";
}
