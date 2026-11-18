using System.Net;
using System.Security.Claims;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Abstract;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;

namespace gosarajevovol3.Tests.Ilma;

/// <summary>
/// Autor testova: Ilma.
/// Testira se OperatorController - pregled pretplatnika i slanje newslettera.
/// Zamjenski objekti (Moq): IEmailService (ništa se stvarno ne šalje) i UserManager.
/// </summary>
[TestClass]
public class OperatorControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private OperatorController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.RegisteredUsers.AddRange(
            new RegisteredUser { Id = 1, Email = "prvi@test.ba", UserName = "prvi@test.ba" },
            new RegisteredUser { Id = 2, Email = "drugi@test.ba", UserName = "drugi@test.ba" });
        _context.Operators.Add(new Operator { Id = 3, Email = "operator@test.ba", UserName = "operator@test.ba" });
        _context.SaveChanges();

        _userManagerMock = TestHelper.MockUserManager();
        _emailServiceMock = new Mock<IEmailService>();
        _controller = new OperatorController(_context, _userManagerMock.Object, _emailServiceMock.Object);
        TestHelper.SetUser(_controller, "operator@test.ba");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SetEmailResult(bool success, HttpStatusCode status = HttpStatusCode.Accepted, string body = "")
    {
        _emailServiceMock
            .Setup(e => e.SendToManyAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailSendResult(success, status, body));
    }

    private void RemoveAllSubscribers()
    {
        _context.RegisteredUsers.RemoveRange(_context.RegisteredUsers);
        _context.SaveChanges();
    }

    // ---------- OnActionExecutionAsync (provjera uloge) ----------

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task OnActionExecutionAsync_KorisnikJeOperator_AkcijaSeIzvrsava()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new Operator());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsNull(context.Result);
        next.Verify(n => n(), Times.Once);
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    public async Task OnActionExecutionAsync_KorisnikJeAdministrator_PristupJeZabranjen()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new Admin());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsInstanceOfType(context.Result, typeof(ForbidResult));
        next.Verify(n => n(), Times.Never);
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    public async Task OnActionExecutionAsync_KorisnikJeObicanKorisnik_PristupJeZabranjen()
    {
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new RegisteredUser());
        var context = TestHelper.CreateActionExecutingContext(_controller);
        var next = new Mock<ActionExecutionDelegate>();

        await _controller.OnActionExecutionAsync(context, next.Object);

        Assert.IsInstanceOfType(context.Result, typeof(ForbidResult));
    }

    [TestMethod]
    [TestCategory("Operator")]
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

    // ---------- Index i CreateNewsletter ----------

    [TestMethod]
    [TestCategory("Operator")]
    public async Task Index_DvaRegistrovanaKorisnika_VracaListuIBrojPretplatnika()
    {
        var result = await _controller.Index() as ViewResult;
        var model = (List<RegisteredUser>)result!.Model!;

        Assert.AreEqual(2, model.Count);
        Assert.AreEqual(2, result.ViewData["TotalSubscribers"]);
    }

    [TestMethod]
    [TestCategory("Operator")]
    public async Task Index_NemaRegistrovanihKorisnika_BrojPretplatnikaJeNula()
    {
        RemoveAllSubscribers();

        var result = await _controller.Index() as ViewResult;

        Assert.AreEqual(0, result!.ViewData["TotalSubscribers"]);
    }

    [TestMethod]
    [TestCategory("Operator")]
    public void CreateNewsletter_BiloKada_VracaFormu()
    {
        Assert.IsInstanceOfType(_controller.CreateNewsletter(), typeof(ViewResult));
    }

    // ---------- SendNewsletter ----------

    /// <summary>
    /// Data-driven test sa podacima iz CSV fajla (izvor podataka):
    /// newsletter se prihvata samo ako su popunjeni i naslov i sadržaj.
    /// </summary>
    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("DataDriven")]
    [CsvDataSource("ilma_newsletter.csv")]
    public async Task SendNewsletter_NaslovISadrzajIzCsvFajla_PrihvataSamoPopunjeneVrijednosti(
        string? naslov, string? sadrzaj, bool ocekivanoBezGreske)
    {
        SetEmailResult(true);

        await _controller.SendNewsletter(naslov!, sadrzaj!);

        Assert.AreEqual(ocekivanoBezGreske, _controller.ModelState.IsValid);
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    public async Task SendNewsletter_PrazanNaslov_VracaFormuSaPorukomINeSaljeNista()
    {
        var result = await _controller.SendNewsletter("", "Sadržaj") as ViewResult;

        Assert.AreEqual("CreateNewsletter", result!.ViewName);
        Assert.AreEqual("Subject and Content cannot be empty.", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        _emailServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    public async Task SendNewsletter_NemaRegistrovanihKorisnika_PreusmjeravaSaPorukomINeSaljeNista()
    {
        RemoveAllSubscribers();

        var result = await _controller.SendNewsletter("Naslov", "Sadržaj") as RedirectToActionResult;

        Assert.AreEqual("Index", result!.ActionName);
        Assert.AreEqual("No registered users found.", _controller.TempData["SuccessMessage"]);
        _emailServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task SendNewsletter_DvaPretplatnika_SaljeJednuPorukuNaObjeAdrese()
    {
        SetEmailResult(true);

        await _controller.SendNewsletter("Novosti", "<p>Tekst</p>");

        _emailServiceMock.Verify(e => e.SendToManyAsync(
            It.Is<IEnumerable<string>>(adrese => adrese.Count() == 2
                                                 && adrese.Contains("prvi@test.ba")
                                                 && adrese.Contains("drugi@test.ba")),
            "Novosti", "<p>Tekst</p>", "<p>Tekst</p>"), Times.Once);
    }

    [TestMethod]
    [TestCategory("Operator")]
    [TestCategory("Mock")]
    public async Task SendNewsletter_KorisnikBezEmaila_NijeMedjuPrimaocima()
    {
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 9, Email = null, UserName = "bez-emaila" });
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 10, Email = "", UserName = "prazan-email" });
        _context.SaveChanges();
        SetEmailResult(true);

        await _controller.SendNewsletter("Novosti", "Tekst");

        _emailServiceMock.Verify(e => e.SendToManyAsync(
            It.Is<IEnumerable<string>>(adrese => adrese.Count() == 2),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Operator")]
    public async Task SendNewsletter_SlanjeUspjesno_PreusmjeravaSaPorukomOBrojuPrimalaca()
    {
        SetEmailResult(true);

        var result = await _controller.SendNewsletter("Novosti", "Tekst") as RedirectToActionResult;

        Assert.AreEqual("Index", result!.ActionName);
        Assert.AreEqual("Newsletter successfully sent to all 2 registered users!", _controller.TempData["SuccessMessage"]);
    }

    [TestMethod]
    [TestCategory("Operator")]
    public async Task SendNewsletter_ServisVratiGresku_VracaFormuSaStatusomITekstomGreske()
    {
        SetEmailResult(false, HttpStatusCode.BadRequest, "invalid sender");

        var result = await _controller.SendNewsletter("Novosti", "Tekst") as ViewResult;

        Assert.AreEqual("CreateNewsletter", result!.ViewName);
        Assert.AreEqual("API Error (Status: BadRequest): invalid sender", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        Assert.IsFalse(_controller.TempData.ContainsKey("SuccessMessage"));
    }

    [TestMethod]
    [TestCategory("Operator")]
    public async Task SendNewsletter_ServisBaciIzuzetak_VracaFormuSaPorukomIzuzetka()
    {
        _emailServiceMock
            .Setup(e => e.SendToManyAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new TimeoutException("isteklo vrijeme"));

        var result = await _controller.SendNewsletter("Novosti", "Tekst") as ViewResult;

        Assert.AreEqual("CreateNewsletter", result!.ViewName);
        Assert.AreEqual("An error occurred while connecting: isteklo vrijeme", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
    }
}
