namespace CinePick.Application.Recommendations;

public interface IRecommendationRequestParser
{
    RecommendationFilter Parse(string text);
}
