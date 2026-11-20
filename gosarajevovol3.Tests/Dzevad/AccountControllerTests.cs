using System.Net;
using System.Text;
using gosarajevovol3.Controllers;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Abstract;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace gosarajevovol3.Tests.Dzevad;

[TestClass]
public class AccountControllerTests
{
    private const string ResetLink = "https://test.ba/Account/ResetPassword?token=abc";

    private ApplicationDbContext _context = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IUrlHelper> _urlHelperMock = null!;
    private AccountController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.Admins.Add(new Admin { Id = 1, Email = "admin@test.ba", UserName = "admin@test.ba" });
        _context.Operators.Add(new Operator { Id = 2, Email = "operator@test.ba", UserName = "operator@test.ba" });
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 3, Email = "korisnik@test.ba", UserName = "korisnik@test.ba" });
        _context.SaveChanges();

        _userServiceMock = new Mock<IUserService>();
        _userManagerMock = TestHelper.MockUserManager();
        _emailServiceMock = new Mock<IEmailService>();
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns(ResetLink);

        _controller = new AccountController(
            _userServiceMock.Object, _userManagerMock.Object, _context, _emailServiceMock.Object);
        TestHelper.SetUser(_controller, null);
        _controller.Url = _urlHelperMock.Object;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SetLoginResult(SignInResult result)
    {
        _userServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(result);
    }

    private void SetEmailResult(bool success, HttpStatusCode status, string body)
    {
        _emailServiceMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailSendResult(success, status, body));
    }

    private void SetExistingUserWithToken(string email, string token)
    {
        var user = new RegisteredUser { Id = 3, Email = email };
        _userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
    }

    private static string Encode(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }


    [TestMethod]
    [TestCategory("Account")]
    public void LoginGet_KorisnikNijePrijavljen_VracaFormuZaPrijavu()
    {
        var result = _controller.Login();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Account")]
    public void LoginGet_KorisnikJeVecPrijavljen_PreusmjeravaNaProfileRoute()
    {
        TestHelper.SetUser(_controller, "korisnik@test.ba");

        var result = _controller.Login() as RedirectToActionResult;

        Assert.AreEqual("ProfileRoute", result!.ActionName);
    }


    [TestMethod]
    [TestCategory("Account")]
    public async Task LoginPost_ModelNijeValidan_VracaFormuINePokusavaPrijavu()
    {
        var model = new LogInViewModel { Email = "", Password = "" };
        _controller.ModelState.AddModelError("Email", "Email is mandatory.");

        var result = await _controller.Login(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        _userServiceMock.Verify(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Account")]
    [Priority(1)]
    public async Task LoginPost_PogresnaLozinka_VracaFormuSaPorukomOGresci()
    {
        SetLoginResult(SignInResult.Failed);
        var model = new LogInViewModel { Email = "korisnik@test.ba", Password = "pogresna" };

        var result = await _controller.Login(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual("Invalid email or password", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("DataDriven")]
    [CsvDataSource("dzevad_preusmjeravanje.csv")]
    public async Task LoginPost_UspjesnaPrijava_PreusmjeravaPremaVrstiKorisnika(
        string email, string ocekivanaAkcija, string ocekivaniKontroler)
    {
        SetLoginResult(SignInResult.Success);

        var result = await _controller.Login(new LogInViewModel { Email = email, Password = "Lozinka1!" }) as RedirectToActionResult;

        Assert.AreEqual(ocekivanaAkcija, result!.ActionName);
        Assert.AreEqual(ocekivaniKontroler, result.ControllerName);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task LoginPost_ValidniPodaci_ProsljedjujeEmailILozinkuServisu()
    {
        SetLoginResult(SignInResult.Success);

        await _controller.Login(new LogInViewModel { Email = "korisnik@test.ba", Password = "Lozinka1!", RememberMe = true });

        _userServiceMock.Verify(s => s.LoginAsync("korisnik@test.ba", "Lozinka1!", false), Times.Once);
    }

    [TestMethod]
    [TestCategory("Account")]
    [ExpectedException(typeof(NullReferenceException))]
    public async Task LoginPost_ModelJeNull_BacaNullReferenceException()
    {
        await _controller.Login(null!);
    }


    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task Logout_PrijavljenKorisnik_OdjavljujeIPreusmjeravaNaPrijavu()
    {
        var result = await _controller.Logout() as RedirectToActionResult;

        _userServiceMock.Verify(s => s.LogoutAsync(), Times.Once);
        Assert.AreEqual("Login", result!.ActionName);
        Assert.AreEqual("Account", result.ControllerName);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task SwitchAccount_PrijavljenKorisnik_OdjavljujeIPreusmjeravaNaPrijavu()
    {
        var result = await _controller.SwitchAccount() as RedirectToActionResult;

        _userServiceMock.Verify(s => s.LogoutAsync(), Times.Once);
        Assert.AreEqual("Login", result!.ActionName);
    }


    [TestMethod]
    [TestCategory("Account")]
    public async Task ProfileRoute_KorisnikNijePrijavljen_PreusmjeravaNaPrijavu()
    {
        var result = await _controller.ProfileRoute() as RedirectToActionResult;

        Assert.AreEqual("Login", result!.ActionName);
        Assert.AreEqual("Account", result.ControllerName);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ProfileRoute_PrijavljenKorisnikBezImena_PreusmjeravaNaPrijavu()
    {
        TestHelper.SetAuthenticatedUserWithoutName(_controller);

        var result = await _controller.ProfileRoute() as RedirectToActionResult;

        Assert.AreEqual("Login", result!.ActionName);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("DataDriven")]
    [CsvDataSource("dzevad_preusmjeravanje.csv")]
    public async Task ProfileRoute_PrijavljenKorisnik_PreusmjeravaPremaVrstiKorisnika(
        string email, string ocekivanaAkcija, string ocekivaniKontroler)
    {
        TestHelper.SetUser(_controller, email);

        var result = await _controller.ProfileRoute() as RedirectToActionResult;

        Assert.AreEqual(ocekivanaAkcija, result!.ActionName);
        Assert.AreEqual(ocekivaniKontroler, result.ControllerName);
    }


    [TestMethod]
    [TestCategory("Account")]
    public void RegisterGet_BiloKada_VracaFormuZaRegistraciju()
    {
        Assert.IsInstanceOfType(_controller.Register(), typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Account")]
    public void ForgotPasswordGet_BiloKada_VracaFormu()
    {
        Assert.IsInstanceOfType(_controller.ForgotPassword(), typeof(ViewResult));
    }

    [TestMethod]
    [TestCategory("Account")]
    public void ResetPasswordConfirmation_BiloKada_VracaPogled()
    {
        Assert.IsInstanceOfType(_controller.ResetPasswordConfirmation(), typeof(ViewResult));
    }


    [TestMethod]
    [TestCategory("Account")]
    public async Task ForgotPasswordPost_ModelNijeValidan_VracaFormuBezSlanjaEmaila()
    {
        var model = new ForgotPasswordViewModel { Email = "nije-email" };
        _controller.ModelState.AddModelError("Email", "Invalid");

        var result = await _controller.ForgotPassword(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        _emailServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ForgotPasswordPost_NalogNePostoji_VracaFormuSaPorukom()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("nema@test.ba")).ReturnsAsync((User?)null);

        var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "nema@test.ba" }) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("No account found with that email address.", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        _emailServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task ForgotPasswordPost_NalogPostoji_SaljeEmailSaLinkomZaReset()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        SetEmailResult(true, HttpStatusCode.Accepted, "");

        await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "korisnik@test.ba" });

        _emailServiceMock.Verify(e => e.SendAsync(
            "korisnik@test.ba",
            "GoSarajevo - Reset Your Password",
            It.Is<string>(html => html.Contains(ResetLink)),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ForgotPasswordPost_EmailPoslan_PostavljaPorukuOUspjehu()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        SetEmailResult(true, HttpStatusCode.Accepted, "");

        var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "korisnik@test.ba" }) as ViewResult;

        Assert.IsNull(result!.Model);
        Assert.AreEqual("A reset link has been sent.", _controller.TempData["SuccessMessage"]);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task ForgotPasswordPost_NalogPostoji_TokenULinkuJeKodiranZaUrl()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN+/=");
        SetEmailResult(true, HttpStatusCode.Accepted, "");
        UrlActionContext? captured = null;
        _urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Callback<UrlActionContext>(c => captured = c)
            .Returns(ResetLink);

        await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "korisnik@test.ba" });

        var values = new Microsoft.AspNetCore.Routing.RouteValueDictionary(captured!.Values);
        Assert.AreEqual("ResetPassword", captured.Action);
        Assert.AreEqual(Encode("TOKEN+/="), values["token"]);
        Assert.AreEqual("korisnik@test.ba", values["email"]);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ForgotPasswordPost_ServisZaEmailVratiGresku_PrikazujeStatusITekstGreske()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        SetEmailResult(false, HttpStatusCode.Unauthorized, "bad api key");
        var model = new ForgotPasswordViewModel { Email = "korisnik@test.ba" };

        var result = await _controller.ForgotPassword(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual("SendGrid error (401): bad api key", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        Assert.IsFalse(_controller.TempData.ContainsKey("SuccessMessage"));
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ForgotPasswordPost_ServisZaEmailBaciIzuzetak_PrikazujePorukuIzuzetka()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        _emailServiceMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("nema mreze"));

        var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "korisnik@test.ba" }) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Failed to send email: nema mreze", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("DataDriven")]
    [DataRow(null, "korisnik@test.ba", DisplayName = "Token je null")]
    [DataRow("", "korisnik@test.ba", DisplayName = "Token je prazan")]
    [DataRow("abc", null, DisplayName = "Email je null")]
    [DataRow("abc", "", DisplayName = "Email je prazan")]
    public void ResetPasswordGet_NedostajeTokenIliEmail_PreusmjeravaNaPrijavu(string? token, string? email)
    {
        var result = _controller.ResetPassword(token!, email!) as RedirectToActionResult;

        Assert.AreEqual("Login", result!.ActionName);
    }

    [TestMethod]
    [TestCategory("Account")]
    public void ResetPasswordGet_TokenIEmailPostoje_VracaFormuSaPopunjenimModelom()
    {
        var result = _controller.ResetPassword("abc", "korisnik@test.ba") as ViewResult;
        var model = (ResetPasswordViewModel)result!.Model!;

        Assert.AreEqual("abc", model.Token);
        Assert.AreEqual("korisnik@test.ba", model.Email);
    }


    private static ResetPasswordViewModel ResetModel(string token)
    {
        return new ResetPasswordViewModel
        {
            Email = "korisnik@test.ba", Token = token, Password = "NovaLozinka1!", ConfirmPassword = "NovaLozinka1!"
        };
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ResetPasswordPost_ModelNijeValidan_VracaFormu()
    {
        var model = ResetModel(Encode("TOKEN"));
        _controller.ModelState.AddModelError("Password", "Prekratka");

        var result = await _controller.ResetPassword(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ResetPasswordPost_NalogNePostoji_IpakPreusmjeravaNaPotvrdu()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _controller.ResetPassword(ResetModel(Encode("TOKEN"))) as RedirectToActionResult;

        Assert.AreEqual("ResetPasswordConfirmation", result!.ActionName);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ResetPasswordPost_TokenNijeIspravnoKodiran_VracaFormuSaPorukom()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");

        var result = await _controller.ResetPassword(ResetModel("a")) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Invalid or corrupted reset link.", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task ResetPasswordPost_IspravanToken_MijenjaLozinkuIPreusmjeravaNaPotvrdu()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        _userManagerMock
            .Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), "TOKEN", "NovaLozinka1!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ResetPassword(ResetModel(Encode("TOKEN"))) as RedirectToActionResult;

        Assert.AreEqual("ResetPasswordConfirmation", result!.ActionName);
        _userManagerMock.Verify(m => m.ResetPasswordAsync(It.IsAny<User>(), "TOKEN", "NovaLozinka1!"), Times.Once);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task ResetPasswordPost_PromjenaLozinkeNeUspije_PrikazujeSveGreske()
    {
        SetExistingUserWithToken("korisnik@test.ba", "TOKEN");
        _userManagerMock
            .Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Invalid token." },
                new IdentityError { Description = "Password too weak." }));

        var result = await _controller.ResetPassword(ResetModel(Encode("TOKEN"))) as ViewResult;

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(
            new[] { "Invalid token.", "Password too weak." },
            _controller.ModelState[""]!.Errors.Select(e => e.ErrorMessage).ToList());
    }


    private static RegisterViewModel RegisterModel()
    {
        return new RegisterViewModel { Email = "novi@test.ba", Password = "Lozinka1!", ConfirmPassword = "Lozinka1!" };
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task RegisterPost_ModelNijeValidan_VracaFormuINeKreiraKorisnika()
    {
        var model = RegisterModel();
        _controller.ModelState.AddModelError("Email", "Invalid");

        var result = await _controller.Register(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task RegisterPost_ValidniPodaci_KreiraRegistrovanogKorisnikaSaPotvrdjenimEmailom()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), "Lozinka1!")).ReturnsAsync(IdentityResult.Success);

        await _controller.Register(RegisterModel());

        _userManagerMock.Verify(m => m.CreateAsync(
            It.Is<User>(u => u is RegisteredUser
                             && u.Email == "novi@test.ba"
                             && u.UserName == "novi@test.ba"
                             && u.NormalizedEmail == "NOVI@TEST.BA"
                             && u.EmailConfirmed),
            "Lozinka1!"), Times.Once);
    }

    [TestMethod]
    [TestCategory("Account")]
    [TestCategory("Mock")]
    public async Task RegisterPost_KorisnikKreiran_PrijavljujeGaIPreusmjeravaNaProfileRoute()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(RegisterModel()) as RedirectToActionResult;

        Assert.AreEqual("ProfileRoute", result!.ActionName);
        _userServiceMock.Verify(s => s.LoginAsync("novi@test.ba", "Lozinka1!", false), Times.Once);
    }

    [TestMethod]
    [TestCategory("Account")]
    public async Task RegisterPost_EmailVecPostoji_VracaFormuSaGreskomINePrijavljuje()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email is already taken." }));
        var model = RegisterModel();

        var result = await _controller.Register(model) as ViewResult;

        Assert.AreSame(model, result!.Model);
        Assert.AreEqual("Email is already taken.", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        _userServiceMock.Verify(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Account")]
    [ExpectedException(typeof(NullReferenceException))]
    public async Task RegisterPost_ModelJeNull_BacaNullReferenceException()
    {
        await _controller.Register(null!);
    }
}
