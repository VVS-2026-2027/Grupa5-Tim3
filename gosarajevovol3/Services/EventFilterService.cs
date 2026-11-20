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
        ArgumentNullException.ThrowIfNull(events);

        return events
            .Where(ev => type == null || ev.Type == type)
            .OrderBy(ev => ev.StartDate)
            .ToList();
    }

    /// <summary>
    /// Događaji koji počinju u zadanom periodu (obje granice su uključene).
    /// </summary>
    public List<Event> FilterByDateRange(IEnumerable<Event> events, DateTime from, DateTime to)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }
        if (from > to)
        {
            throw new ArgumentException("Start of the period must not be after its end.");
        }

        return events
            .Where(ev => ev.StartDate >= from && ev.StartDate <= to)
            .OrderBy(ev => ev.StartDate)
            .ToList();
    }
}
