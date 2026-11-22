using gosarajevovol2.Models.Enums;
using gosarajevovol3.Models;
using gosarajevovol3.Services;

namespace gosarajevovol3.Tests.TDD;

/// <summary>
/// Task 3 - TDD, funkcionalnost 2: filtriranje i pretraga događaja.
/// Testovi su pisani PRIJE implementacije (faza RED), ciklus po ciklus.
/// </summary>
[TestClass]
public class EventFilterServiceTests
{
    private EventFilterService _service = null!;
    private List<Event> _events = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new EventFilterService();
        _events = new List<Event>
        {
            new Event { Id = 1, EventName = "Sarajevo Film Festival", EventNameEn = "Sarajevo Film Festival", Type = EventType.Festival, StartDate = new DateTime(2026, 8, 14) },
            new Event { Id = 2, EventName = "Hasanaginica", EventNameEn = "The Hasanaginica", Type = EventType.Theater, StartDate = new DateTime(2026, 3, 10) },
            new Event { Id = 3, EventName = "Koncert Dine Merlina", EventNameEn = null, Type = EventType.Concert, StartDate = new DateTime(2026, 6, 1) },
            new Event { Id = 4, EventName = "Jazz veče", EventNameEn = "Jazz night", Type = EventType.Concert, StartDate = new DateTime(2026, 1, 20) }
        };
    }

    private static List<int> Ids(List<Event> events)
    {
        return events.Select(ev => ev.Id).ToList();
    }

    // ---------- Ciklus 3: filtriranje po tipu (test napisala: Ilma) ----------

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByType_TipNijeZadan_VracaSveDogadjajeOdNajranijeg()
    {
        var result = _service.FilterByType(_events, null);

        CollectionAssert.AreEqual(new[] { 4, 2, 3, 1 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByType_TipKoncert_VracaSamoKoncerteOdNajranijeg()
    {
        var result = _service.FilterByType(_events, EventType.Concert);

        CollectionAssert.AreEqual(new[] { 4, 3 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByType_NemaDogadjajaTogTipa_VracaPraznuListu()
    {
        var result = _service.FilterByType(_events, EventType.Movie);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void FilterByType_ListaJeNull_BacaArgumentNullException()
    {
        _service.FilterByType(null!, EventType.Concert);
    }

    // ---------- Ciklus 4: filtriranje po periodu (test napisala: Hana) ----------

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByDateRange_PeriodObuhvataDvaDogadjaja_VracaIhOdNajranijeg()
    {
        var result = _service.FilterByDateRange(_events, new DateTime(2026, 3, 1), new DateTime(2026, 6, 30));

        CollectionAssert.AreEqual(new[] { 2, 3 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByDateRange_DogadjajiTacnoNaGranicama_GraniceSuUkljucene()
    {
        var result = _service.FilterByDateRange(_events, new DateTime(2026, 1, 20), new DateTime(2026, 3, 10));

        CollectionAssert.AreEqual(new[] { 4, 2 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByDateRange_PocetakIKrajSuIstiDan_VracaDogadjajTogDana()
    {
        var result = _service.FilterByDateRange(_events, new DateTime(2026, 6, 1), new DateTime(2026, 6, 1));

        CollectionAssert.AreEqual(new[] { 3 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    public void FilterByDateRange_PeriodBezDogadjaja_VracaPraznuListu()
    {
        var result = _service.FilterByDateRange(_events, new DateTime(2027, 1, 1), new DateTime(2027, 12, 31));

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [ExpectedException(typeof(ArgumentException))]
    public void FilterByDateRange_PocetakJeNakonKraja_BacaArgumentException()
    {
        _service.FilterByDateRange(_events, new DateTime(2026, 6, 30), new DateTime(2026, 3, 1));
    }

    [TestMethod]
    [TestCategory("TDD")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void FilterByDateRange_ListaJeNull_BacaArgumentNullException()
    {
        _service.FilterByDateRange(null!, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
    }

    // ---------- Ciklus 5: pretraga po nazivu (test napisao: Dževad) ----------

    [TestMethod]
    [TestCategory("TDD")]
    [DataRow("festival", new[] { 1 }, DisplayName = "Mala slova u pretrazi")]
    [DataRow("JAZZ", new[] { 4 }, DisplayName = "Velika slova u pretrazi")]
    [DataRow("night", new[] { 4 }, DisplayName = "Riječ postoji samo u engleskom nazivu")]
    [DataRow("  merlin  ", new[] { 3 }, DisplayName = "Razmaci oko teksta se zanemaruju")]
    [DataRow("a", new[] { 4, 2, 3, 1 }, DisplayName = "Više rezultata, od najranijeg")]
    [DataRow("opera", new int[0], DisplayName = "Nema rezultata")]
    public void Search_TekstPretrage_VracaDogadjajeCijiNazivSadrziTekst(string tekst, int[] ocekivaniIds)
    {
        var result = _service.Search(_events, tekst);

        CollectionAssert.AreEqual(ocekivaniIds, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    [DataRow(null, DisplayName = "Tekst je null")]
    [DataRow("", DisplayName = "Tekst je prazan")]
    [DataRow("   ", DisplayName = "Tekst su samo razmaci")]
    public void Search_PrazanTekst_VracaSveDogadjajeOdNajranijeg(string? tekst)
    {
        var result = _service.Search(_events, tekst);

        CollectionAssert.AreEqual(new[] { 4, 2, 3, 1 }, Ids(result));
    }

    [TestMethod]
    [TestCategory("TDD")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Search_ListaJeNull_BacaArgumentNullException()
    {
        _service.Search(null!, "festival");
    }
}
