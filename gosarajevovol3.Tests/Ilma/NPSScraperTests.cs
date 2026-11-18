using System.Net;
using gosarajevovol2.Models.Enums;
using gosarajevovol3.Services.Scrapers;
using gosarajevovol3.Tests.Helpers;

namespace gosarajevovol3.Tests.Ilma;

/// <summary>
/// Autor testova: Ilma.
/// Testira se NPSScraper - čitanje repertoara sa stranice Narodnog pozorišta Sarajevo (nps.ba).
/// Internet je zamijenjen objektom FakeHttpMessageHandler, koji vraća pripremljen HTML.
/// </summary>
[TestClass]
public class NPSScraperTests
{
    private const string PlayUrl = "https://nps.ba/repertoar/hasanaginica";

    private FakeHttpMessageHandler _handler = null!;
    private NPSScraper _scraper = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new FakeHttpMessageHandler();
        _scraper = new NPSScraper(new HttpClient(_handler));
    }

    private static string Page(string body)
    {
        return $"<html><body><h1>Drama Hasanaginica</h1>{body}</body></html>";
    }

    // ---------- GetPlayUrls ----------

    [TestMethod]
    [TestCategory("NPSScraper")]
    [Priority(1)]
    public async Task GetPlayUrls_RepertoarSaPredstavama_VracaPuneAdreseBezDuplikata()
    {
        _handler.AddResponse("nps.ba/repertoar",
            "<a href='/repertoar/hasanaginica'>A</a><a href='https://nps.ba/repertoar/ero'>B</a><a href='/repertoar/hasanaginica'>A opet</a>");

        var urls = await _scraper.GetPlayUrls();

        CollectionAssert.AreEqual(
            new[] { "https://nps.ba/repertoar/hasanaginica", "https://nps.ba/repertoar/ero" },
            urls);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task GetPlayUrls_LinkNaSamRepertoar_NijeUListi()
    {
        _handler.AddResponse("nps.ba/repertoar",
            "<a href='/repertoar/'>Repertoar</a><a href='/o-nama'>O nama</a><a href='/repertoar/ero'>Ero</a>");

        var urls = await _scraper.GetPlayUrls();

        CollectionAssert.AreEqual(new[] { "https://nps.ba/repertoar/ero" }, urls);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task GetPlayUrls_StranicaBezLinkova_VracaPraznuListu()
    {
        _handler.AddResponse("nps.ba/repertoar", "<html><body><p>Sezona je zavrsena</p></body></html>");

        var urls = await _scraper.GetPlayUrls();

        Assert.AreEqual(0, urls.Count);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task GetPlayUrls_StranicaNedostupna_VracaPraznuListu()
    {
        _handler.AddResponse("nps.ba/repertoar", "", HttpStatusCode.InternalServerError);

        var urls = await _scraper.GetPlayUrls();

        Assert.AreEqual(0, urls.Count);
    }

    // ---------- ScrapePlayDetails: osnovni podaci ----------

    public static IEnumerable<object?[]> PredstaveIzXmlFajla()
    {
        return XmlTestData.Read("ilma_predstave.xml",
            ("html", typeof(string)), ("naziv", typeof(string)), ("slika", typeof(string)), ("opisPredstave", typeof(string)));
    }

    /// <summary>
    /// Data-driven test sa podacima iz XML fajla (DynamicData): iz sačuvanog HTML-a
    /// moraju se izvući naziv, slika i opis predstave.
    /// </summary>
    [TestMethod]
    [TestCategory("NPSScraper")]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(PredstaveIzXmlFajla), DynamicDataSourceType.Method)]
    public async Task ScrapePlayDetails_StranicaIzXmlFajla_IzvlaciNazivSlikuIOpis(
        string html, string naziv, string slika, string opisPredstave)
    {
        _handler.AddResponse("/repertoar/", html);

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(naziv, ev!.EventName);
        Assert.AreEqual(slika, ev.PhotoUrl);
        Assert.AreEqual(opisPredstave, ev.EventDescription);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_BiloKojaPredstava_ImaStalnePodatkeOPozoristu()
    {
        _handler.AddResponse("/repertoar/", Page(""));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(EventType.Theater, ev!.Type);
        Assert.AreEqual("National Theatre Sarajevo", ev.LocationAddress);
        Assert.AreEqual(PlayUrl, ev.WebUrl);
        Assert.AreEqual(43.8568, ev.Coordinates.Latitude, 0.0001);
        Assert.AreEqual(18.4207, ev.Coordinates.Longitude, 0.0001);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_StranicaNePostoji_VracaNull()
    {
        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.IsNull(ev);
    }

    // ---------- ScrapePlayDetails: opis ----------

    /// <summary>
    /// Data-driven test (DataRow): pasusi koji nisu dio opisa predstave se preskaču.
    /// </summary>
    [TestMethod]
    [TestCategory("NPSScraper")]
    [TestCategory("DataDriven")]
    [DataRow("<p>Drama Opera Balet</p>", DisplayName = "Tekst navigacije")]
    [DataRow("<p>hasanaginica</p>", DisplayName = "Pasus jednak nazivu predstave")]
    [DataRow("<p>Kupi ulaznicu online</p>", DisplayName = "Dugme za kupovinu")]
    [DataRow("<p class='Nav-item'>Pocetna</p>", DisplayName = "Pasus sa klasom nav")]
    [DataRow("<div class='main-NAV'><p>Kontakt</p></div>", DisplayName = "Pasus unutar elementa sa klasom nav")]
    [DataRow("<p style='text-align: right'>Foto: arhiv</p>", DisplayName = "Pasus poravnat desno")]
    [DataRow("<p>   </p>", DisplayName = "Prazan pasus")]
    public async Task ScrapePlayDetails_PasusKojiNijeOpis_NeUlaziUOpis(string suvisanPasus)
    {
        _handler.AddResponse("/repertoar/", Page($"<article>{suvisanPasus}<p>Pravi opis predstave.</p></article>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual("Pravi opis predstave.", ev!.EventDescription);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_SviPasusiSuSuvisni_OpisJeNoDescription()
    {
        _handler.AddResponse("/repertoar/", Page("<article><p>Drama Opera Balet</p><p>Kupi ulaznicu</p></article>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual("No description", ev!.EventDescription);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_OpisOdTacno250Znakova_NeSkracujeSe()
    {
        var opis = new string('a', 250);
        _handler.AddResponse("/repertoar/", Page($"<article><p>{opis}</p></article>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(opis, ev!.EventDescription);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_OpisOd251Znaka_SkracujeSeNa247ZnakovaITriTacke()
    {
        _handler.AddResponse("/repertoar/", Page($"<article><p>{new string('a', 251)}</p></article>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(new string('a', 247) + "...", ev!.EventDescription);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_ViseRazmakaIPrelomaReda_SvodiSeNaJedanRazmak()
    {
        _handler.AddResponse("/repertoar/", Page("<article><p>Prvi   dio\n\n teksta.</p><p>Drugi dio.</p></article>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual("Prvi dio teksta. Drugi dio.", ev!.EventDescription);
    }

    // ---------- ScrapePlayDetails: datum i trajanje ----------

    [TestMethod]
    [TestCategory("NPSScraper")]
    [Priority(1)]
    public async Task ScrapePlayDetails_JsonLdSaDatumom_PostavljaPocetakPredstave()
    {
        _handler.AddResponse("/repertoar/",
            Page("<script type='application/ld+json'>{\"@type\":\"Event\",\"startDate\":\"2026-12-24T19:30:00\"}</script>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(new DateTime(2026, 12, 24, 19, 30, 0), ev!.StartDate);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public async Task ScrapePlayDetails_PrviJsonLdNeispravan_KoristiSljedeci()
    {
        _handler.AddResponse("/repertoar/", Page(
            "<script type='application/ld+json'>ovo nije json</script>" +
            "<script type='application/ld+json'>{\"name\":\"bez datuma\"}</script>" +
            "<script type='application/ld+json'>{\"startDate\":\"2026-05-01T20:00:00\"}</script>" +
            "<script type='application/ld+json'>{\"startDate\":\"2027-01-01T20:00:00\"}</script>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(new DateTime(2026, 5, 1, 20, 0, 0), ev!.StartDate);
    }

    /// <summary>
    /// Data-driven test (DataRow): kada datum nije moguće pročitati, početak je trenutno vrijeme.
    /// </summary>
    [TestMethod]
    [TestCategory("NPSScraper")]
    [TestCategory("DataDriven")]
    [DataRow("", DisplayName = "Nema JSON-LD elementa")]
    [DataRow("<script type='application/ld+json'>{\"startDate\":\"uskoro\"}</script>", DisplayName = "Datum nije datum")]
    [DataRow("<script type='application/ld+json'>{\"startDate\":\"\"}</script>", DisplayName = "Datum je prazan")]
    [DataRow("<script type='application/ld+json'>{\"startDate\":null}</script>", DisplayName = "Datum je null")]
    public async Task ScrapePlayDetails_DatumSeNeMozeProcitati_PocetakJeTrenutnoVrijeme(string jsonLd)
    {
        _handler.AddResponse("/repertoar/", Page(jsonLd));
        var prije = DateTime.Now;

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.IsTrue(ev!.StartDate >= prije && ev.StartDate <= DateTime.Now);
    }

    /// <summary>
    /// Data-driven test sa podacima iz CSV fajla (izvor podataka): trajanje predstave
    /// se čita iz teksta stranice; ako ga nema, podrazumijeva se 90 minuta.
    /// </summary>
    [TestMethod]
    [TestCategory("NPSScraper")]
    [TestCategory("DataDriven")]
    [CsvDataSource("ilma_trajanje.csv")]
    public async Task ScrapePlayDetails_TekstIzCsvFajla_KrajJePocetakPlusTrajanje(string tekst, int ocekivanoMinuta)
    {
        _handler.AddResponse("/repertoar/", Page(
            $"<div>{tekst}</div><script type='application/ld+json'>{{\"startDate\":\"2026-12-24T19:30:00\"}}</script>"));

        var ev = await _scraper.ScrapePlayDetails(PlayUrl);

        Assert.AreEqual(ocekivanoMinuta, (ev!.EndDate - ev.StartDate).TotalMinutes);
    }

    [TestMethod]
    [TestCategory("NPSScraper")]
    public void Konstruktor_BezParametara_KreiraScraper()
    {
        var scraper = new NPSScraper();

        Assert.IsNotNull(scraper);
    }
}
