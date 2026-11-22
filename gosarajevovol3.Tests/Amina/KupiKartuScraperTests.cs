using System.Net;
using gosarajevovol2.Models.Enums;
using gosarajevovol3.Services;
using gosarajevovol3.Services.Scrapers;
using gosarajevovol3.Tests.Helpers;

namespace gosarajevovol3.Tests.Amina;

[TestClass]
public class KupiKartuScraperTests
{
    private const string EventUrl = "https://www.kupikartu.ba/karte/event/100";

    private FakeHttpMessageHandler _handler = null!;
    private KupiKartuScraper _scraper = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new FakeHttpMessageHandler();
        var geocoding = new GeocodingService(new HttpClient(_handler));
        _scraper = new KupiKartuScraper(new HttpClient(_handler), geocoding);
    }

    private static string EventPage(string datum, string lokacija, string naziv = "Koncert")
    {
        return $"<html><body><h1>{naziv}</h1><p>{datum}</p><p>{lokacija}</p></body></html>";
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetHamburgerMenuUrls_StranicaSaKategorijama_VracaPuneAdrese()
    {
        _handler.AddResponse("kupikartu.ba",
            "<a href='/karte/kategorija/2'>Koncerti</a><a href=' /karte/kategorija/3 '>Pozoriste</a><a href='/o-nama'>O nama</a>");

        var urls = await _scraper.GetHamburgerMenuUrls();

        CollectionAssert.AreEqual(
            new[] { "https://www.kupikartu.ba/karte/kategorija/2", "https://www.kupikartu.ba/karte/kategorija/3" },
            urls);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetHamburgerMenuUrls_IstaKategorijaViseputa_VracaJeJednom()
    {
        _handler.AddResponse("kupikartu.ba",
            "<a href='/karte/kategorija/2'>A</a><a href='https://www.kupikartu.ba/karte/kategorija/2'>B</a>");

        var urls = await _scraper.GetHamburgerMenuUrls();

        Assert.AreEqual(1, urls.Count);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetHamburgerMenuUrls_StranicaBezKategorija_VracaPraznuListu()
    {
        _handler.AddResponse("kupikartu.ba", "<html><body><p>Nema linkova</p></body></html>");

        var urls = await _scraper.GetHamburgerMenuUrls();

        Assert.AreEqual(0, urls.Count);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetHamburgerMenuUrls_StranicaNedostupna_VracaPraznuListu()
    {
        _handler.AddResponse("kupikartu.ba", "", HttpStatusCode.ServiceUnavailable);

        var urls = await _scraper.GetHamburgerMenuUrls();

        Assert.AreEqual(0, urls.Count);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetEventUrls_KategorijaSaDogadjajima_VracaAdreseBezDuplikata()
    {
        _handler.AddResponse("kategorija/2",
            "<a href='/karte/event/1'>A</a><a href='/karte/event/2'>B</a><a href='/karte/event/1'>A opet</a><a href='/kontakt'>K</a>");

        var urls = await _scraper.GetEventUrls("https://www.kupikartu.ba/karte/kategorija/2");

        CollectionAssert.AreEqual(
            new[] { "https://www.kupikartu.ba/karte/event/1", "https://www.kupikartu.ba/karte/event/2" },
            urls);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetEventUrls_KategorijaBezDogadjaja_VracaPraznuListu()
    {
        _handler.AddResponse("kategorija/2", "<html><body></body></html>");

        var urls = await _scraper.GetEventUrls("https://www.kupikartu.ba/karte/kategorija/2");

        Assert.AreEqual(0, urls.Count);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task GetEventUrls_StranicaNePostoji_VracaPraznuListu()
    {
        var urls = await _scraper.GetEventUrls("https://www.kupikartu.ba/karte/kategorija/99");

        Assert.AreEqual(0, urls.Count);
    }

  
    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [TestCategory("DataDriven")]
    [CsvDataSource("amina_kategorije.csv")]
    public void ChooseCategory_AdresaIzCsvFajla_VracaOcekivaniTip(string url, EventType ocekivaniTip)
    {
        var tip = KupiKartuScraper.ChooseCategory(url);

        Assert.AreEqual(ocekivaniTip, tip);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [TestCategory("DataDriven")]
    [XmlDataSource("amina_dogadjaji.xml", "html", "naziv", "adresa", "slika", "opisDogadjaja")]
    public async Task ScrapeEvents_StranicaIzXmlFajla_IzvlaciPodatkeODogadjaju(
        string html, string naziv, string adresa, string slika, string opisDogadjaja)
    {
        _handler.AddResponse("/karte/event/", html);

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Theater);

        Assert.AreEqual(naziv, ev!.EventName);
        Assert.AreEqual(adresa, ev.LocationAddress);
        Assert.AreEqual(slika, ev.PhotoUrl);
        Assert.AreEqual(opisDogadjaja.ReplaceLineEndings(), ev.EventDescription.ReplaceLineEndings());
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [Priority(1)]
    public async Task ScrapeEvents_IspravnaStranica_PopunjavaAdresuStraniceITip()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Zetra, Sarajevo"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Movie);

        Assert.AreEqual(EventUrl, ev!.WebUrl);
        Assert.AreEqual(EventType.Movie, ev.Type);
        Assert.AreEqual(ev.StartDate, ev.EndDate);
    }

   
    public static IEnumerable<object?[]> IspravniZapisiDatuma => new[]
    {
        new object?[] { "15/03", 15, 3 },
        new object?[] { "15-17/03", 15, 3 },
        new object?[] { " 01 - 05/12 ", 1, 12 },
        new object?[] { "31/12", 31, 12 },
    };

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(IspravniZapisiDatuma))]
    public async Task ScrapeEvents_IspravanZapisDatuma_PostavljaDanIMjesec(string datum, int dan, int mjesec)
    {
        _handler.AddResponse("/karte/event/", EventPage(datum, "Zetra, Sarajevo"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(new DateTime(DateTime.Now.Year, mjesec, dan), ev!.StartDate);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [TestCategory("DataDriven")]
    [DataRow("uskoro", DisplayName = "Tekst umjesto datuma")]
    [DataRow("15-17", DisplayName = "Raspon bez mjeseca")]
    [DataRow("32/01", DisplayName = "Nepostojeći dan")]
    [DataRow("15.03.", DisplayName = "Tačke umjesto kose crte")]
    public async Task ScrapeEvents_NeispravanZapisDatuma_PostavljaDanasnjiDatum(string datum)
    {
        _handler.AddResponse("/karte/event/", EventPage(datum, "Zetra, Sarajevo"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(DateTime.Today, ev!.StartDate);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_DogadjajNijeUSarajevu_VracaNull()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Arena, Zenica"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.IsNull(ev);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_StranicaBezPasusa_VracaNull()
    {
        _handler.AddResponse("/karte/event/", "<html><body><h1>Koncert</h1></body></html>");

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.IsNull(ev);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_StranicaNePostoji_VracaNull()
    {
        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.IsNull(ev);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_KoncertSaRijecjuFestivalUNazivu_PostajeFestival()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Zetra, Sarajevo", "Sarajevo Jazz FESTIVAL"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(EventType.Festival, ev!.Type);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_PredstavaSaRijecjuFestivalUNazivu_OstajePredstava()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Zetra, Sarajevo", "Festival smijeha"));

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Theater);

        Assert.AreEqual(EventType.Theater, ev!.Type);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_AdresaPronadjena_PostavljaKoordinateIzGeokodiranja()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Zetra, Sarajevo"));
        _handler.AddResponse("nominatim", "[{\"lat\":\"43.8720\",\"lon\":\"18.4090\"}]");

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(43.8720, ev!.Coordinates.Latitude, 0.0001);
        Assert.AreEqual(18.4090, ev.Coordinates.Longitude, 0.0001);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public async Task ScrapeEvents_PunaAdresaNijePronadjena_PokusavaSaPrveDvijeRijeci()
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", "Dom mladih, Terezije bb, Sarajevo"));
        _handler.AddResponse("Dom%20mladih%2C%20Sarajevo", "[{\"lat\":\"43.8600\",\"lon\":\"18.4150\"}]");
        _handler.AddResponse("nominatim", "[]");

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(43.8600, ev!.Coordinates.Latitude, 0.0001);
        Assert.AreEqual(18.4150, ev.Coordinates.Longitude, 0.0001);
    }

   
    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    [TestCategory("DataDriven")]
    [DataRow("Sarajevo", DisplayName = "Lokacija je samo naziv grada (adresa je prazna)")]
    [DataRow("Zetra, Sarajevo", DisplayName = "Adresa od jedne riječi koja nije pronađena")]
    [DataRow("Dom mladih, Terezije bb, Sarajevo", DisplayName = "Ni skraćena adresa nije pronađena")]
    public async Task ScrapeEvents_AdresaNijePronadjena_OstajuKoordinateCentraSarajeva(string lokacija)
    {
        _handler.AddResponse("/karte/event/", EventPage("15/03", lokacija));
        _handler.AddResponse("nominatim", "[]");

        var ev = await _scraper.ScrapeEvents(EventUrl, EventType.Concert);

        Assert.AreEqual(43.8563, ev!.Coordinates.Latitude, 0.0001);
        Assert.AreEqual(18.4131, ev.Coordinates.Longitude, 0.0001);
    }

    [TestMethod]
    [TestCategory("KupiKartuScraper")]
    public void Konstruktor_BezParametara_KreiraScraper()
    {
        var scraper = new KupiKartuScraper();

        Assert.IsNotNull(scraper);
    }
}
