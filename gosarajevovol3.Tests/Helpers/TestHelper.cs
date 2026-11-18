using System.Security.Claims;
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace gosarajevovol3.Tests.Helpers;

/// <summary>
/// Zajedničke pomoćne metode za sve testne klase.
/// Ovdje je sve što bi se inače ponavljalo u svakom testu (priprema baze, korisnika, mock objekata).
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Pravi novu, praznu bazu u memoriji. Svaki poziv daje bazu sa drugim imenom,
    /// pa testovi ne dijele podatke i međusobno su nezavisni.
    /// </summary>
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Kontroleru postavlja "prijavljenog" korisnika sa datim e-mailom.
    /// Ako je email null, korisnik nije prijavljen.
    /// </summary>
    public static void SetUser(Controller controller, string? email)
    {
        var identity = email == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }, "TestAuth");

        SetIdentity(controller, identity);
    }

    /// <summary>
    /// Korisnik koji je prijavljen, ali nema ime (e-mail) u identitetu.
    /// </summary>
    public static void SetAuthenticatedUserWithoutName(Controller controller)
    {
        SetIdentity(controller, new ClaimsIdentity("TestAuth"));
    }

    private static void SetIdentity(Controller controller, ClaimsIdentity identity)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    /// <summary>
    /// Mock za UserManager. UserManager nema interfejs, pa se mock pravi nad klasom,
    /// a konstruktoru se predaje lažno skladište korisnika.
    /// </summary>
    public static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    /// <summary>
    /// Mock za SignInManager (prijava i odjava korisnika).
    /// </summary>
    public static Mock<SignInManager<User>> MockSignInManager(UserManager<User> userManager)
    {
        return new Mock<SignInManager<User>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);
    }

    /// <summary>
    /// Pravi kontekst koji ASP.NET predaje metodi OnActionExecutionAsync prije svake akcije.
    /// </summary>
    public static ActionExecutingContext CreateActionExecutingContext(Controller controller)
    {
        var actionContext = new ActionContext(
            controller.ControllerContext.HttpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);
    }

    /// <summary>
    /// Mock za IServiceScopeFactory. Pozadinski servisi preko njega traže bazu podataka;
    /// ovdje uvijek dobiju proslijeđeni kontekst (bazu u memoriji).
    /// </summary>
    public static Mock<IServiceScopeFactory> MockScopeFactory(ApplicationDbContext context)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(ApplicationDbContext))).Returns(context);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        return factory;
    }

    /// <summary>
    /// Puna putanja do fajla sa testnim podacima u folderu Podaci.
    /// </summary>
    public static string DataFile(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Podaci", fileName);
    }
}
