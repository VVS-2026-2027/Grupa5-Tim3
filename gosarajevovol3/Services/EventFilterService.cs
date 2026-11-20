using gosarajevovol2.Models.Enums;
using gosarajevovol3.Models;

namespace gosarajevovol3.Services;

/// <summary>
/// Filtriranje i pretraga događaja: po tipu, po periodu i po tekstu.
/// Rezultat je uvijek poredan od najranijeg događaja.
/// Razvijeno TDD pristupom (Task 3, funkcionalnost 2).
/// </summary>
public class EventFilterService
{
    /// <summary>
    /// Događaji zadanog tipa. Ako tip nije zadan (null), vraćaju se svi događaji.
    /// </summary>
    public List<Event> FilterByType(IEnumerable<Event> events, EventType? type)
    {
        throw new NotImplementedException();
    }
}
