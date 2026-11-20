using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace gosarajevovol3.Tests.Dzevad;

[TestClass]
public class UserProfileControllerTests
{
    private ApplicationDbContext _context = null!;
    private UserProfileController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 1, Email = "korisnik@test.ba", UserName = "korisnik@test.ba" });
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 2, Email = "drugi@test.ba", UserName = "drugi@test.ba" });
        _context.Admins.Add(new Admin { Id = 3, Email = "admin@test.ba", UserName = "admin@test.ba" });
        _context.Hospitality.Add(new Hospitality
        {
            Id = 1, HospitalityName = "Inat kuća", HospitalityDescription = "Restoran",
            HospitalityType = HospitalityType.Restaurant, LocationAddress = "Veliki Alifakovac 1"
        });
        _context.Reviews.AddRange(
            new Review { Id = 1, Comment = "Starija", Rating = 4, RegisteredUserId = 1, HospitalityId = 1, CreatedAt = new DateTime(2026, 1, 10) },
            new Review { Id = 2, Comment = "Novija", Rating = 5, RegisteredUserId = 1, HospitalityId = 1, CreatedAt = new DateTime(2026, 3, 5) },
            new Review { Id = 3, Comment = "Tuđa", Rating = 1, RegisteredUserId = 2, HospitalityId = 1, CreatedAt = new DateTime(2026, 2, 1) });
        _context.SmartPlanners.AddRange(
            new SmartPlanner { Id = 7, RegisteredUserId = 1, PreferenceId = 1 },
            new SmartPlanner { Id = 5, RegisteredUserId = 1, PreferenceId = 1 },
            new SmartPlanner { Id = 6, RegisteredUserId = 2, PreferenceId = 2 });
        _context.SaveChanges();

        _controller = new UserProfileController(_context);
        TestHelper.SetUser(_controller, "korisnik@test.ba");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private async Task<ProfileViewModel> GetProfileAsync()
    {
        var result = await _controller.Profile() as ViewResult;
        return (ProfileViewModel)result!.Model!;
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    public async Task Profile_KorisnikNijePrijavljen_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetUser(_controller, null);

        var result = await _controller.Profile() as RedirectToActionResult;

        Assert.AreEqual("Login", result!.ActionName);
        Assert.AreEqual("Account", result.ControllerName);
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    [TestCategory("DataDriven")]
    [DataRow("admin@test.ba", DisplayName = "Administrator nema korisnički profil")]
    [DataRow("nepoznat@test.ba", DisplayName = "E-mail koji ne postoji u bazi")]
    public async Task Profile_PrijavljeniNijeRegistrovaniKorisnik_PreusmjeravaNaPrijavu(string email)
    {
        TestHelper.SetUser(_controller, email);

        var result = await _controller.Profile() as RedirectToActionResult;

        Assert.AreEqual("Login", result!.ActionName);
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    [Priority(1)]
    public async Task Profile_RegistrovaniKorisnik_VracaNjegovEmail()
    {
        var model = await GetProfileAsync();

        Assert.AreEqual("korisnik@test.ba", model.Email);
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    public async Task Profile_RegistrovaniKorisnik_VracaSamoNjegoveRecenzijeOdNajnovije()
    {
        var model = await GetProfileAsync();

        CollectionAssert.AreEqual(new[] { "Novija", "Starija" }, model.Reviews.Select(r => r.Comment).ToList());
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    public async Task Profile_RegistrovaniKorisnik_RecenzijeSadrzeUcitanObjekat()
    {
        var model = await GetProfileAsync();

        Assert.AreEqual("Inat kuća", model.Reviews[0].Hospitality!.HospitalityName);
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    public async Task Profile_RegistrovaniKorisnik_VracaSamoNjegovePlanovePoRedoslijeduId()
    {
        var model = await GetProfileAsync();

        CollectionAssert.AreEqual(new[] { 5, 7 }, model.Plans.Select(p => p.Id).ToList());
    }

    [TestMethod]
    [TestCategory("UserProfile")]
    public async Task Profile_KorisnikBezRecenzijaIPlanova_VracaPrazneListe()
    {
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 9, Email = "novi@test.ba", UserName = "novi@test.ba" });
        _context.SaveChanges();
        TestHelper.SetUser(_controller, "novi@test.ba");

        var model = await GetProfileAsync();

        Assert.AreEqual(0, model.Reviews.Count);
        Assert.AreEqual(0, model.Plans.Count);
    }
}
