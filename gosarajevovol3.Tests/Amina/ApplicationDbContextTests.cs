using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace gosarajevovol3.Tests.Amina;

[TestClass]
public class ApplicationDbContextTests
{
    private static ApplicationDbContext _context = null!;

    [ClassInitialize]
    public static void ClassSetup(TestContext testContext)
    {
        _context = TestHelper.CreateContext();
        _context.Database.EnsureCreated();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Seed_NakonKreiranjaBaze_PostojeCetiriKorisnika()
    {
        Assert.AreEqual(4, _context.Users.Count());
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Seed_NakonKreiranjaBaze_PostojiJedanAdministrator()
    {
        var admin = _context.Admins.Single();

        Assert.AreEqual("admin@gmail.com", admin.Email);
        Assert.IsTrue(admin.EmailConfirmed);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Seed_NakonKreiranjaBaze_PostojiJedanOperator()
    {
        Assert.AreEqual("operator@gmail.com", _context.Operators.Single().Email);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Seed_NakonKreiranjaBaze_PostojeDvaRegistrovanaKorisnika()
    {
        Assert.AreEqual(2, _context.RegisteredUsers.Count());
    }

 
    [TestMethod]
    [TestCategory("DbContext")]
    [TestCategory("DataDriven")]
    [DataRow("admin@gmail.com", "Admin123!", DisplayName = "Administrator")]
    [DataRow("operator@gmail.com", "Operator123!", DisplayName = "Operator")]
    [DataRow("korisnik@gmail.com", "Korisnik123!", DisplayName = "Registrovani korisnik")]
    public void Seed_PocetniKorisnik_LozinkaJeSacuvanaKaoIspravanHash(string email, string lozinka)
    {
        var user = _context.Users.Single(u => u.Email == email);

        var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash!, lozinka);

        Assert.AreNotEqual(lozinka, user.PasswordHash);
        Assert.AreEqual(PasswordVerificationResult.Success, result);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Seed_PogresnaLozinka_ProvjeraHashaNeUspijeva()
    {
        var admin = _context.Admins.Single();

        var result = new PasswordHasher<User>().VerifyHashedPassword(admin, admin.PasswordHash!, "pogresna");

        Assert.AreEqual(PasswordVerificationResult.Failed, result);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Model_TabelaKorisnika_ZoveSeUsers()
    {
        var entity = _context.Model.FindEntityType(typeof(User))!;

        Assert.AreEqual("Users", entity.GetTableName());
    }

  
    [TestMethod]
    [TestCategory("DbContext")]
    [TestCategory("DataDriven")]
    [DataRow(typeof(RegisteredUser), "RegisteredUser")]
    [DataRow(typeof(Admin), "Admin")]
    [DataRow(typeof(Operator), "Operator")]
    public void Model_VrstaKorisnika_ImaOcekivanuOznakuTipa(Type tip, string ocekivanaOznaka)
    {
        var entity = _context.Model.FindEntityType(tip)!;

        Assert.AreEqual(ocekivanaOznaka, entity.GetDiscriminatorValue());
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Model_BrisanjeKorisnika_KaskadnoBrisePlanove()
    {
        var foreignKey = _context.Model.FindEntityType(typeof(SmartPlanner))!
            .GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(RegisteredUser));

        Assert.AreEqual(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Model_BrisanjePreferenceKojuKoristiPlan_NijeDozvoljeno()
    {
        var foreignKey = _context.Model.FindEntityType(typeof(SmartPlanner))!
            .GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Preference));

        Assert.AreEqual(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Model_BrisanjeKorisnika_KaskadnoBrisePreference()
    {
        var foreignKey = _context.Model.FindEntityType(typeof(Preference))!
            .GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(RegisteredUser));

        Assert.AreEqual(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.IsTrue(foreignKey.IsUnique);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void DbSetovi_NakonKreiranjaKonteksta_SviSuDostupni()
    {
        Assert.IsNotNull(_context.Attractions);
        Assert.IsNotNull(_context.Events);
        Assert.IsNotNull(_context.Hospitality);
        Assert.IsNotNull(_context.SmartPlanners);
        Assert.IsNotNull(_context.Preferences);
        Assert.IsNotNull(_context.Reviews);
        Assert.IsNotNull(_context.Newsletters);
        Assert.IsNotNull(_context.Coordinates);
    }

    [TestMethod]
    [TestCategory("DbContext")]
    public void Coordinates_NoviZapis_CuvaSeICitaIzBaze()
    {
        using var context = TestHelper.CreateContext();
        context.Coordinates.Add(new Coordinates { Latitude = 43.8563, Longitude = 18.4131 });
        context.SaveChanges();

        var saved = context.Coordinates.Single();

        Assert.AreEqual(43.8563, saved.Latitude);
        Assert.AreEqual(18.4131, saved.Longitude);
    }
}
