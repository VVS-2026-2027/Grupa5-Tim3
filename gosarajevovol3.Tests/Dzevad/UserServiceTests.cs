using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Concrete;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace gosarajevovol3.Tests.Dzevad;

/// <summary>
/// Autor testova: Dževad.
/// Testira se UserService - registracija, prijava, odjava i brisanje korisnika.
/// UserManager i SignInManager su zamjenski objekti (Moq), pa se ne koristi prava baza korisnika.
/// </summary>
[TestClass]
public class UserServiceTests
{
    private ApplicationDbContext _context = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<SignInManager<User>> _signInManagerMock = null!;
    private UserService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = TestHelper.CreateContext();
        _context.RegisteredUsers.Add(new RegisteredUser { Id = 10, Email = "korisnik@test.ba", UserName = "korisnik@test.ba" });
        _context.Admins.Add(new Admin { Id = 11, Email = "admin@test.ba", UserName = "admin@test.ba" });
        _context.SaveChanges();

        _userManagerMock = TestHelper.MockUserManager();
        _userManagerMock.Setup(m => m.Users).Returns(_context.Users);
        _signInManagerMock = TestHelper.MockSignInManager(_userManagerMock.Object);
        _service = new UserService(_userManagerMock.Object, _signInManagerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SetCreateResult(IdentityResult result)
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(result);
    }

    // ---------- Registracija ----------

    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("Mock")]
    [Priority(1)]
    public async Task RegisterUserAsync_ValidniPodaci_KreiraRegistrovanogKorisnika()
    {
        SetCreateResult(IdentityResult.Success);

        var result = await _service.RegisterUserAsync("novi@test.ba", "Lozinka1!");

        Assert.IsTrue(result.Succeeded);
        _userManagerMock.Verify(m => m.CreateAsync(
            It.Is<User>(u => u is RegisteredUser && u.Email == "novi@test.ba" && u.UserName == "novi@test.ba"),
            "Lozinka1!"), Times.Once);
    }

    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("Mock")]
    public async Task RegisterOperatorAsync_ValidniPodaci_KreiraOperatora()
    {
        SetCreateResult(IdentityResult.Success);

        await _service.RegisterOperatorAsync("op@test.ba", "Lozinka1!");

        _userManagerMock.Verify(m => m.CreateAsync(It.Is<User>(u => u is Operator && u.Email == "op@test.ba"), "Lozinka1!"), Times.Once);
    }

    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("Mock")]
    public async Task RegisterAdminAsync_ValidniPodaci_KreiraAdministratora()
    {
        SetCreateResult(IdentityResult.Success);

        await _service.RegisterAdminAsync("adm@test.ba", "Lozinka1!");

        _userManagerMock.Verify(m => m.CreateAsync(It.Is<User>(u => u is Admin && u.Email == "adm@test.ba"), "Lozinka1!"), Times.Once);
    }

    [TestMethod]
    [TestCategory("UserService")]
    public async Task RegisterUserAsync_KreiranjeNeUspije_VracaGreskuIzUserManagera()
    {
        SetCreateResult(IdentityResult.Failed(new IdentityError { Description = "Passwords must be at least 6 characters." }));

        var result = await _service.RegisterUserAsync("novi@test.ba", "123");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Passwords must be at least 6 characters.", result.Errors.Single().Description);
    }

    // ---------- Prijava i odjava ----------

    [TestMethod]
    [TestCategory("UserService")]
    public async Task LoginAsync_KorisnikNePostoji_VracaNeuspjehBezPokusajaPrijave()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("nema@test.ba")).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync("nema@test.ba", "Lozinka1!", false);

        Assert.AreSame(SignInResult.Failed, result);
        _signInManagerMock.Verify(
            s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    /// <summary>
    /// Data-driven test (DataRow): servis vraća tačno onaj rezultat koji da SignInManager,
    /// a opcija "zapamti me" se prosljeđuje bez izmjene.
    /// </summary>
    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("DataDriven")]
    [TestCategory("Mock")]
    [DataRow(true, true, DisplayName = "Ispravna lozinka, zapamti me")]
    [DataRow(true, false, DisplayName = "Ispravna lozinka, bez pamćenja")]
    [DataRow(false, false, DisplayName = "Pogrešna lozinka")]
    public async Task LoginAsync_KorisnikPostoji_VracaRezultatSignInManagera(bool lozinkaIspravna, bool zapamti)
    {
        var user = new RegisteredUser { Email = "korisnik@test.ba", UserName = "korisnicko.ime" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("korisnik@test.ba")).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.PasswordSignInAsync("korisnicko.ime", "Lozinka1!", zapamti, false))
            .ReturnsAsync(lozinkaIspravna ? SignInResult.Success : SignInResult.Failed);

        var result = await _service.LoginAsync("korisnik@test.ba", "Lozinka1!", zapamti);

        Assert.AreEqual(lozinkaIspravna, result.Succeeded);
    }

    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("Mock")]
    public async Task LogoutAsync_BiloKada_PozivaOdjavuTacnoJednom()
    {
        await _service.LogoutAsync();

        _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
    }

    // ---------- Čitanje i brisanje korisnika ----------

    [TestMethod]
    [TestCategory("UserService")]
    public async Task GetUserByIdAsync_PostojeciId_VracaKorisnika()
    {
        var user = await _service.GetUserByIdAsync(10);

        Assert.AreEqual("korisnik@test.ba", user!.Email);
    }

    [TestMethod]
    [TestCategory("UserService")]
    public async Task GetUserByIdAsync_NepostojeciId_VracaNull()
    {
        var user = await _service.GetUserByIdAsync(999);

        Assert.IsNull(user);
    }

    [TestMethod]
    [TestCategory("UserService")]
    public async Task GetAllUsersAsync_DvaKorisnikaUBazi_VracaOba()
    {
        var users = await _service.GetAllUsersAsync();

        Assert.AreEqual(2, users.Count);
        CollectionAssert.AllItemsAreInstancesOfType(users, typeof(User));
    }

    [TestMethod]
    [TestCategory("UserService")]
    public async Task DeleteUserAsync_NepostojeciId_VracaGreskuUserNotFound()
    {
        var result = await _service.DeleteUserAsync(999);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("User not found.", result.Errors.Single().Description);
        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("UserService")]
    [TestCategory("Mock")]
    public async Task DeleteUserAsync_PostojeciId_BriseTogKorisnika()
    {
        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        var result = await _service.DeleteUserAsync(10);

        Assert.IsTrue(result.Succeeded);
        _userManagerMock.Verify(m => m.DeleteAsync(It.Is<User>(u => u.Id == 10)), Times.Once);
    }
}
