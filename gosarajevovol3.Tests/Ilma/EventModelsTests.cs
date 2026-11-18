using System.ComponentModel.DataAnnotations;
using gosarajevovol2.Models.Enums;
using gosarajevovol3.Models;

namespace gosarajevovol3.Tests.Ilma;

/// <summary>
/// Autor testova: Ilma.
/// Testiraju se modeli Event, Newsletter i ErrorViewModel, te enumeracije EventType i NewsletterStatus.
/// </summary>
[TestClass]
public class EventModelsTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static Event ValidEvent()
    {
        return new Event { EventName = "Koncert", EventDescription = "Opis", LocationAddress = "Zetra" };
    }

    // ---------- Event ----------

    [TestMethod]
    [TestCategory("Model")]
    public void Event_NoviObjekat_ImaPrazneTekstoveIKoordinate()
    {
        var ev = new Event();

        Assert.AreEqual(string.Empty, ev.EventName);
        Assert.AreEqual(string.Empty, ev.PhotoUrl);
        Assert.AreEqual(string.Empty, ev.WebUrl);
        Assert.IsNull(ev.EventNameEn);
        Assert.IsNotNull(ev.Coordinates);
        Assert.AreEqual(EventType.Concert, ev.Type);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Event_SviObavezniPodaciUneseni_ProlaziValidaciju()
    {
        var errors = Validate(ValidEvent());

        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Data-driven test (DataRow): granične dužine naziva i adrese događaja.
    /// </summary>
    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow(200, 250, true, DisplayName = "Naziv 200 i adresa 250 znakova (gornje granice)")]
    [DataRow(201, 250, false, DisplayName = "Naziv 201 znak")]
    [DataRow(200, 251, false, DisplayName = "Adresa 251 znak")]
    [DataRow(0, 10, false, DisplayName = "Prazan naziv")]
    [DataRow(10, 0, false, DisplayName = "Prazna adresa")]
    public void Event_DuzinaNazivaIAdrese_ValidacijaDajeOcekivaniRezultat(int duzinaNaziva, int duzinaAdrese, bool ocekivano)
    {
        var ev = ValidEvent();
        ev.EventName = new string('n', duzinaNaziva);
        ev.LocationAddress = new string('a', duzinaAdrese);

        var errors = Validate(ev);

        Assert.AreEqual(ocekivano, errors.Count == 0);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Event_PrazanOpis_NeProlaziValidaciju()
    {
        var ev = ValidEvent();
        ev.EventDescription = "";

        var errors = Validate(ev);

        CollectionAssert.Contains(errors.Single().MemberNames.ToList(), nameof(Event.EventDescription));
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Event_PredugEngleskiNaziv_NeProlaziValidaciju()
    {
        var ev = ValidEvent();
        ev.EventNameEn = new string('e', 201);

        var errors = Validate(ev);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Event_PostavljanjeSvihSvojstava_VrijednostiSeCitajuNazad()
    {
        var start = new DateTime(2026, 8, 14, 20, 0, 0);

        var ev = new Event
        {
            Id = 5, EventName = "SFF", EventNameEn = "Sarajevo Film Festival", EventDescription = "Festival",
            EventDescriptionEn = "Festival EN", StartDate = start, EndDate = start.AddDays(7), Type = EventType.Festival,
            PhotoUrl = "sff.jpg", WebUrl = "https://sff.ba", CoordinatesId = 3, LocationAddress = "Narodno pozorište",
            LocationAddressEn = "National Theatre"
        };

        Assert.AreEqual(5, ev.Id);
        Assert.AreEqual("Sarajevo Film Festival", ev.EventNameEn);
        Assert.AreEqual("Festival EN", ev.EventDescriptionEn);
        Assert.AreEqual(start.AddDays(7), ev.EndDate);
        Assert.AreEqual(EventType.Festival, ev.Type);
        Assert.AreEqual(3, ev.CoordinatesId);
        Assert.AreEqual("National Theatre", ev.LocationAddressEn);
    }

    // ---------- Newsletter ----------

    [TestMethod]
    [TestCategory("Model")]
    public void Newsletter_NoviObjekat_JeNacrtSaTrenutnimVremenom()
    {
        var prije = DateTime.UtcNow;

        var newsletter = new Newsletter();

        Assert.AreEqual(NewsletterStatus.Draft, newsletter.Status);
        Assert.IsTrue(newsletter.SentAt >= prije && newsletter.SentAt <= DateTime.UtcNow);
        Assert.AreEqual(string.Empty, newsletter.Subject);
    }

    /// <summary>
    /// Data-driven test (DataRow): validacija naslova i sadržaja newslettera.
    /// </summary>
    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow(10, "Sadržaj", true, DisplayName = "Ispravan newsletter")]
    [DataRow(200, "Sadržaj", true, DisplayName = "Naslov 200 znakova (gornja granica)")]
    [DataRow(201, "Sadržaj", false, DisplayName = "Naslov 201 znak")]
    [DataRow(0, "Sadržaj", false, DisplayName = "Prazan naslov")]
    [DataRow(10, "", false, DisplayName = "Prazan sadržaj")]
    public void Newsletter_NaslovISadrzaj_ValidacijaDajeOcekivaniRezultat(int duzinaNaslova, string sadrzaj, bool ocekivano)
    {
        var newsletter = new Newsletter { Subject = new string('n', duzinaNaslova), Content = sadrzaj };

        var errors = Validate(newsletter);

        Assert.AreEqual(ocekivano, errors.Count == 0);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Newsletter_DodjelaOperatora_OperatorIStatusSeCuvaju()
    {
        var oper = new Operator { Id = 2 };

        var newsletter = new Newsletter { Id = 1, OperatorId = 2, Operator = oper, Status = NewsletterStatus.Sent };

        Assert.AreSame(oper, newsletter.Operator);
        Assert.AreEqual(2, newsletter.OperatorId);
        Assert.AreEqual(NewsletterStatus.Sent, newsletter.Status);
        Assert.AreEqual(1, newsletter.Id);
    }

    // ---------- ErrorViewModel ----------

    /// <summary>
    /// Data-driven test (DataRow): ID zahtjeva se prikazuje samo kada postoji.
    /// </summary>
    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow("abc-123", true, DisplayName = "ID postoji")]
    [DataRow("", false, DisplayName = "ID je prazan")]
    [DataRow(null, false, DisplayName = "ID je null")]
    public void ErrorViewModel_RequestId_ShowRequestIdOvisiOPostojanjuIda(string? requestId, bool ocekivano)
    {
        var model = new ErrorViewModel { RequestId = requestId };

        Assert.AreEqual(ocekivano, model.ShowRequestId);
    }

    // ---------- Enumeracije ----------

    /// <summary>
    /// Data-driven test (DataRow): brojčane vrijednosti tipova događaja su zapisane u bazi,
    /// pa se ne smiju promijeniti.
    /// </summary>
    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow(EventType.Concert, 0)]
    [DataRow(EventType.Theater, 1)]
    [DataRow(EventType.Movie, 2)]
    [DataRow(EventType.Festival, 3)]
    [DataRow(EventType.Other, 4)]
    public void EventType_SvakiTip_ImaStalnuBrojcanuVrijednost(EventType tip, int ocekivanaVrijednost)
    {
        Assert.AreEqual(ocekivanaVrijednost, (int)tip);
    }

    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow(NewsletterStatus.Draft, 0)]
    [DataRow(NewsletterStatus.Sent, 1)]
    [DataRow(NewsletterStatus.Archived, 2)]
    public void NewsletterStatus_SvakiStatus_ImaStalnuBrojcanuVrijednost(NewsletterStatus status, int ocekivanaVrijednost)
    {
        Assert.AreEqual(ocekivanaVrijednost, (int)status);
    }
}
