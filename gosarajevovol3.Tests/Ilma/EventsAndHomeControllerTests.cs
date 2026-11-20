using System.Diagnostics;
using System.Text.Json;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace gosarajevovol3.Tests.Ilma;

[TestClass]
public class EventsAndHomeControllerTests
{
    private ApplicationDbContext _context = null!;
    private EventsController _events = null!;
    private HomeController _home = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.Events.AddRange(
            new Event
            {
                Id = 1, EventName = "Prošli koncert", LocationAddress = "Zetra",
                StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(-5), WebUrl = "https://test.ba/1"
            },
            new Event
            {
                Id = 2, EventName = "Kasniji festival", EventNameEn = "Later festival", LocationAddress = "Vijećnica",
                StartDate = new DateTime(2099, 8, 20, 21, 0, 0), EndDate = new DateTime(2099, 8, 20, 23, 30, 0), WebUrl = "https://test.ba/2"
            },
            new Event
            {
                Id = 3, EventName = "Raniji koncert", EventNameEn = "", LocationAddress = "Dom mladih",
                StartDate = new DateTime(2099, 3, 1, 20, 0, 0), EndDate = new DateTime(2099, 3, 1, 22, 0, 0), WebUrl = "https://test.ba/3"
            });
        _context.SaveChanges();

        _events = new EventsController(_context);
        _home = new HomeController(Mock.Of<ILogger<HomeController>>());
        TestHelper.SetUser(_home, null);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private async Task<JsonElement> GetEventsJsonAsync()
    {
        var result = await _events.GetEventsJson() as JsonResult;
        return JsonDocument.Parse(JsonSerializer.Serialize(result!.Value)).RootElement;
    }


    [TestMethod]
    [TestCategory("Events")]
    [Priority(1)]
    public async Task Index_ProsliIBuduciDogadjaji_VracaSamoBuduceOdNajranijeg()
    {
        var result = await _events.Index() as ViewResult;
        var model = (List<Event>)result!.Model!;

        CollectionAssert.AreEqual(new[] { 3, 2 }, model.Select(e => e.Id).ToList());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task Index_NemaBuducihDogadjaja_VracaPraznuListu()
    {
        _context.Events.RemoveRange(_context.Events.Where(e => e.Id != 1));
        _context.SaveChanges();

        var result = await _events.Index() as ViewResult;

        Assert.AreEqual(0, ((List<Event>)result!.Model!).Count);
    }


    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_TriDogadjajaUBazi_VracaSvaTriUkljucujuciProsle()
    {
        var json = await GetEventsJsonAsync();

        Assert.AreEqual(3, json.GetArrayLength());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_DogadjajImaEngleskiNaziv_NaslovJeEngleski()
    {
        var json = await GetEventsJsonAsync();

        Assert.AreEqual("Later festival", json[1].GetProperty("title").GetString());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_EngleskiNazivJePrazan_NaslovJeOriginalniNaziv()
    {
        var json = await GetEventsJsonAsync();

        Assert.AreEqual("Raniji koncert", json[2].GetProperty("title").GetString());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_EngleskiNazivJeNull_NaslovJeOriginalniNaziv()
    {
        var json = await GetEventsJsonAsync();

        Assert.AreEqual("Prošli koncert", json[0].GetProperty("title").GetString());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_BiloKojiDogadjaj_DatumiSuUFormatuZaKalendar()
    {
        var json = await GetEventsJsonAsync();

        Assert.AreEqual(2, json[1].GetProperty("id").GetInt32());
        Assert.AreEqual("2099-08-20T21:00:00", json[1].GetProperty("start").GetString());
        Assert.AreEqual("2099-08-20T23:30:00", json[1].GetProperty("end").GetString());
        Assert.AreEqual("https://test.ba/2", json[1].GetProperty("url").GetString());
    }

    [TestMethod]
    [TestCategory("Events")]
    public async Task GetEventsJson_BazaJePrazna_VracaPrazanNiz()
    {
        _context.Events.RemoveRange(_context.Events);
        _context.SaveChanges();

        var json = await GetEventsJsonAsync();

        Assert.AreEqual(0, json.GetArrayLength());
    }


    [TestMethod]
    [TestCategory("Home")]
    public void HomeIndex_BiloKada_BiraJedanOdTriVidea()
    {
        var result = _home.Index() as ViewResult;

        CollectionAssert.Contains(
            new[] { "/Videos/videoHome1.mp4", "/Videos/videoHome2.mp4", "/Videos/videoHome3.mp4" },
            result!.ViewData["SelectedVideo"]);
    }

    [TestMethod]
    [TestCategory("Home")]
    public void Privacy_BiloKada_VracaPogled()
    {
        Assert.IsInstanceOfType(_home.Privacy(), typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Home")]
    public void Error_NemaAktivnosti_IdZahtjevaJeTraceIdentifier()
    {
        _home.HttpContext.TraceIdentifier = "trag-123";

        var result = _home.Error() as ViewResult;

        Assert.AreEqual("trag-123", ((ErrorViewModel)result!.Model!).RequestId);
    }

    [TestMethod]
    [TestCategory("Home")]
    public void Error_PostojiAktivnost_IdZahtjevaJeIdAktivnosti()
    {
        using var activity = new Activity("test-zahtjev").Start();

        var result = _home.Error() as ViewResult;

        Assert.AreEqual(activity.Id, ((ErrorViewModel)result!.Model!).RequestId);
    }
}
