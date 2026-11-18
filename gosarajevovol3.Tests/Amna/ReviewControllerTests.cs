using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace gosarajevovol3.Tests.Amna;

/// <summary>
/// Autor testova: Amna.
/// Testira se ReviewController - ostavljanje, izmjena i brisanje recenzija.
/// </summary>
[TestClass]
public class ReviewControllerTests
{
    private const string UserEmail = "amna@test.ba";
    private const int UserId = 20;
    private const int OtherUserId = 21;
    private const int HospitalityId = 1;

    private ApplicationDbContext _context = null!;
    private ReviewController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.RegisteredUsers.Add(new RegisteredUser { Id = UserId, Email = UserEmail, UserName = UserEmail });
        _context.RegisteredUsers.Add(new RegisteredUser { Id = OtherUserId, Email = "drugi@test.ba", UserName = "drugi@test.ba" });
        _context.Hospitality.Add(new Hospitality
        {
            Id = HospitalityId,
            HospitalityName = "Inat kuća",
            HospitalityDescription = "Opis",
            HospitalityType = HospitalityType.Restaurant,
            LocationAddress = "Adresa"
        });
        _context.SaveChanges();

        _controller = new ReviewController(_context);
        TestHelper.SetUser(_controller, UserEmail);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void AddReview(int id, int userId, int rating = 4, string comment = "Dobro")
    {
        _context.Reviews.Add(new Review
        {
            Id = id, RegisteredUserId = userId, HospitalityId = HospitalityId, Rating = rating, Comment = comment
        });
        _context.SaveChanges();
    }

    private static void AssertRedirectToLogin(IActionResult result)
    {
        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Login", redirect.ActionName);
        Assert.AreEqual("Account", redirect.ControllerName);
    }

    // ---------- Leave (GET) ----------

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_NeprijavljenKorisnik_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, null);

        var result = await _controller.Leave(HospitalityId);

        AssertRedirectToLogin(result);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_KorisnikNijeRegistrovan_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, "admin@test.ba");

        var result = await _controller.Leave(HospitalityId);

        AssertRedirectToLogin(result);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_ObjekatNePostoji_VracaNotFound()
    {
        var result = await _controller.Leave(999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_KorisnikNemaRecenziju_VracaPraznuFormu()
    {
        var result = await _controller.Leave(HospitalityId) as ViewResult;
        var model = result!.Model as ReviewFormViewModel;

        Assert.IsNotNull(model);
        Assert.IsNull(model.ReviewId);
        Assert.AreEqual(HospitalityId, model.HospitalityId);
        Assert.AreEqual("Inat kuća", model.HospitalityName);
        Assert.AreEqual(0, model.Rating);
        Assert.AreEqual(string.Empty, model.Comment);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_KorisnikVecImaRecenziju_FormaJePopunjenaPostojecomRecenzijom()
    {
        AddReview(5, UserId, 3, "Solidno");

        var result = await _controller.Leave(HospitalityId) as ViewResult;
        var model = (ReviewFormViewModel)result!.Model!;

        Assert.AreEqual(5, model.ReviewId);
        Assert.AreEqual(3, model.Rating);
        Assert.AreEqual("Solidno", model.Comment);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeaveGet_RecenzijaPripadaDrugomKorisniku_FormaOstajePrazna()
    {
        AddReview(5, OtherUserId, 1, "Loše");

        var result = await _controller.Leave(HospitalityId) as ViewResult;
        var model = (ReviewFormViewModel)result!.Model!;

        Assert.IsNull(model.ReviewId);
        Assert.AreEqual(0, model.Rating);
    }

    // ---------- Leave (POST) ----------

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeavePost_NeprijavljenKorisnik_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, null);

        var result = await _controller.Leave(new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = 5, Comment = "Top" });

        AssertRedirectToLogin(result);
        Assert.AreEqual(0, _context.Reviews.Count());
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeavePost_KorisnikNijeRegistrovan_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, "operator@test.ba");

        var result = await _controller.Leave(new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = 5, Comment = "Top" });

        AssertRedirectToLogin(result);
    }

    [TestMethod]
    [TestCategory("Review")]
    [DataRow(0, DisplayName = "Ocjena nije odabrana")]
    [DataRow(-1, DisplayName = "Negativna ocjena")]
    [DataRow(6, DisplayName = "Ocjena iznad najveće dozvoljene")]
    public async Task LeavePost_OcjenaVanOpsega_VracaFormuSaGreskomINeSpremaRecenziju(int ocjena)
    {
        var model = new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = ocjena, Comment = "Komentar" };

        var result = await _controller.Leave(model) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(model, result.Model);
        Assert.IsTrue(_controller.ModelState.ContainsKey("Rating"));
        Assert.AreEqual("Inat kuća", model.HospitalityName);
        Assert.AreEqual(0, _context.Reviews.Count());
    }

    [TestMethod]
    [TestCategory("Review")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task LeavePost_PrazanKomentar_VracaFormuSaGreskom(string? komentar)
    {
        var model = new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = 4, Comment = komentar! };

        var result = await _controller.Leave(model) as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsTrue(_controller.ModelState.ContainsKey("Comment"));
        Assert.AreEqual("Please write a comment.", _controller.ModelState["Comment"]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeavePost_NeispravanUnosZaNepostojeciObjekat_NazivObjektaJePrazan()
    {
        var model = new ReviewFormViewModel { HospitalityId = 999, Rating = 0, Comment = "Komentar" };

        await _controller.Leave(model);

        Assert.AreEqual(string.Empty, model.HospitalityName);
    }

    [TestMethod]
    [TestCategory("Review")]
    [Priority(1)]
    public async Task LeavePost_NovaIspravnaRecenzija_SpremaRecenzijuIPreusmjeravaNaDetaljeObjekta()
    {
        var model = new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = 5, Comment = "Odlično" };

        var result = await _controller.Leave(model) as RedirectToActionResult;
        var spremljena = _context.Reviews.Single();

        Assert.AreEqual("HospitalityDetails", result!.ActionName);
        Assert.AreEqual("ThingsToDo", result.ControllerName);
        Assert.AreEqual(HospitalityId, result.RouteValues!["id"]);
        Assert.AreEqual(5, spremljena.Rating);
        Assert.AreEqual("Odlično", spremljena.Comment);
        Assert.AreEqual(UserId, spremljena.RegisteredUserId);
        Assert.AreEqual(HospitalityId, spremljena.HospitalityId);
    }

    [TestMethod]
    [TestCategory("Review")]
    [DataRow(1)]
    [DataRow(5)]
    public async Task LeavePost_GranicneIspravneOcjene_RecenzijaJeSpremljena(int ocjena)
    {
        var model = new ReviewFormViewModel { HospitalityId = HospitalityId, Rating = ocjena, Comment = "Komentar" };

        await _controller.Leave(model);

        Assert.AreEqual(ocjena, _context.Reviews.Single().Rating);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeavePost_IzmjenaVlastiteRecenzije_MijenjaOcjenuIKomentarBezDodavanjaNove()
    {
        AddReview(5, UserId, 2, "Staro");
        var model = new ReviewFormViewModel { ReviewId = 5, HospitalityId = HospitalityId, Rating = 4, Comment = "Novo" };

        var result = await _controller.Leave(model);
        var recenzija = _context.Reviews.Single();

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(4, recenzija.Rating);
        Assert.AreEqual("Novo", recenzija.Comment);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task LeavePost_IzmjenaTudjeRecenzije_VracaNotFoundIRecenzijaOstajeIsta()
    {
        AddReview(5, OtherUserId, 2, "Tuđe");
        var model = new ReviewFormViewModel { ReviewId = 5, HospitalityId = HospitalityId, Rating = 5, Comment = "Izmjena" };

        var result = await _controller.Leave(model);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        Assert.AreEqual("Tuđe", _context.Reviews.Single().Comment);
    }

    [TestMethod]
    [TestCategory("Review")]
    [TestCategory("Izuzeci")]
    [ExpectedException(typeof(NullReferenceException))]
    public async Task LeavePost_ModelJeNull_BacaNullReferenceException()
    {
        await _controller.Leave((ReviewFormViewModel)null!);
    }

    // ---------- Delete ----------

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_NeprijavljenKorisnik_PreusmjeravaNaPrijavu()
    {
        AddReview(5, UserId);
        TestHelper.SetUser(_controller, null);

        var result = await _controller.Delete(5);

        AssertRedirectToLogin(result);
        Assert.AreEqual(1, _context.Reviews.Count());
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_KorisnikNijeRegistrovan_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, "admin@test.ba");

        var result = await _controller.Delete(5);

        AssertRedirectToLogin(result);
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_RecenzijaNePostoji_VracaNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_TudjaRecenzija_VracaNotFoundINeBriseJe()
    {
        AddReview(5, OtherUserId);

        var result = await _controller.Delete(5);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        Assert.AreEqual(1, _context.Reviews.Count());
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_SaStraniceObjekta_BriseRecenzijuIVracaNaDetaljeObjekta()
    {
        AddReview(5, UserId);

        var result = await _controller.Delete(5, HospitalityId) as RedirectToActionResult;

        Assert.AreEqual("HospitalityDetails", result!.ActionName);
        Assert.AreEqual("ThingsToDo", result.ControllerName);
        Assert.AreEqual(HospitalityId, result.RouteValues!["id"]);
        Assert.AreEqual(0, _context.Reviews.Count());
    }

    [TestMethod]
    [TestCategory("Review")]
    public async Task Delete_SaProfila_BriseRecenzijuIVracaNaProfil()
    {
        AddReview(5, UserId);

        var result = await _controller.Delete(5) as RedirectToActionResult;

        Assert.AreEqual("Profile", result!.ActionName);
        Assert.AreEqual("UserProfile", result.ControllerName);
        Assert.AreEqual(0, _context.Reviews.Count());
    }
}
