namespace CinePick.Infrastructure.Cinemas;

internal sealed class ShowtimeProviderOptions
{
    public const string SectionName = "ShowtimeProviders";
    public string[] AllowedTicketHosts { get; set; } = ["tickets.example.invalid"];
}
