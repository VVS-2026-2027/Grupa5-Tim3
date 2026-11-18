using gosarajevovol2.Models.Enums;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services;
using gosarajevovol3.Services.Abstract;
using gosarajevovol3.Services.Scrapers;
using gosarajevovol3.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace gosarajevovol3.Tests.Amina;

[TestClass]
public class KupiKartuServiceTests
{
    private const string CategoryUrl = "https://www.kupikartu.ba/karte/kategorija/2";
    private const string EventUrl = "https://www.kupikartu.ba/karte/event/7";

    private ApplicationDbContext _context = null!;
    private FakeHttpMessageHandler _handler = null!;
    private Mock<IEventTranslator> _translatorMock = null!;
    private KupiKartuService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _handler = new FakeHttpMessageHandler();
        _translatorMock = new Mock<IEventTranslator>();

        var scraper = new KupiKartuScraper(new HttpClient(_handler), new GeocodingService(new HttpClient(_handler)));
        _service = new KupiKartuService(
            TestHelper.MockScopeFactory(_context).Object, scraper, _translatorMock.Object, TimeSpan.Zero);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _service.Dispose();
        _context.Dispose();
    }

    /// <summary>Lažna stranica: jedna kategorija koncerata sa jednim događajem u Sarajevu.</summary>
    private void AddSiteWithOneEvent(string lokacija = "Zetra, Sarajevo")
    {
        _handler.AddResponse("/karte/event/7",
            $"<html><body><h1>Koncert godine</h1><p>15/03</p><p>{lokacija}</p></body></html>");
        _handler.AddResponse("/karte/kategorija/2", "<a href='/karte/event/7'>Koncert godine</a>");
        _handler.AddResponse("nominatim", "[]");
        _handler.AddResponse("kupikartu.ba", "<a href='/karte/kategorija/2'>Koncerti</a>");
    }

    /// <summary>Pokreće servis, čeka prvo čitanje stranice i zaustavlja ga.</summary>
    private async Task RunServiceOnceAsync()
    {
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Priority(1)]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaJePrazna_UpisujeDogadjajSaStranice()
    {
        AddSiteWithOneEvent();

        await RunServiceOnceAsync();

        var ev = _context.Events.Single();
        Assert.AreEqual("Koncert godine", ev.EventName);
        Assert.AreEqual(EventUrl, ev.WebUrl);
        Assert.AreEqual(EventType.Concert, ev.Type);
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_NoviDogadjaj_PozivaPrevodilacTacnoJednom()
    {
        AddSiteWithOneEvent();

        await RunServiceOnceAsync();

        _translatorMock.Verify(t => t.TranslateInEnAsync(It.Is<Event>(e => e.EventName == "Koncert godine")), Times.Once);
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_PrevodilacPostaviEngleskiNaziv_NazivSeCuvaUBazi()
    {
        AddSiteWithOneEvent();
        _translatorMock
            .Setup(t => t.TranslateInEnAsync(It.IsAny<Event>()))
            .Callback<Event>(e => e.EventNameEn = "Concert of the year")
            .Returns(Task.CompletedTask);

        await RunServiceOnceAsync();

        Assert.AreEqual("Concert of the year", _context.Events.Single().EventNameEn);
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaVecImaDogadjajeSaKupiKartu_NeCitaStranicu()
    {
        _context.Events.Add(new Event { EventName = "Stari", WebUrl = "https://www.kupikartu.ba/karte/event/1", LocationAddress = "Zetra" });
        _context.SaveChanges();
        AddSiteWithOneEvent();

        await RunServiceOnceAsync();

        Assert.AreEqual(0, _handler.RequestedUrls.Count);
        Assert.AreEqual(1, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaImaSamoDogadjajeDrugogIzvora_CitaStranicu()
    {
        _context.Events.Add(new Event { EventName = "Predstava", WebUrl = "https://nps.ba/repertoar/hasanaginica", LocationAddress = "NPS" });
        _context.SaveChanges();
        AddSiteWithOneEvent();

        await RunServiceOnceAsync();

        Assert.AreEqual(2, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_DogadjajNijeUSarajevu_NeUpisujeGaINePrevodi()
    {
        AddSiteWithOneEvent("Arena, Zenica");

        await RunServiceOnceAsync();

        Assert.AreEqual(0, _context.Events.Count());
        _translatorMock.Verify(t => t.TranslateInEnAsync(It.IsAny<Event>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_IstiDogadjajUDvijeKategorije_UpisujeGaJednom()
    {
        _handler.AddResponse("/karte/event/7", "<html><body><h1>Koncert godine</h1><p>15/03</p><p>Zetra, Sarajevo</p></body></html>");
        _handler.AddResponse("/karte/kategorija/", "<a href='/karte/event/7'>Koncert godine</a>");
        _handler.AddResponse("nominatim", "[]");
        _handler.AddResponse("kupikartu.ba", "<a href='/karte/kategorija/2'>Koncerti</a><a href='/karte/kategorija/5'>Ostalo</a>");

        await RunServiceOnceAsync();

        Assert.AreEqual(1, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_StranicaNedostupna_BazaOstajePrazna()
    {
        await RunServiceOnceAsync();

        Assert.AreEqual(0, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_ProdjeVrijemeDoSljedecegCitanja_StranicaSeCitaPonovo()
    {
        AddSiteWithOneEvent();
        _service.NextRunDelay = () => TimeSpan.FromMilliseconds(20);

        await _service.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await _service.StopAsync(CancellationToken.None);

        Assert.IsTrue(_handler.RequestedUrls.Count(url => url == "https://www.kupikartu.ba/") >= 2);
        Assert.AreEqual(1, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("KupiKartuService")]
    public void Konstruktor_SamoScopeFactory_KreiraServis()
    {
        using var service = new KupiKartuService(TestHelper.MockScopeFactory(_context).Object);

        Assert.IsNotNull(service);
    }
}
