using gosarajevovol3.Models;

namespace gosarajevovol3.Services;

/// <summary>
/// Statistika recenzija ugostiteljskog objekta: prosječna ocjena i opisna oznaka.
/// Razvijeno TDD pristupom (Task 3, funkcionalnost 1).
/// </summary>
public class ReviewStatisticsService
{
    /// <summary>
    /// Prosječna ocjena zaokružena na jednu decimalu. Bez recenzija prosjek je 0.
    /// </summary>
    public double CalculateAverageRating(IEnumerable<Review> reviews)
    {
        if (reviews == null)
        {
            throw new ArgumentNullException(nameof(reviews));
        }

        int sum = 0;
        int count = 0;
        foreach (var review in reviews)
        {
            sum += review.Rating;
            count++;
        }

        if (count == 0)
        {
            return 0;
        }

        return Math.Round((double)sum / count, 1, MidpointRounding.AwayFromZero);
    }
}
