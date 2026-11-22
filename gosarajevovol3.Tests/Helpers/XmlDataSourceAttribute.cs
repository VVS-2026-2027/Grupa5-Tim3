using System.Reflection;
using System.Xml.Linq;

namespace gosarajevovol3.Tests.Helpers;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class XmlDataSourceAttribute : Attribute, ITestDataSource
{
    private readonly string _fileName;
    private readonly string[] _elementi;

    public XmlDataSourceAttribute(string fileName, params string[] elementi)
    {
        _fileName = fileName;
        _elementi = elementi;
    }

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var doc = XDocument.Load(TestHelper.DataFile(_fileName));

        foreach (var slucaj in doc.Root!.Elements("slucaj"))
        {
            yield return _elementi
                .Select((naziv, i) => CsvDataSourceAttribute.ConvertValue(slucaj.Element(naziv)!.Value, parameters[i].ParameterType))
                .ToArray();
        }
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        return $"{methodInfo.Name} ({string.Join("; ", data ?? Array.Empty<object?>())})";
    }
}
