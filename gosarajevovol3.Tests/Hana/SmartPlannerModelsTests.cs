using System.ComponentModel.DataAnnotations;
using gosarajevovol3.Models;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;

namespace gosarajevovol3.Tests.Hana;

[TestClass]
public class SmartPlannerModelsTests
{
    private static bool IsValid(object model)
    {
        return Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void SmartPlannerInputViewModel_NoviObjekat_ImaPodrazumijevaneVrijednosti()
    {
        var model = new SmartPlannerInputViewModel();

        Assert.AreEqual(DateTime.Today, model.ArrivalDate);
        Assert.AreEqual(DateTime.Today.AddDays(1), model.DepartureDate);
        Assert.AreEqual(3, model.HistoryScore);
        Assert.AreEqual(3, model.NatureScore);
        Assert.AreEqual(3, model.CultureScore);
        Assert.AreEqual(3, model.FoodScore);
        Assert.AreEqual(3, model.NightlifeScore);
        Assert.IsNull(model.StartAddress);
        Assert.IsNull(model.StartLat);
        Assert.IsNull(model.StartLng);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [DataRow(1, 1, 1)]
    [DataRow(1, 2, 2)]
    [DataRow(2, 3, 2)]
    [DataRow(3, 4, 4)]
    [DataRow(4, 5, 4)]
    [DataRow(5, 5, 5)]
    [DataRow(1, 5, 3)]
    public void SmartPlannerInputViewModel_DvaOdgovora_RezultatJeZaokruzeniProsjek(int q1, int q2, int ocekivano)
    {
        var model = new SmartPlannerInputViewModel
        {
            HistoryQ1 = q1, HistoryQ2 = q2,
            NatureQ1 = q1, NatureQ2 = q2,
            CultureQ1 = q1, CultureQ2 = q2,
            FoodQ1 = q1, FoodQ2 = q2,
            NightlifeQ1 = q1, NightlifeQ2 = q2
        };

        Assert.AreEqual(ocekivano, model.HistoryScore);
        Assert.AreEqual(ocekivano, model.NatureScore);
        Assert.AreEqual(ocekivano, model.CultureScore);
        Assert.AreEqual(ocekivano, model.FoodScore);
        Assert.AreEqual(ocekivano, model.NightlifeScore);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [TestCategory("DataDriven")]
    [CsvDataSource("hana_broj_dana.csv")]
    public void SmartPlannerInputViewModel_DatumiIzCsvFajla_RacunaBrojDanaIIspravnostDatuma(
        DateTime dolazak, DateTime odlazak, int ocekivaniBrojDana, bool datumiIspravni)
    {
        var model = new SmartPlannerInputViewModel { ArrivalDate = dolazak, DepartureDate = odlazak };

        Assert.AreEqual(ocekivaniBrojDana, model.NumberOfDays);
        Assert.AreEqual(datumiIspravni, model.DatesAreValid);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(5, true)]
    [DataRow(6, false)]
    public void SmartPlannerInputViewModel_OdgovorNaPitanje_ValidanSamoIzmedjuJedanIPet(int odgovor, bool ocekivano)
    {
        var model = new SmartPlannerInputViewModel { FoodQ1 = odgovor };

        Assert.AreEqual(ocekivano, IsValid(model));
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void SmartPlannerResultViewModel_NoviObjekat_ImaPrazanPlan()
    {
        var model = new SmartPlannerResultViewModel();

        Assert.IsNotNull(model.PlanByDay);
        Assert.AreEqual(0, model.PlanByDay.Count);
        Assert.AreEqual(0, model.NumberOfDays);
        Assert.IsNull(model.StartAddress);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void PlannerLocationViewModel_NoviObjekat_TekstualnaPoljaSuPrazna()
    {
        var model = new PlannerLocationViewModel();

        Assert.AreEqual(string.Empty, model.Name);
        Assert.AreEqual(string.Empty, model.Description);
        Assert.AreEqual(string.Empty, model.ImageUrl);
        Assert.AreEqual(string.Empty, model.Category);
        Assert.AreEqual(string.Empty, model.Type);
        Assert.AreEqual(0, model.Latitude);
        Assert.AreEqual(0, model.Longitude);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void SmartPlanner_NoviObjekat_OdlazakJeDanNakonDolaskaIListeSuPrazne()
    {
        var planner = new SmartPlanner();

        Assert.IsTrue(planner.DepartureDate > planner.ArrivalDate);
        Assert.AreEqual(0, planner.Attractions.Count);
        Assert.AreEqual(0, planner.HospitalitySites.Count);
        Assert.AreEqual(0, planner.Events.Count);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void SmartPlanner_PostavljenaSvojstva_VracajuPostavljeneVrijednosti()
    {
        var korisnik = new RegisteredUser { Id = 7 };
        var preferenca = new Preference { Id = 3 };

        var planner = new SmartPlanner
        {
            Id = 1,
            RegisteredUserId = 7,
            RegisteredUser = korisnik,
            PreferenceId = 3,
            Preference = preferenca
        };

        Assert.AreEqual(1, planner.Id);
        Assert.AreEqual(7, planner.RegisteredUserId);
        Assert.AreSame(korisnik, planner.RegisteredUser);
        Assert.AreEqual(3, planner.PreferenceId);
        Assert.AreSame(preferenca, planner.Preference);
    }

    [TestMethod]
    [TestCategory("Model")]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(3, true)]
    [DataRow(5, true)]
    [DataRow(6, false)]
    public void Preference_NivoInteresovanja_ValidanSamoIzmedjuJedanIPet(int nivo, bool ocekivano)
    {
        var preferenca = new Preference
        {
            History = nivo, TraditionalFood = nivo, Nature = nivo, Culture = nivo, Nightlife = nivo,
            RegisteredUserId = 1,
            RegisteredUser = new RegisteredUser()
        };

        Assert.AreEqual(ocekivano, IsValid(preferenca));
        Assert.AreEqual(nivo, preferenca.History);
        Assert.AreEqual(nivo, preferenca.TraditionalFood);
        Assert.AreEqual(nivo, preferenca.Nature);
        Assert.AreEqual(nivo, preferenca.Culture);
        Assert.AreEqual(nivo, preferenca.Nightlife);
        Assert.AreEqual(1, preferenca.RegisteredUserId);
    }
}
