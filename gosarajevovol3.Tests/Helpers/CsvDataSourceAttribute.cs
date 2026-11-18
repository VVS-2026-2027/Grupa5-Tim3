using System.Globalization;
using System.Reflection;

namespace gosarajevovol3.Tests.Helpers;

/// <summary>
/// Data-driven test sa podacima iz CSV fajla (pristup "izvor podataka" - DataSource).
///
/// Klasični [DataSource] atribut i TestContext.DataRow postoje samo za .NET Framework.
/// Ova aplikacija je .NET 8, pa je izvor podataka napravljen preko interfejsa ITestDataSource,
/// istog koji interno koriste [DataRow] i [DynamicData].
///
/// Prvi red CSV fajla je zaglavlje. Svaki sljedeći red je jedno izvršavanje testa.
/// Kolone se redom pretvaraju u tipove parametara testne metode.
/// Polja su odvojena znakom ';' (tačka-zarez), da zarez može ostati dio teksta.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CsvDataSourceAttribute : Attribute, ITestDataSource
{
    private readonly string _fileName;

    public CsvDataSourceAttribute(string fileName)
    {
        _fileName = fileName;
    }

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var lines = File.ReadAllLines(TestHelper.DataFile(_fileName))
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        foreach (var line in lines)
        {
            var fields = line.Split(';');
            yield return parameters
                .Select((p, i) => ConvertValue(fields[i], p.ParameterType))
                .ToArray();
        }
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        return $"{methodInfo.Name} ({string.Join("; ", data ?? Array.Empty<object?>())})";
    }

    /// <summary>
    /// Pretvara tekst iz fajla u traženi tip. Tekst "null" znači null vrijednost.
    /// </summary>
    public static object? ConvertValue(string text, Type type)
    {
        if (text == "null") return null;

        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target == typeof(string)) return text;
        if (target.IsEnum) return Enum.Parse(target, text);
        if (target == typeof(DateTime)) return DateTime.Parse(text, CultureInfo.InvariantCulture);
        return Convert.ChangeType(text, target, CultureInfo.InvariantCulture);
    }
}
