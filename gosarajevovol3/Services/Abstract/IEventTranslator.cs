using gosarajevovol3.Models;

namespace gosarajevovol3.Services.Abstract;

/// <summary>
/// Prevodi naziv, opis i adresu događaja na engleski jezik.
/// Izdvojeno u interfejs da se u unit testovima ne poziva stvarni Google prevodilac.
/// </summary>
public interface IEventTranslator
{
    Task TranslateInEnAsync(Event ev);
}
