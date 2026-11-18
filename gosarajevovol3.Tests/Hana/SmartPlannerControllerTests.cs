using gosarajevovol2.Models.Enums;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace gosarajevovol3.Tests.Hana;

/// <summary>
/// Autor testova: Hana.
/// Testira se SmartPlannerController - generisanje plana obilaska po danima.
/// </summary>
[TestClass]
public class SmartPlannerControllerTests
{
    private const string RegisteredEmail = "hana@test.ba";
    private const string TestApiKey = "TEST-KLJUC";

    private ApplicationDbContext _context = null!;
    private Mock<IConfiguration> _configMock = null!;
    private SmartPlannerController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 10, Email = RegisteredEmail, UserName = RegisteredEmail });
        _context.SaveChanges();

        // Zamjenski objekat za konfiguraciju: umjesto fajla appsettings.json vraća testni ključ.
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["GoogleMaps:ApiKey"]).Returns(TestApiKey);

        _controller = new SmartPlannerController(_context, _configMock.Object);
        TestHelper.SetUser(_controller, RegisteredEmail);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    // ---------- pomoćne metode za pripremu podataka ----------

    private static SmartPlannerInputViewModel InputForDays(int days)
    {
        return new SmartPlannerInputViewModel
        {
            ArrivalDate = new DateTime(2026, 7, 1),
            DepartureDate = new DateTime(2026, 7, 1).AddDays(days - 1)
        };
    }

    private void AddAttractions(AttractionType type, int count)
    {
        _context.Attractions.AddRange(Enumerable.Range(1, count).Select(i => new Attraction
        {
            AttractionName = $"{type} atrakcija {i}",
            AttractionDescription = "Opis",
            AttractionType = type,
            LocationAddress = "Adresa",
            Coordinates = new Coordinates { Latitude = 43.85 + i * 0.001, Longitude = 18.41 + i * 0.001 }
        }));
        _context.SaveChanges();
    }

    private void AddHospitality(HospitalityType type, int count)
    {
        _context.Hospitality.AddRange(Enumerable.Range(1, count).Select(i => new Hospitality
        {
            HospitalityName = $"{type} objekat {i}",
            HospitalityDescription = "Opis",
            HospitalityType = type,
            LocationAddress = "Adresa",
            Coordinates = new Coordinates { Latitude = 43.86 + i * 0.001, Longitude = 18.42 + i * 0.001 }
        }));
        _context.SaveChanges();
    }

    private void AddEvent(string name, EventType type, DateTime start, string? nameEn = null, string? descriptionEn = null)
    {
        _context.Events.Add(new Event
        {
            EventName = name,
            EventNameEn = nameEn,
            EventDescription = "Opis događaja",
            EventDescriptionEn = descriptionEn,
            Type = type,
            StartDate = start,
            EndDate = start.AddHours(2),
            LocationAddress = "Adresa",
            Coordinates = new Coordinates { Latitude = 43.857, Longitude = 18.415 }
        });
        _context.SaveChanges();
    }

    /// <summary>Dovoljno podataka da se svaki dan može popuniti sa svih 5 lokacija.</summary>
    private void AddRichData()
    {
        AddAttractions(AttractionType.Historical, 4);
        AddAttractions(AttractionType.Religious, 4);
        AddAttractions(AttractionType.Monument, 4);
        AddAttractions(AttractionType.Nature, 4);
        AddAttractions(AttractionType.Other, 4);
        AddHospitality(HospitalityType.Restaurant, 6);
        AddHospitality(HospitalityType.Museums, 4);
        AddHospitality(HospitalityType.Bars, 4);
    }

    private async Task<SmartPlannerResultViewModel> GeneratePlanAsync(SmartPlannerInputViewModel input)
    {
        var result = (ViewResult)await _controller.GenerirajPlan(input);
        return (SmartPlannerResultViewModel)result.Model!;
    }

    // ---------- Index ----------

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public void Index_NeprijavljenKorisnik_VracaNotRegisteredView()
    {
        TestHelper.SetUser(_controller, null);

        var result = _controller.Index() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("NotRegistered", result.ViewName);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public void Index_PrijavljenKorisnikKojiNijeRegistrovan_VracaNotRegisteredView()
    {
        TestHelper.SetUser(_controller, "admin@test.ba");

        var result = _controller.Index() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("NotRegistered", result.ViewName);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [Priority(1)]
    public void Index_RegistrovanKorisnik_VracaFormuSaPraznimModelom()
    {
        var result = _controller.Index() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsNull(result.ViewName);
        Assert.IsInstanceOfType(result.Model, typeof(SmartPlannerInputViewModel));
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [TestCategory("Mock")]
    public void Index_RegistrovanKorisnik_CitaKljucMapeIzKonfiguracije()
    {
        var result = _controller.Index() as ViewResult;

        Assert.AreEqual(TestApiKey, result!.ViewData["GoogleMapsApiKey"]);
        _configMock.Verify(c => c["GoogleMaps:ApiKey"], Times.Once);
    }

    // ---------- GenerirajPlan: provjere pristupa i unosa ----------

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_NeprijavljenKorisnik_VracaNotRegisteredView()
    {
        TestHelper.SetUser(_controller, null);

        var result = await _controller.GenerirajPlan(InputForDays(2)) as ViewResult;

        Assert.AreEqual("NotRegistered", result!.ViewName);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_KorisnikNijeRegistrovan_VracaNotRegisteredView()
    {
        TestHelper.SetUser(_controller, "nepoznat@test.ba");

        var result = await _controller.GenerirajPlan(InputForDays(2)) as ViewResult;

        Assert.AreEqual("NotRegistered", result!.ViewName);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_NeispravanModel_VracaFormuSaIstimUnosom()
    {
        var input = InputForDays(2);
        _controller.ModelState.AddModelError("HistoryQ1", "Vrijednost mora biti između 1 i 5.");

        var result = await _controller.GenerirajPlan(input) as ViewResult;

        Assert.AreEqual("Index", result!.ViewName);
        Assert.AreSame(input, result.Model);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_OdlazakPrijeDolaska_VracaFormuSaGreskomZaDatum()
    {
        var input = new SmartPlannerInputViewModel
        {
            ArrivalDate = new DateTime(2026, 7, 5),
            DepartureDate = new DateTime(2026, 7, 1)
        };

        var result = await _controller.GenerirajPlan(input) as ViewResult;

        Assert.AreEqual("Index", result!.ViewName);
        Assert.IsTrue(_controller.ModelState.ContainsKey("DepartureDate"));
        Assert.AreEqual(
            "Departure date must be after the arrival date.",
            _controller.ModelState["DepartureDate"]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [TestCategory("Izuzeci")]
    [ExpectedException(typeof(NullReferenceException))]
    public async Task GenerirajPlan_UnosJeNull_BacaNullReferenceException()
    {
        await _controller.GenerirajPlan(null!);
    }

    // ---------- GenerirajPlan: rezultat ----------

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [Priority(1)]
    public async Task GenerirajPlan_IspravanUnos_VracaResultViewSaPodacimaOPutovanju()
    {
        var input = InputForDays(3);
        input.StartAddress = "Ferhadija 1";
        input.StartLat = 43.8587;
        input.StartLng = 18.4250;

        var result = await _controller.GenerirajPlan(input) as ViewResult;
        var model = result!.Model as SmartPlannerResultViewModel;

        Assert.AreEqual("Result", result.ViewName);
        Assert.IsNotNull(model);
        Assert.AreEqual(input.ArrivalDate, model.ArrivalDate);
        Assert.AreEqual(input.DepartureDate, model.DepartureDate);
        Assert.AreEqual(3, model.NumberOfDays);
        Assert.AreEqual("Ferhadija 1", model.StartAddress);
        Assert.AreEqual(43.8587, model.StartLat);
        Assert.AreEqual(18.4250, model.StartLng);
        Assert.AreEqual(TestApiKey, result.ViewData["GoogleMapsApiKey"]);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    public async Task GenerirajPlan_ViseDana_PlanImaPoJedanUnosZaSvakiDan(int brojDana)
    {
        AddRichData();

        var model = await GeneratePlanAsync(InputForDays(brojDana));

        Assert.AreEqual(brojDana, model.PlanByDay.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, brojDana).ToList(), model.PlanByDay.Keys.ToList());
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_PraznaBaza_SvakiDanImaPraznuListu()
    {
        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(0, model.PlanByDay[1].Count);
        Assert.AreEqual(0, model.PlanByDay[2].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DovoljnoPodataka_SvakiDanImaTacnoPetLokacija()
    {
        AddRichData();

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(5, model.PlanByDay[1].Count);
        Assert.AreEqual(5, model.PlanByDay[2].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_ViseDana_IstaLokacijaSeNePonavlja()
    {
        AddRichData();

        var model = await GeneratePlanAsync(InputForDays(3));
        var sviNazivi = model.PlanByDay.Values.SelectMany(dan => dan).Select(l => l.Name).ToList();

        CollectionAssert.AllItemsAreUnique(sviNazivi);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_SamoAtrakcije_LokacijaSadrziPodatkeAtrakcije()
    {
        AddAttractions(AttractionType.Other, 1);

        var model = await GeneratePlanAsync(InputForDays(2));
        var lokacija = model.PlanByDay[1].Single();

        Assert.AreEqual("Other atrakcija 1", lokacija.Name);
        Assert.AreEqual("Attraction", lokacija.Category);
        Assert.AreEqual("Other", lokacija.Type);
        Assert.AreEqual(43.851, lokacija.Latitude, 0.0001);
        Assert.AreEqual(18.411, lokacija.Longitude, 0.0001);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_SamoRestorani_PrvaLokacijaJeUgostiteljskiObjekat()
    {
        AddHospitality(HospitalityType.Restaurant, 3);

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual("Hospitality", model.PlanByDay[1][0].Category);
        Assert.AreEqual("Restaurant", model.PlanByDay[1][0].Type);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_KaficiIKupovina_NeUlazeUPlan()
    {
        AddHospitality(HospitalityType.Cafe, 3);
        AddHospitality(HospitalityType.Shopping, 3);
        AddHospitality(HospitalityType.Cinemas, 3);

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(0, model.PlanByDay[1].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_PostojiBar_BarJePosljednjaLokacijaDana()
    {
        AddRichData();

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual("Bars", model.PlanByDay[1].Last().Type);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_NocniZivotNajnizaOcjena_BarNijeUPlanu()
    {
        AddRichData();
        var input = InputForDays(2);
        input.NightlifeQ1 = 1;
        input.NightlifeQ2 = 1;

        var model = await GeneratePlanAsync(input);
        var tipovi = model.PlanByDay[1].Select(l => l.Type).ToList();

        CollectionAssert.DoesNotContain(tipovi, "Bars");
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_SamoHistorijskeAtrakcije_NajviseDvijeIstogTipaDnevno()
    {
        AddAttractions(AttractionType.Historical, 10);

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(2, model.PlanByDay[1].Count(l => l.Type == "Historical"));
    }

    // ---------- GenerirajPlan: događaji ----------

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_VecernjiKoncertNaDanDolaska_KoncertJeNaKrajuPrvogDana()
    {
        AddRichData();
        AddEvent("Koncert u Zetri", EventType.Concert, new DateTime(2026, 7, 1, 20, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(5, model.PlanByDay[1].Count);
        Assert.AreEqual("Koncert u Zetri", model.PlanByDay[1][4].Name);
        Assert.AreEqual("Event", model.PlanByDay[1][4].Category);
        Assert.AreEqual("Concert", model.PlanByDay[1][4].Type);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [DataRow(9, 0, DisplayName = "Jutarnji događaj je prvi u danu")]
    [DataRow(14, 2, DisplayName = "Popodnevni događaj je treći u danu")]
    [DataRow(19, 4, DisplayName = "Večernji događaj je posljednji u danu")]
    [DataRow(0, 4, DisplayName = "Događaj bez vremena (00:00) računa se kao večernji")]
    public async Task GenerirajPlan_PredstavaUOdredjenoVrijeme_DogadjajJeNaOcekivanojPoziciji(int sat, int ocekivanaPozicija)
    {
        AddRichData();
        AddEvent("Predstava", EventType.Theater, new DateTime(2026, 7, 1, sat, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual("Predstava", model.PlanByDay[1][ocekivanaPozicija].Name);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_KoncertINocniZivotNajvisaOcjena_UPlanuSuIBarIKoncert()
    {
        AddRichData();
        AddEvent("Koncert", EventType.Concert, new DateTime(2026, 7, 1, 21, 0, 0));
        var input = InputForDays(2);
        input.NightlifeQ1 = 5;
        input.NightlifeQ2 = 5;

        var model = await GeneratePlanAsync(input);
        var tipovi = model.PlanByDay[1].Select(l => l.Type).ToList();

        CollectionAssert.Contains(tipovi, "Bars");
        CollectionAssert.Contains(tipovi, "Concert");
        Assert.AreEqual(5, model.PlanByDay[1].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DogadjajImaEngleskiPrevod_KoristiSeEngleskiNazivIOpis()
    {
        AddEvent("Hamlet predstava", EventType.Theater, new DateTime(2026, 7, 1, 20, 0, 0), "Hamlet play", "Play description");

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual("Hamlet play", model.PlanByDay[1][0].Name);
        Assert.AreEqual("Play description", model.PlanByDay[1][0].Description);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DogadjajBezPrevoda_KoristiSeOriginalniNazivIOpis()
    {
        AddEvent("Hamlet predstava", EventType.Theater, new DateTime(2026, 7, 1, 20, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual("Hamlet predstava", model.PlanByDay[1][0].Name);
        Assert.AreEqual("Opis događaja", model.PlanByDay[1][0].Description);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DogadjajVanTerminaPutovanja_NijeUPlanu()
    {
        AddEvent("Kasniji koncert", EventType.Concert, new DateTime(2026, 8, 15, 20, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(0, model.PlanByDay[1].Count);
        Assert.AreEqual(0, model.PlanByDay[2].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DogadjajTipaOther_ImaPrenizakRezultatINijeUPlanu()
    {
        AddEvent("Sajam", EventType.Other, new DateTime(2026, 7, 1, 18, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(0, model.PlanByDay[1].Count);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [DataRow(EventType.Movie)]
    [DataRow(EventType.Festival)]
    [DataRow(EventType.Theater)]
    public async Task GenerirajPlan_DogadjajSaDovoljnimRezultatom_UlaziUPlan(EventType tip)
    {
        AddEvent("Događaj", tip, new DateTime(2026, 7, 1, 18, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(tip.ToString(), model.PlanByDay[1].Single().Type);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public async Task GenerirajPlan_DvaDogadjajaIstogDana_UPlanJeSamoJedanDogadjajDnevno()
    {
        AddEvent("Prva predstava", EventType.Theater, new DateTime(2026, 7, 1, 18, 0, 0));
        AddEvent("Druga predstava", EventType.Theater, new DateTime(2026, 7, 1, 20, 0, 0));

        var model = await GeneratePlanAsync(InputForDays(2));

        Assert.AreEqual(1, model.PlanByDay[1].Count(l => l.Category == "Event"));
        Assert.AreEqual("Prva predstava", model.PlanByDay[1].Single().Name);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [Timeout(5000)]
    public async Task GenerirajPlan_TridesetDana_ZavrsavaUnutarVremenskogOgranicenja()
    {
        AddRichData();

        var model = await GeneratePlanAsync(InputForDays(30));

        Assert.AreEqual(30, model.PlanByDay.Count);
    }

    // ---------- pomoćne metode kontrolera ----------

    [TestMethod]
    [TestCategory("SmartPlanner")]
    [DataRow("Nature", 5, 2, DisplayName = "Priroda sa visokom ocjenom - dvije dnevno")]
    [DataRow("Nature", 4, 2, DisplayName = "Priroda sa ocjenom 4 - dvije dnevno (granica)")]
    [DataRow("Nature", 3, 1, DisplayName = "Priroda sa ocjenom 3 - jedna dnevno")]
    [DataRow("Historical", 3, 2)]
    [DataRow("Religious", 3, 2)]
    [DataRow("Monument", 3, 2)]
    [DataRow("Other", 3, 5)]
    [DataRow("Restaurant", 3, 5)]
    public void GetDailyTypeLimit_TipLokacije_VracaDnevnoOgranicenje(string tip, int ocjenaPrirode, int ocekivano)
    {
        var input = new SmartPlannerInputViewModel { NatureQ1 = ocjenaPrirode, NatureQ2 = ocjenaPrirode };

        int limit = _controller.GetDailyTypeLimit(tip, input);

        Assert.AreEqual(ocekivano, limit);
    }

    /// <summary>
    /// Data-driven test sa podacima iz CSV fajla (izvor podataka).
    /// </summary>
    [TestMethod]
    [TestCategory("SmartPlanner")]
    [TestCategory("DataDriven")]
    [CsvDataSource("hana_hospitality_slots.csv")]
    public void CalculateHospitalitySlots_OcjeneIzCsvFajla_VracaOcekivaniBrojMjesta(
        int history, int nature, int culture, int food, int nightlife, int ocekivano)
    {
        var input = new SmartPlannerInputViewModel
        {
            HistoryQ1 = history, HistoryQ2 = history,
            NatureQ1 = nature, NatureQ2 = nature,
            CultureQ1 = culture, CultureQ2 = culture,
            FoodQ1 = food, FoodQ2 = food,
            NightlifeQ1 = nightlife, NightlifeQ2 = nightlife
        };

        int slots = _controller.CalculateHospitalitySlots(input);

        Assert.AreEqual(ocekivano, slots);
    }

    public static IEnumerable<object?[]> HaversinePodaci()
    {
        return XmlTestData.Read("hana_haversine.xml",
            ("lat1", typeof(double)), ("lon1", typeof(double)),
            ("lat2", typeof(double)), ("lon2", typeof(double)),
            ("ocekivano", typeof(double)), ("tolerancija", typeof(double)));
    }

    /// <summary>
    /// Data-driven test sa podacima iz XML fajla (DynamicData).
    /// </summary>
    [TestMethod]
    [TestCategory("SmartPlanner")]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(HaversinePodaci), DynamicDataSourceType.Method)]
    public void Haversine_KoordinateIzXmlFajla_VracaUdaljenostUMetrima(
        double lat1, double lon1, double lat2, double lon2, double ocekivano, double tolerancija)
    {
        double udaljenost = SmartPlannerController.Haversine(lat1, lon1, lat2, lon2);

        Assert.AreEqual(ocekivano, udaljenost, tolerancija);
    }

    [TestMethod]
    [TestCategory("SmartPlanner")]
    public void Haversine_ZamijenjeneTacke_UdaljenostJeIsta()
    {
        double tamo = SmartPlannerController.Haversine(43.8563, 18.4131, 43.3438, 17.8078);
        double nazad = SmartPlannerController.Haversine(43.3438, 17.8078, 43.8563, 18.4131);

        Assert.AreEqual(tamo, nazad, 0.001);
    }
}
