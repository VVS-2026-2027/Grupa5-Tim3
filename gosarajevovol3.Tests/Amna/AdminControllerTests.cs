using gosarajevovol2.Models.Enums;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Models.Enums;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using System.Security.Claims;

namespace gosarajevovol3.Tests.Amna;

[TestClass]
public class AdminControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private AdminController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.Attractions.Add(new Attraction
        {
            Id = 1,
            AttractionName = "Vijećnica",
            AttractionDescription = "Gradska vijećnica",
            AttractionType = AttractionType.Historical,
            LocationAddress = "Obala Kulina bana",
            Coordinates = new Coordinates { Latitude = 43.859, Longitude = 18.433 }
        });
        _context.Hospitality.Add(new Hospitality
        {
            Id = 1,
            HospitalityName = "Inat kuća",
            HospitalityDescription = "Restoran",
            HospitalityType = HospitalityType.Restaurant,
            LocationAddress = "Veliki Alifakovac 1",
            Coordinates = new Coordinates { Latitude = 43.858, Longitude = 18.434 }
        });
        _context.SaveChanges();

        _userManagerMock = TestHelper.MockUserManager();
        _controller = new AdminController(_context, _userManagerMock.Object);
        TestHelper.SetUser(_controller, "admin@test.ba");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private static Attraction NewAttractionModel(int id = 0)
    {
        return new Attraction
        {
            Id = id,
            AttractionName = "Sebilj",
            AttractionDescription = "Česma na Baščaršiji",
            AttractionType = AttractionType.Monument,
            LocationAddress = "Baščaršija 1",
            PhotoUrl = "sebilj.jpg"
        };
    }

    private static Hospitality NewHospitalityModel(int id = 0)
    {
        return new Hospitality
        {
            Id = id,
            HospitalityName = "Dveri",
            HospitalityDescription = "Domaća kuhinja",
            HospitalityType = HospitalityType.Cafe,
            LocationAddress = "Prote Bakovića 12",
            PhotoUrl = "dveri.jpg",
            GooglePlaceId = "place-123"
        };
    }

    private static void AssertRedirectToDashboard(IActionResult result)
    {
        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Dashboard", redirect.ActionName);
    }


    [TestMethod]
    [TestCategory("Admin")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task OnActionExecutionAsync_KorisnikJeAdministrator_AkcijaSeIzvrsava()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new Admin());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsNull(context.Result);
        next.Verify(n => n(), Times.Once);
    }

    [TestMethod]
    [TestCategory("Admin")]
    [TestCategory("Mock")]
    public async Task OnActionExecutionAsync_KorisnikJeObicanKorisnik_PristupJeZabranjen()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new RegisteredUser());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsInstanceOfType(context.Result, typeof(ForbidResult));
        next.Verify(n => n(), Times.Never);
    }

    [TestMethod]
    [TestCategory("Admin")]
    [TestCategory("Mock")]
    public async Task OnActionExecutionAsync_KorisnikJeOperator_PristupJeZabranjen()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new Operator());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsInstanceOfType(context.Result, typeof(ForbidResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    [TestCategory("Mock")]
    public async Task OnActionExecutionAsync_KorisnikNijePronadjen_PristupJeZabranjen()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User?)null);
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsInstanceOfType(context.Result, typeof(ForbidResult));
        next.Verify(n => n(), Times.Never);
    }


    [TestMethod]
    [TestCategory("Admin")]
    public async Task Dashboard_PodaciUBazi_PrikazujeUkupneBrojeveIListe()
    {
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 30, Email = "k@test.ba", UserName = "k@test.ba" });
        _context.Reviews.Add(new Review { Id = 1, RegisteredUserId = 30, HospitalityId = 1, Rating = 5, Comment = "Top" });
        _context.Events.Add(new Event { Id = 1, EventName = "Koncert", EventDescription = "Opis", LocationAddress = "Zetra" });
        _context.SaveChanges();

        var result = await _controller.Dashboard() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ViewData["TotalUsers"]);
        Assert.AreEqual(1, result.ViewData["TotalAttractions"]);
        Assert.AreEqual(1, result.ViewData["TotalEvents"]);
        Assert.AreEqual(1, result.ViewData["TotalReviews"]);
        Assert.AreEqual(1, ((List<Attraction>)result.ViewData["Attractions"]!).Count);
        Assert.AreEqual(1, ((List<Hospitality>)result.ViewData["Hospitality"]!).Count);
        Assert.AreEqual(1, ((List<Event>)result.ViewData["Events"]!).Count);
        Assert.AreEqual("k@test.ba", ((List<Review>)result.ViewData["Reviews"]!)[0].RegisteredUser.Email);
    }


    [TestMethod]
    [TestCategory("Admin")]
    public void CreateAttractionGet_Poziv_VracaPraznuFormu()
    {
        var result = _controller.CreateAttraction() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsNull(result.Model);
    }

    [TestMethod]
    [TestCategory("Admin")]
    [Priority(1)]
    public async Task CreateAttractionPost_IspravanModel_DodajeAtrakcijuSaKoordinatama()
    {
        var result = await _controller.CreateAttraction(NewAttractionModel(), 43.8597, 18.4313);
        var dodana = _context.Attractions.Single(a => a.AttractionName == "Sebilj");

        AssertRedirectToDashboard(result);
        Assert.AreEqual(2, _context.Attractions.Count());
        Assert.AreEqual(43.8597, dodana.Coordinates.Latitude);
        Assert.AreEqual(18.4313, dodana.Coordinates.Longitude);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task CreateAttractionPost_NeispravanModel_VracaFormuINeDodajeAtrakciju()
    {
        var model = NewAttractionModel();
        _controller.ModelState.AddModelError("AttractionName", "Naziv je obavezan.");

        var result = await _controller.CreateAttraction(model, 43.8, 18.4) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(model, result.Model);
        Assert.AreEqual(1, _context.Attractions.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task CreateAttractionPost_GreskaSamoNaKoordinatama_GreskaSeZanemarujeIAtrakcijaSeDodaje()
    {
        _controller.ModelState.AddModelError("Coordinates", "Koordinate nisu poslane kroz formu.");

        var result = await _controller.CreateAttraction(NewAttractionModel(), 43.8, 18.4);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(2, _context.Attractions.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionGet_PostojecaAtrakcija_VracaAtrakcijuSaKoordinatama()
    {
        var result = await _controller.EditAttraction(1) as ViewResult;
        var model = result!.Model as Attraction;

        Assert.IsNotNull(model);
        Assert.AreEqual("Vijećnica", model.AttractionName);
        Assert.AreEqual(43.859, model.Coordinates.Latitude);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionGet_NepostojecaAtrakcija_VracaNotFound()
    {
        var result = await _controller.EditAttraction(999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionPost_IdIzUrlaRazlicitOdIdaModela_VracaNotFound()
    {
        var result = await _controller.EditAttraction(1, NewAttractionModel(2), 43.8, 18.4);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionPost_AtrakcijaNijeUBazi_VracaNotFound()
    {
        var result = await _controller.EditAttraction(999, NewAttractionModel(999), 43.8, 18.4);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionPost_IspravanModel_MijenjaSvaPoljaIKoordinate()
    {
        var result = await _controller.EditAttraction(1, NewAttractionModel(1), 44.0, 18.0);
        var izmijenjena = _context.Attractions.Single();

        AssertRedirectToDashboard(result);
        Assert.AreEqual("Sebilj", izmijenjena.AttractionName);
        Assert.AreEqual("Česma na Baščaršiji", izmijenjena.AttractionDescription);
        Assert.AreEqual("sebilj.jpg", izmijenjena.PhotoUrl);
        Assert.AreEqual(AttractionType.Monument, izmijenjena.AttractionType);
        Assert.AreEqual("Baščaršija 1", izmijenjena.LocationAddress);
        Assert.AreEqual(44.0, izmijenjena.Coordinates.Latitude);
        Assert.AreEqual(18.0, izmijenjena.Coordinates.Longitude);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditAttractionPost_NeispravanModel_VracaFormuIPodaciOstajuIsti()
    {
        var model = NewAttractionModel(1);
        _controller.ModelState.AddModelError("AttractionName", "Naziv je predug.");

        var result = await _controller.EditAttraction(1, model, 44.0, 18.0) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual("Vijećnica", _context.Attractions.Single().AttractionName);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task DeleteAttraction_PostojecaAtrakcija_BriseAtrakciju()
    {
        var result = await _controller.DeleteAttraction(1);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(0, _context.Attractions.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task DeleteAttraction_NepostojecaAtrakcija_NeBriseNistaIVracaNaPlocu()
    {
        var result = await _controller.DeleteAttraction(999);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(1, _context.Attractions.Count());
    }


    [TestMethod]
    [TestCategory("Admin")]
    public void CreateHospitalityGet_Poziv_VracaPraznuFormu()
    {
        var result = _controller.CreateHospitality();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task CreateHospitalityPost_IspravanModel_DodajeObjekatSaKoordinatama()
    {
        var result = await _controller.CreateHospitality(NewHospitalityModel(), 43.86, 18.42);
        var dodan = _context.Hospitality.Single(h => h.HospitalityName == "Dveri");

        AssertRedirectToDashboard(result);
        Assert.AreEqual(43.86, dodan.Coordinates.Latitude);
        Assert.AreEqual(18.42, dodan.Coordinates.Longitude);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task CreateHospitalityPost_NeispravanModel_VracaFormuINeDodajeObjekat()
    {
        var model = NewHospitalityModel();
        _controller.ModelState.AddModelError("HospitalityName", "Naziv je obavezan.");

        var result = await _controller.CreateHospitality(model, 43.86, 18.42) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual(1, _context.Hospitality.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    [DataRow("Coordinates")]
    [DataRow("Reviews")]
    public async Task CreateHospitalityPost_GreskaNaPoljuKojeSeNeUnosi_GreskaSeZanemaruje(string polje)
    {
        _controller.ModelState.AddModelError(polje, "Polje nije poslano kroz formu.");

        var result = await _controller.CreateHospitality(NewHospitalityModel(), 43.86, 18.42);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(2, _context.Hospitality.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityGet_PostojeciObjekat_VracaObjekat()
    {
        var result = await _controller.EditHospitality(1) as ViewResult;

        Assert.AreEqual("Inat kuća", ((Hospitality)result!.Model!).HospitalityName);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityGet_NepostojeciObjekat_VracaNotFound()
    {
        var result = await _controller.EditHospitality(999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityPost_IdIzUrlaRazlicitOdIdaModela_VracaNotFound()
    {
        var result = await _controller.EditHospitality(1, NewHospitalityModel(2), 43.8, 18.4);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityPost_ObjekatNijeUBazi_VracaNotFound()
    {
        var result = await _controller.EditHospitality(999, NewHospitalityModel(999), 43.8, 18.4);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityPost_IspravanModel_MijenjaSvaPoljaIKoordinate()
    {
        var result = await _controller.EditHospitality(1, NewHospitalityModel(1), 44.1, 18.1);
        var izmijenjen = _context.Hospitality.Single();

        AssertRedirectToDashboard(result);
        Assert.AreEqual("Dveri", izmijenjen.HospitalityName);
        Assert.AreEqual("Domaća kuhinja", izmijenjen.HospitalityDescription);
        Assert.AreEqual("dveri.jpg", izmijenjen.PhotoUrl);
        Assert.AreEqual(HospitalityType.Cafe, izmijenjen.HospitalityType);
        Assert.AreEqual("Prote Bakovića 12", izmijenjen.LocationAddress);
        Assert.AreEqual("place-123", izmijenjen.GooglePlaceId);
        Assert.AreEqual(44.1, izmijenjen.Coordinates.Latitude);
        Assert.AreEqual(18.1, izmijenjen.Coordinates.Longitude);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task EditHospitalityPost_NeispravanModel_VracaFormuIPodaciOstajuIsti()
    {
        var model = NewHospitalityModel(1);
        _controller.ModelState.AddModelError("HospitalityName", "Naziv je predug.");

        var result = await _controller.EditHospitality(1, model, 44.1, 18.1) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual("Inat kuća", _context.Hospitality.Single().HospitalityName);
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task DeleteHospitality_PostojeciObjekat_BriseObjekat()
    {
        var result = await _controller.DeleteHospitality(1);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(0, _context.Hospitality.Count());
    }

    [TestMethod]
    [TestCategory("Admin")]
    public async Task DeleteHospitality_NepostojeciObjekat_NeBriseNista()
    {
        var result = await _controller.DeleteHospitality(999);

        AssertRedirectToDashboard(result);
        Assert.AreEqual(1, _context.Hospitality.Count());
    }
}
