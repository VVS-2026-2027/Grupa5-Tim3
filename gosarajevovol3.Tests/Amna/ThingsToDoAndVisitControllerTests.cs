using gosarajevovol2.Models.Enums;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace gosarajevovol3.Tests.Amna;

[TestClass]
public class ThingsToDoAndVisitControllerTests
{
    private ApplicationDbContext _context = null!;
    private ThingsToDoController _thingsToDo = null!;
    private VisitController _visit = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        SeedHospitality();
        SeedAttractions();

        _thingsToDo = new ThingsToDoController(_context);
        _visit = new VisitController(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedHospitality()
    {
        var korisnik = new RegisteredUser { Id = 50, Email = "gost@test.ba", UserName = "gost@test.ba" };
        _context.RegisteredUsers.Add(korisnik);

        _context.Hospitality.AddRange(
            NewHospitality(1, "Inat kuća", HospitalityType.Restaurant),
            NewHospitality(2, "Dveri", HospitalityType.Restaurant),
            NewHospitality(3, "Kawa", HospitalityType.Cafe),
            NewHospitality(4, "Zlatna ribica", HospitalityType.Bars),
            NewHospitality(5, "Cinema City", HospitalityType.Cinemas),
            NewHospitality(6, "Zemaljski muzej", HospitalityType.Museums));

        _context.Reviews.Add(new Review { Id = 1, HospitalityId = 1, RegisteredUserId = 50, Rating = 5, Comment = "Odlično" });
        _context.SaveChanges();
    }

    private static Hospitality NewHospitality(int id, string name, HospitalityType type)
    {
        return new Hospitality
        {
            Id = id,
            HospitalityName = name,
            HospitalityDescription = "Opis",
            HospitalityType = type,
            LocationAddress = "Adresa"
        };
    }

    private void SeedAttractions()
    {
        _context.Attractions.AddRange(
            NewAttraction(1, "Vijećnica", AttractionType.Historical),
            NewAttraction(2, "Baščaršija", AttractionType.Historical),
            NewAttraction(3, "Begova džamija", AttractionType.Religious),
            NewAttraction(4, "Vječna vatra", AttractionType.Monument),
            NewAttraction(5, "Vrelo Bosne", AttractionType.Nature),
            NewAttraction(6, "Trebević", AttractionType.Nature),
            NewAttraction(7, "Skakavac", AttractionType.Nature));
        _context.SaveChanges();
    }

    private static Attraction NewAttraction(int id, string name, AttractionType type)
    {
        return new Attraction
        {
            Id = id,
            AttractionName = name,
            AttractionDescription = "Opis",
            AttractionType = type,
            LocationAddress = "Adresa",
            Coordinates = new Coordinates { Latitude = 43.85, Longitude = 18.41 }
        };
    }


    [TestMethod]
    [TestCategory("ThingsToDo")]
    [Priority(1)]
    public async Task Index_ObjektiRazlicitihTipova_SvakaKategorijaDobijaSvojeObjekte()
    {
        var result = await _thingsToDo.Index() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual(2, ((List<Hospitality>)result.ViewData["Restaurants"]!).Count);
        Assert.AreEqual(1, ((List<Hospitality>)result.ViewData["Cafes"]!).Count);
        Assert.AreEqual(1, ((List<Hospitality>)result.ViewData["Bars"]!).Count);
        Assert.AreEqual(0, ((List<Hospitality>)result.ViewData["Shopping"]!).Count);
        Assert.AreEqual(1, ((List<Hospitality>)result.ViewData["Cinemas"]!).Count);
        Assert.AreEqual(1, ((List<Hospitality>)result.ViewData["Museums"]!).Count);
    }

    [TestMethod]
    [TestCategory("ThingsToDo")]
    [TestCategory("DataDriven")]
    [CsvDataSource("amna_tipovi_objekata.csv")]
    public async Task Hospitality_TipIzCsvFajla_VracaSamoObjekteTogTipa(HospitalityType tip, int ocekivaniBroj)
    {
        var result = await _thingsToDo.Hospitality(tip) as ViewResult;
        var objekti = (List<Hospitality>)result!.Model!;

        Assert.AreEqual(ocekivaniBroj, objekti.Count);
        Assert.AreEqual(tip.ToString(), result.ViewData["CategoryName"]);
        Assert.IsTrue(objekti.All(o => o.HospitalityType == tip));
    }

    [TestMethod]
    [TestCategory("ThingsToDo")]
    public async Task HospitalityDetails_PostojeciObjekat_VracaObjekatSaRecenzijamaIAutorom()
    {
        var result = await _thingsToDo.HospitalityDetails(1) as ViewResult;
        var objekat = result!.Model as Hospitality;

        Assert.IsNotNull(objekat);
        Assert.AreEqual("Inat kuća", objekat.HospitalityName);
        Assert.AreEqual(1, objekat.Reviews.Count);
        Assert.AreEqual("gost@test.ba", objekat.Reviews[0].RegisteredUser.Email);
    }

    [TestMethod]
    [TestCategory("ThingsToDo")]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(999)]
    public async Task HospitalityDetails_NepostojeciId_VracaNotFound(int id)
    {
        var result = await _thingsToDo.HospitalityDetails(id);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }


    [TestMethod]
    [TestCategory("Visit")]
    public void VisitIndex_BezTipa_VracaSveAtrakcijeSaKoordinatama()
    {
        var result = _visit.Index() as ViewResult;
        var atrakcije = (List<Attraction>)result!.Model!;

        Assert.AreEqual(7, atrakcije.Count);
        Assert.IsNull(result.ViewData["ActiveType"]);
        CollectionAssert.AllItemsAreNotNull(atrakcije.Select(a => a.Coordinates).ToList());
    }

    [TestMethod]
    [TestCategory("Visit")]
    [TestCategory("DataDriven")]
    [CsvDataSource("amna_tipovi_atrakcija.csv")]
    public void VisitIndex_TipIzCsvFajla_VracaOcekivaniBrojAtrakcija(string tip, int ocekivaniBroj)
    {
        var result = _visit.Index(tip) as ViewResult;
        var atrakcije = (List<Attraction>)result!.Model!;

        Assert.AreEqual(ocekivaniBroj, atrakcije.Count);
        Assert.AreEqual(tip, result.ViewData["ActiveType"]);
    }

    [TestMethod]
    [TestCategory("Visit")]
    public void VisitIndex_TipNature_SveVraceneAtrakcijeSuPrirodne()
    {
        var result = _visit.Index("Nature") as ViewResult;
        var nazivi = ((List<Attraction>)result!.Model!).Select(a => a.AttractionName).ToList();

        CollectionAssert.AreEquivalent(new[] { "Vrelo Bosne", "Trebević", "Skakavac" }, nazivi);
    }

    [TestMethod]
    [TestCategory("Visit")]
    public void VisitDetails_PostojecaAtrakcija_VracaAtrakcijuSaKoordinatama()
    {
        var result = _visit.Details(1) as ViewResult;
        var atrakcija = result!.Model as Attraction;

        Assert.IsNotNull(atrakcija);
        Assert.AreEqual("Vijećnica", atrakcija.AttractionName);
        Assert.AreEqual(43.85, atrakcija.Coordinates.Latitude);
        Assert.AreEqual(18.41, atrakcija.Coordinates.Longitude);
    }

    [TestMethod]
    [TestCategory("Visit")]
    public void VisitDetails_NepostojecaAtrakcija_VracaNotFound()
    {
        var result = _visit.Details(999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }


    [TestMethod]
    [TestCategory("Information")]
    public void InformationIndex_Poziv_VracaView()
    {
        var controller = new InformationController();

        var result = controller.Index();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Information")]
    [DataRow("tram", "3")]
    [DataRow("bus", "31E")]
    [DataRow(null, null)]
    public void InformationSchedule_TipILinija_ProsljedjujuSePogledu(string? tip, string? linija)
    {
        var controller = new InformationController();

        var result = controller.Schedule(tip!, linija!) as ViewResult;

        Assert.AreEqual(tip, result!.ViewData["Type"]);
        Assert.AreEqual(linija, result.ViewData["Line"]);
    }
}
