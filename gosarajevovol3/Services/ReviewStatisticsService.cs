using gosarajevovol3.Models;

namespace gosarajevovol3.Services;

/// <summary>
/// Statistika recenzija ugostiteljskog objekta: prosječna ocjena i opisna oznaka.
/// Razvijeno TDD pristupom (Task 3, funkcionalnost 1).
/// </summary>
public class ReviewStatisticsService
{
    private const double AverageWithoutReviews = 0;
    private const int AverageDecimals = 1;

    /// <summary>
    /// Prosječna ocjena zaokružena na jednu decimalu. Bez recenzija prosjek je 0.
    /// </summary>
    public double CalculateAverageRating(IEnumerable<Review> reviews)
    {
        ArgumentNullException.ThrowIfNull(reviews);

        var ratings = reviews.Select(review => review.Rating).ToList();
        if (ratings.Count == 0)
        {
            return AverageWithoutReviews;
        }

        return Math.Round(ratings.Average(), AverageDecimals, MidpointRounding.AwayFromZero);
    }
}
