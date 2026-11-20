using System.ComponentModel.DataAnnotations;
using gosarajevovol2.Models.Enums;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;

namespace gosarajevovol3.Tests.Amna;

[TestClass]
public class HospitalityModelsTests
{
    private static bool IsValid(object model)
    {
        return Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
    }

    public static IEnumerable<object?[]> ObjektiIzXmlFajla()
    {
        return XmlTestData.Read("amna_validacija_objekata.xml",
            ("naziv", typeof(string)), ("opisObjekta", typeof(string)), ("adresa", typeof(string)),
            ("duzinaUrlaSlike", typeof(int)), ("validno", typeof(bool)));
    }

    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(ObjektiIzXmlFajla), DynamicDataSourceType.Method)]
    public void Hospitality_PodaciIzXmlFajla_ValidacijaDajeOcekivaniRezultat(
        string naziv, string opis, string adresa, int duzinaUrlaSlike, bool ocekivano)
    {
        var objekat = new Hospitality
        {
            HospitalityName = naziv,
            HospitalityDescription = opis,
            LocationAddress = adresa,
            PhotoUrl = new string('a', duzinaUrlaSlike),
            HospitalityType = HospitalityType.Restaurant
        };

        Assert.AreEqual(ocekivano, IsValid(objekat));
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Hospitality_NoviObjekat_ImaPrazneListeIPodrazumijevaneVrijednosti()
    {
        var objekat = new Hospitality();

        Assert.AreEqual(string.Empty, objekat.HospitalityName);
        Assert.AreEqual(string.Empty, objekat.PhotoUrl);
        Assert.IsNull(objekat.GooglePlaceId);
        Assert.IsNotNull(objekat.Coordinates);
        Assert.AreEqual(0, objekat.Reviews.Count);
        Assert.AreEqual(0, objekat.CoordinatesId);
    }

    [TestMethod]
    [TestCategory("Model")]
    [DataRow(200, true, DisplayName = "Naziv na granici (200 znakova)")]
    [DataRow(201, false, DisplayName = "Naziv predug (201 znak)")]
    public void Attraction_DuzinaNaziva_ValidnaDoDvjestoZnakova(int duzina, bool ocekivano)
    {
        var atrakcija = new Attraction
        {
            AttractionName = new string('a', duzina),
            AttractionDescription = "Opis",
            LocationAddress = "Adresa",
            AttractionType = AttractionType.Nature
        };

        Assert.AreEqual(ocekivano, IsValid(atrakcija));
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Attraction_BezObaveznihPolja_NijeValidna()
    {
        var atrakcija = new Attraction();

        Assert.IsFalse(IsValid(atrakcija));
        Assert.IsNotNull(atrakcija.Coordinates);
        Assert.AreEqual(0, atrakcija.CoordinatesId);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Coordinates_PostavljeneVrijednosti_VracajuIsteVrijednosti()
    {
        var koordinate = new Coordinates { Id = 4, Latitude = 43.8563, Longitude = 18.4131 };

        Assert.AreEqual(4, koordinate.Id);
        Assert.AreEqual(43.8563, koordinate.Latitude);
        Assert.AreEqual(18.4131, koordinate.Longitude);
    }

    [TestMethod]
    [TestCategory("Model")]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(5, true)]
    [DataRow(6, false)]
    public void Review_Ocjena_ValidnaSamoIzmedjuJedanIPet(int ocjena, bool ocekivano)
    {
        var recenzija = new Review { Rating = ocjena, Comment = "Komentar", RegisteredUser = new RegisteredUser() };

        Assert.AreEqual(ocekivano, IsValid(recenzija));
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Review_NovaRecenzija_ImaDatumKreiranjaINemaObjekat()
    {
        var prije = DateTime.UtcNow.AddSeconds(-1);

        var recenzija = new Review { Id = 3, RegisteredUserId = 8 };

        Assert.IsTrue(recenzija.CreatedAt >= prije);
        Assert.AreEqual(string.Empty, recenzija.Comment);
        Assert.IsNull(recenzija.HospitalityId);
        Assert.IsNull(recenzija.Hospitality);
        Assert.AreEqual(3, recenzija.Id);
        Assert.AreEqual(8, recenzija.RegisteredUserId);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void ReviewFormViewModel_NoviObjekat_ImaPrazneVrijednosti()
    {
        var model = new ReviewFormViewModel();

        Assert.IsNull(model.ReviewId);
        Assert.AreEqual(0, model.HospitalityId);
        Assert.AreEqual(string.Empty, model.HospitalityName);
        Assert.AreEqual(0, model.Rating);
        Assert.AreEqual(string.Empty, model.Comment);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void HospitalityType_Enumeracija_ImaSestTipovaObjekata()
    {
        var tipovi = Enum.GetNames(typeof(HospitalityType));

        CollectionAssert.AreEqual(
            new[] { "Restaurant", "Cafe", "Bars", "Shopping", "Cinemas", "Museums" },
            tipovi);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void AttractionType_Enumeracija_ImaPetTipovaAtrakcija()
    {
        var tipovi = Enum.GetNames(typeof(AttractionType));

        CollectionAssert.AreEqual(
            new[] { "Historical", "Religious", "Monument", "Nature", "Other" },
            tipovi);
    }
}
