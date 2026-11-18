using System.Xml.Linq;

namespace gosarajevovol3.Tests.Helpers;

/// <summary>
/// Čita testne podatke iz XML fajla za atribut [DynamicData].
/// Svaki element &lt;slucaj&gt; je jedno izvršavanje testa, a njegovi podelementi
/// (redom kako su navedeni u parametru "polja") su argumenti testne metode.
/// </summary>
public static class XmlTestData
{
    public static IEnumerable<object?[]> Read(string fileName, params (string Name, Type Type)[] polja)
    {
        var doc = XDocument.Load(TestHelper.DataFile(fileName));

        return doc.Root!.Elements("slucaj")
            .Select(slucaj => polja
                .Select(p => CsvDataSourceAttribute.ConvertValue(slucaj.Element(p.Name)!.Value, p.Type))
                .ToArray())
            .ToList();
    }
}
