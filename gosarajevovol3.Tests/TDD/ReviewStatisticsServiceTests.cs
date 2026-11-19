using gosarajevovol3.Models;
using gosarajevovol3.Services;

namespace gosarajevovol3.Tests.TDD;

/// <summary>
/// Task 3 - TDD, funkcionalnost 1: statistika recenzija ugostiteljskog objekta.
/// Testovi su pisani PRIJE implementacije (faza RED), ciklus po ciklus.
/// </summary>
[TestClass]
public class ReviewStatisticsServiceTests
{
    private ReviewStatisticsService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new ReviewStatisticsService();
    }

    private static List<Review> ReviewsWithRatings(params int[] ratings)
    {
        return ratings.Select(rating => new Review { Rating = rating, Comment = "Komentar" }).ToList();
    }

    // ---------- Ciklus 1: prosječna ocjena (test napisala: Amna) ----------

    [TestMethod]
    [TestCategory("TDD")]
    public void CalculateAverageRating_NemaRecenzija_VracaNulu()
    {
        var average = _service.CalculateAverageRating(new List<Review>());

        Assert.AreEqual(0, average);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [DataRow(new[] { 5 }, 5.0, DisplayName = "Jedna recenzija")]
    [DataRow(new[] { 4, 5 }, 4.5, DisplayName = "Dvije recenzije")]
    [DataRow(new[] { 4, 5, 5 }, 4.7, DisplayName = "4.666 se zaokružuje na 4.7")]
    [DataRow(new[] { 4, 4, 4, 5 }, 4.3, DisplayName = "4.25 se zaokružuje naviše na 4.3")]
    [DataRow(new[] { 1, 1, 2 }, 1.3, DisplayName = "1.333 se zaokružuje na 1.3")]
    public void CalculateAverageRating_RecenzijePostoje_VracaProsjekNaJednuDecimalu(int[] ocjene, double ocekivano)
    {
        var average = _service.CalculateAverageRating(ReviewsWithRatings(ocjene));

        Assert.AreEqual(ocekivano, average, 0.0001);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CalculateAverageRating_ListaJeNull_BacaArgumentNullException()
    {
        _service.CalculateAverageRating(null!);
    }
}
