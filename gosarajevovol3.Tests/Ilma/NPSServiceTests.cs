using gosarajevovol2.Models.Enums;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Abstract;
using gosarajevovol3.Services.Scrapers;
using gosarajevovol3.Tests.Helpers;
using Moq;

namespace gosarajevovol3.Tests.Ilma;

[TestClass]
public class NPSServiceTests
{
    private ApplicationDbContext _context = null!;
    private FakeHttpMessageHandler _handler = null!;
    private Mock<IEventTranslator> _translatorMock = null!;
    private NPSService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _handler = new FakeHttpMessageHandler();
        _translatorMock = new Mock<IEventTranslator>();
        _service = new NPSService(
            TestHelper.MockScopeFactory(_context).Object,
            new NPSScraper(new HttpClient(_handler)),
            _translatorMock.Object,
            TimeSpan.Zero);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _service.Dispose();
        _context.Dispose();
    }

    private void AddRepertoireWithTwoPlays()
    {
        _handler.AddResponse("/repertoar/hasanaginica", "<html><body><h1>Drama Hasanaginica</h1></body></html>");
        _handler.AddResponse("/repertoar/ero", "<html><body><h1>Opera Ero s onoga svijeta</h1></body></html>");
        _handler.AddResponse("nps.ba/repertoar", "<a href='/repertoar/hasanaginica'>A</a><a href='/repertoar/ero'>B</a>");
    }

    private async Task RunServiceOnceAsync()
    {
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [Priority(1)]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaJePrazna_UpisujeSvePredstaveSaRepertoara()
    {
        AddRepertoireWithTwoPlays();

        await RunServiceOnceAsync();

        CollectionAssert.AreEquivalent(
            new[] { "Hasanaginica", "Ero s onoga svijeta" },
            _context.Events.Select(e => e.EventName).ToList());
        Assert.IsTrue(_context.Events.All(e => e.Type == EventType.Theater));
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_DvijeNovePredstave_PrevodilacSePozivaZaSvaku()
    {
        AddRepertoireWithTwoPlays();

        await RunServiceOnceAsync();

        _translatorMock.Verify(t => t.TranslateInEnAsync(It.IsAny<Event>()), Times.Exactly(2));
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_PrevodilacPostaviEngleskiNaziv_NazivSeCuvaUBazi()
    {
        _handler.AddResponse("/repertoar/hasanaginica", "<html><body><h1>Drama Hasanaginica</h1></body></html>");
        _handler.AddResponse("nps.ba/repertoar", "<a href='/repertoar/hasanaginica'>A</a>");
        _translatorMock
            .Setup(t => t.TranslateInEnAsync(It.IsAny<Event>()))
            .Callback<Event>(e => e.EventNameEn = "The Hasanaginica")
            .Returns(Task.CompletedTask);

        await RunServiceOnceAsync();

        Assert.AreEqual("The Hasanaginica", _context.Events.Single().EventNameEn);
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaVecImaPredstaveSaNps_NeCitaRepertoar()
    {
        _context.Events.Add(new Event { EventName = "Stara", WebUrl = "https://nps.ba/repertoar/stara", LocationAddress = "NPS" });
        _context.SaveChanges();
        AddRepertoireWithTwoPlays();

        await RunServiceOnceAsync();

        Assert.AreEqual(0, _handler.RequestedUrls.Count);
        Assert.AreEqual(1, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_BazaImaSamoDogadjajeDrugogIzvora_CitaRepertoar()
    {
        _context.Events.Add(new Event { EventName = "Koncert", WebUrl = "https://www.kupikartu.ba/karte/event/1", LocationAddress = "Zetra" });
        _context.SaveChanges();
        AddRepertoireWithTwoPlays();

        await RunServiceOnceAsync();

        Assert.AreEqual(3, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [TestCategory("Mock")]
    [Timeout(10000)]
    public async Task ExecuteAsync_StranicaPredstaveNedostupna_PredstavaSePreskace()
    {
        _handler.AddResponse("/repertoar/ero", "<html><body><h1>Opera Ero s onoga svijeta</h1></body></html>");
        _handler.AddResponse("nps.ba/repertoar", "<a href='/nedostupno/repertoar/x'>A</a><a href='/repertoar/ero'>B</a>");

        await RunServiceOnceAsync();

        Assert.AreEqual("Ero s onoga svijeta", _context.Events.Single().EventName);
        _translatorMock.Verify(t => t.TranslateInEnAsync(It.IsAny<Event>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_RepertoarNedostupan_BazaOstajePrazna()
    {
        await RunServiceOnceAsync();

        Assert.AreEqual(0, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("NPSService")]
    [Timeout(10000)]
    public async Task ExecuteAsync_ProdjeVrijemeDoSljedecegCitanja_RepertoarSeCitaPonovo()
    {
        AddRepertoireWithTwoPlays();
        _service.NextRunDelay = () => TimeSpan.FromMilliseconds(20);

        await _service.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await _service.StopAsync(CancellationToken.None);

        Assert.IsTrue(_handler.RequestedUrls.Count(url => url == "https://nps.ba/repertoar") >= 2);
        Assert.AreEqual(2, _context.Events.Count());
    }

    [TestMethod]
    [TestCategory("NPSService")]
    public void Konstruktor_SamoScopeFactory_KreiraServis()
    {
        using var service = new NPSService(TestHelper.MockScopeFactory(_context).Object);

        Assert.IsNotNull(service);
    }
}
