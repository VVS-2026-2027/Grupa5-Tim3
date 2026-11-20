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
    private const string InvalidPeriodMessage = "Start of the period must not be after its end.";

    /// <summary>
    /// Događaji zadanog tipa. Ako tip nije zadan (null), vraćaju se svi događaji.
    /// </summary>
    public List<Event> FilterByType(IEnumerable<Event> events, EventType? type)
    {
        ArgumentNullException.ThrowIfNull(events);

        return ToOrderedList(events.Where(ev => type == null || ev.Type == type));
    }

    /// <summary>
    /// Događaji koji počinju u zadanom periodu (obje granice su uključene).
    /// </summary>
    public List<Event> FilterByDateRange(IEnumerable<Event> events, DateTime from, DateTime to)
    {
        ArgumentNullException.ThrowIfNull(events);
        EnsureValidPeriod(from, to);

        return ToOrderedList(events.Where(ev => ev.StartDate >= from && ev.StartDate <= to));
    }

    /// <summary>
    /// Pretraga po nazivu događaja (bosanski ili engleski naziv), bez razlike velikih i malih slova.
    /// Prazan tekst pretrage vraća sve događaje.
    /// </summary>
    public List<Event> Search(IEnumerable<Event> events, string? text)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (string.IsNullOrWhiteSpace(text))
        {
            return ToOrderedList(events);
        }

        string wanted = text.Trim();
        return ToOrderedList(events.Where(ev => NameContains(ev.EventName, wanted) || NameContains(ev.EventNameEn, wanted)));
    }

    private static void EnsureValidPeriod(DateTime from, DateTime to)
    {
        if (from > to)
        {
            throw new ArgumentException(InvalidPeriodMessage);
        }
    }

    private static List<Event> ToOrderedList(IEnumerable<Event> events)
    {
        return events.OrderBy(ev => ev.StartDate).ToList();
    }

    private static bool NameContains(string? name, string wanted)
    {
        return name != null && name.Contains(wanted, StringComparison.OrdinalIgnoreCase);
    }
}
