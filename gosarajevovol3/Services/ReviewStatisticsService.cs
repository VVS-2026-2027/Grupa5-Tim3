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
        throw new NotImplementedException();
    }
}
