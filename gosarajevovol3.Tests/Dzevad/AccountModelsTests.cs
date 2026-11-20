using System.ComponentModel.DataAnnotations;
using gosarajevovol3.Models;
using gosarajevovol3.Tests.Helpers;
using gosarajevovol3.ViewModels;

namespace gosarajevovol3.Tests.Dzevad;

[TestClass]
public class AccountModelsTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }


    public static IEnumerable<object?[]> RegistracijeIzXmlFajla()
    {
        return XmlTestData.Read("dzevad_registracija.xml",
            ("email", typeof(string)), ("lozinka", typeof(string)), ("potvrda", typeof(string)), ("validno", typeof(bool)));
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(RegistracijeIzXmlFajla), DynamicDataSourceType.Method)]
    public void RegisterViewModel_PodaciIzXmlFajla_ValidacijaDajeOcekivaniRezultat(
        string email, string lozinka, string potvrda, bool ocekivano)
    {
        var model = new RegisterViewModel { Email = email, Password = lozinka, ConfirmPassword = potvrda };

        var errors = Validate(model);

        Assert.AreEqual(ocekivano, errors.Count == 0);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void RegisterViewModel_PrekratkaLozinka_PorukaNavodiGraniceDuzine()
    {
        var model = new RegisterViewModel { Email = "novi@test.ba", Password = "abc", ConfirmPassword = "abc" };

        var errors = Validate(model);

        Assert.AreEqual("The Password must be at least 6 and at max 100 characters long.", errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void RegisterViewModel_RazliciteLozinke_PorukaKazeDaSeNePoklapaju()
    {
        var model = new RegisterViewModel { Email = "novi@test.ba", Password = "Lozinka1!", ConfirmPassword = "Druga1!" };

        var errors = Validate(model);

        Assert.AreEqual("The password and confirmation password do not match.", errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void RegisterViewModel_NoviObjekat_SvaPoljaSuPrazanTekst()
    {
        var model = new RegisterViewModel();

        Assert.AreEqual(string.Empty, model.Email);
        Assert.AreEqual(string.Empty, model.Password);
        Assert.AreEqual(string.Empty, model.ConfirmPassword);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [TestCategory("DataDriven")]
    [DataRow(null, "Lozinka1!", "Email is mandatory.", DisplayName = "Nema e-maila")]
    [DataRow("nije-email", "Lozinka1!", "Format of Email is invalid.", DisplayName = "Neispravan format e-maila")]
    [DataRow("korisnik@test.ba", null, "Password is mandatory.", DisplayName = "Nema lozinke")]
    public void LogInViewModel_NeispravanUnos_VracaOdgovarajucuPoruku(string? email, string? lozinka, string ocekivanaPoruka)
    {
        var model = new LogInViewModel { Email = email!, Password = lozinka! };

        var errors = Validate(model);

        Assert.AreEqual(ocekivanaPoruka, errors.Single().ErrorMessage);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void LogInViewModel_IspravanUnos_NemaGresaka()
    {
        var model = new LogInViewModel { Email = "korisnik@test.ba", Password = "Lozinka1!", RememberMe = true };

        var errors = Validate(model);

        Assert.AreEqual(0, errors.Count);
        Assert.IsTrue(model.RememberMe);
    }


    [TestMethod]
    [TestCategory("ViewModel")]
    [TestCategory("DataDriven")]
    [DataRow("korisnik@test.ba", true, DisplayName = "Ispravan e-mail")]
    [DataRow("", false, DisplayName = "Prazan e-mail")]
    [DataRow("korisnik", false, DisplayName = "E-mail bez domene")]
    public void ForgotPasswordViewModel_Email_ValidacijaDajeOcekivaniRezultat(string email, bool ocekivano)
    {
        var model = new ForgotPasswordViewModel { Email = email };

        var errors = Validate(model);

        Assert.AreEqual(ocekivano, errors.Count == 0);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    [TestCategory("DataDriven")]
    [DataRow("token", "korisnik@test.ba", "Nova123", "Nova123", true, DisplayName = "Svi podaci ispravni")]
    [DataRow("", "korisnik@test.ba", "Nova123", "Nova123", false, DisplayName = "Nema tokena")]
    [DataRow("token", "nije-email", "Nova123", "Nova123", false, DisplayName = "Neispravan e-mail")]
    [DataRow("token", "korisnik@test.ba", "abc", "abc", false, DisplayName = "Prekratka lozinka")]
    [DataRow("token", "korisnik@test.ba", "Nova123", "Nova124", false, DisplayName = "Potvrda se razlikuje")]
    public void ResetPasswordViewModel_Unos_ValidacijaDajeOcekivaniRezultat(
        string token, string email, string lozinka, string potvrda, bool ocekivano)
    {
        var model = new ResetPasswordViewModel { Token = token, Email = email, Password = lozinka, ConfirmPassword = potvrda };

        var errors = Validate(model);

        Assert.AreEqual(ocekivano, errors.Count == 0);
    }

    [TestMethod]
    [TestCategory("ViewModel")]
    public void ResetPasswordViewModel_PrekratkaLozinka_PorukaNavodiNajmanjuDuzinu()
    {
        var model = new ResetPasswordViewModel { Token = "t", Email = "korisnik@test.ba", Password = "abc", ConfirmPassword = "abc" };

        var errors = Validate(model);

        Assert.AreEqual("The password must be at least 6 characters long.", errors.Single().ErrorMessage);
    }


    [TestMethod]
    [TestCategory("ViewModel")]
    public void ProfileViewModel_NoviObjekat_ImaPrazneListeIPrazanEmail()
    {
        var model = new ProfileViewModel();

        Assert.AreEqual(string.Empty, model.Email);
        Assert.AreEqual(0, model.Reviews.Count);
        Assert.AreEqual(0, model.Plans.Count);
    }


    [TestMethod]
    [TestCategory("Model")]
    public void RegisteredUser_NoviObjekat_ImaPrazneListeIBezPreferenci()
    {
        var user = new RegisteredUser();

        Assert.AreEqual(0, user.PlannerList.Count);
        Assert.AreEqual(0, user.ReviewList.Count);
        Assert.IsNull(user.Preference);
    }

    [TestMethod]
    [TestCategory("Model")]
    public void Operator_NoviObjekat_ImaPraznuListuNewslettera()
    {
        var oper = new Operator();

        Assert.AreEqual(0, oper.Newsletters.Count);
    }

    [TestMethod]
    [TestCategory("Model")]
    [TestCategory("DataDriven")]
    [DataRow(typeof(RegisteredUser))]
    [DataRow(typeof(Admin))]
    [DataRow(typeof(Operator))]
    public void VrstaKorisnika_BiloKoja_NasljedjujeKlasuUser(Type tip)
    {
        var user = Activator.CreateInstance(tip);

        Assert.IsInstanceOfType(user, typeof(User));
    }

    [TestMethod]
    [TestCategory("Model")]
    public void RegisteredUser_DodjelaPreference_PreferencaJeDostupna()
    {
        var preference = new Preference();

        var user = new RegisteredUser { Preference = preference };

        Assert.AreSame(preference, user.Preference);
    }
}
