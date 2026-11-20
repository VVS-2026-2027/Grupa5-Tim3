using System.Net;
using gosarajevovol3.Services;
using gosarajevovol3.Tests.Helpers;

namespace gosarajevovol3.Tests.Amina;

[TestClass]
public class GeocodingServiceTests
{
    private FakeHttpMessageHandler _handler = null!;
    private GeocodingService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new FakeHttpMessageHandler();
        _service = new GeocodingService(new HttpClient(_handler));
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    [Priority(1)]
    public async Task GetCoordinatesAsync_ServisVratiJedanRezultat_VracaKoordinate()
    {
        _handler.AddResponse("nominatim", "[{\"lat\":\"43.8590\",\"lon\":\"18.4330\"}]");

        var result = await _service.GetCoordinatesAsync("Obala Kulina bana 1");

        Assert.IsNotNull(result);
        Assert.AreEqual(43.8590, result.Value.Lat, 0.0001);
        Assert.AreEqual(18.4330, result.Value.Lng, 0.0001);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_ServisVratiPraznuListu_VracaNull()
    {
        _handler.AddResponse("nominatim", "[]");

        var result = await _service.GetCoordinatesAsync("Nepostojeca ulica 999");

        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_ServisVratiGresku500_VracaNull()
    {
        _handler.AddResponse("nominatim", "greska", HttpStatusCode.InternalServerError);

        var result = await _service.GetCoordinatesAsync("Ferhadija 1");

        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_OdgovorNijeJson_VracaNull()
    {
        _handler.AddResponse("nominatim", "<html>Ovo nije JSON</html>");

        var result = await _service.GetCoordinatesAsync("Ferhadija 1");

        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_RezultatBezKoordinata_VracaNulaNula()
    {
      
        _handler.AddResponse("nominatim", "[{\"display_name\":\"Sarajevo\"}]");

        var result = await _service.GetCoordinatesAsync("Sarajevo");

        Assert.AreEqual((0d, 0d), result);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_BiloKojaAdresa_UpituDodajeGradIDrzavu()
    {
        _handler.AddResponse("nominatim", "[]");

        await _service.GetCoordinatesAsync("Titova 5");

        StringAssert.Contains(_handler.RequestedUrls.Single(), "Titova%205%2C%20Sarajevo%2C%20Bosnia%20and%20Herzegovina");
        StringAssert.EndsWith(_handler.RequestedUrls.Single(), "&format=json&limit=1");
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public async Task GetCoordinatesAsync_BiloKojaAdresa_SaljeZaglavljeUserAgent()
    {
        _handler.AddResponse("nominatim", "[]");

        await _service.GetCoordinatesAsync("Titova 5");

        StringAssert.StartsWith(_handler.LastUserAgent, "GoSarajevoApp/1.0");
    }

  
    [TestMethod]
    [TestCategory("Geocoding")]
    [TestCategory("DataDriven")]
    [DataRow("[{\"lat\":\"43.85\",\"lon\":\"18.41\"}]", 43.85, 18.41, DisplayName = "Koordinate kao tekst")]
    [DataRow("[{\"lat\":43.85,\"lon\":18.41}]", 43.85, 18.41, DisplayName = "Koordinate kao broj")]
    [DataRow("[{\"lat\":\"-33.9\",\"lon\":\"151.2\"}]", -33.9, 151.2, DisplayName = "Negativna širina")]
    [DataRow("[{\"lat\":\"44\",\"lon\":\"18\"},{\"lat\":\"1\",\"lon\":\"2\"}]", 44.0, 18.0, DisplayName = "Uzima se prvi rezultat")]
    public async Task GetCoordinatesAsync_RazlicitiZapisiKoordinata_VracaTacneVrijednosti(string json, double lat, double lon)
    {
        _handler.AddResponse("nominatim", json);

        var result = await _service.GetCoordinatesAsync("adresa");

        Assert.AreEqual(lat, result!.Value.Lat, 0.0001);
        Assert.AreEqual(lon, result.Value.Lng, 0.0001);
    }

    [TestMethod]
    [TestCategory("Integracijski")]
    [TestProperty("Zavisnost", "Internet")]
    [Ignore("Zahtijeva internet i stvarni servis nominatim.openstreetmap.org")]
    public async Task GetCoordinatesAsync_StvarniServis_VracaKoordinateVijecnice()
    {
        var service = new GeocodingService();

        var result = await service.GetCoordinatesAsync("Obala Kulina bana 1");

        Assert.AreEqual(43.859, result!.Value.Lat, 0.01);
    }

    [TestMethod]
    [TestCategory("Geocoding")]
    public void Konstruktor_BezParametara_KreiraServis()
    {
        var service = new GeocodingService();

        Assert.IsNotNull(service);
    }
}
