using System.Diagnostics.CodeAnalysis;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Abstract;
using GTranslate.Translators;

namespace gosarajevovol3.Services.Scrapers;

/// <summary>
/// Prevod preko Google prevodioca (GTranslate). Kod je prenesen iz NPSService i
/// KupiKartuService, gdje je bio dupliran. Isključeno iz pokrivenosti koda jer zahtijeva mrežu.
/// </summary>
[ExcludeFromCodeCoverage]
public class GoogleEventTranslator : IEventTranslator
{
    private readonly string _sourceName;

    public GoogleEventTranslator(string sourceName)
    {
        _sourceName = sourceName;
    }

    public async Task TranslateInEnAsync(Event ev)
    {
        var translator = new GoogleTranslator();

        try
        {
            if (!string.IsNullOrEmpty(ev.EventName))
            {
                var nameTrans = await translator.TranslateAsync(ev.EventName, "en", "bs");
                ev.EventNameEn = nameTrans.Translation;
                await Task.Delay(800);
            }

            if (!string.IsNullOrEmpty(ev.EventDescription))
            {
                var descTrans = await translator.TranslateAsync(ev.EventDescription, "en", "bs");
                ev.EventDescriptionEn = descTrans.Translation;
                await Task.Delay(800);
            }

            if (!string.IsNullOrEmpty(ev.LocationAddress))
            {
                var locTrans = await translator.TranslateAsync(ev.LocationAddress, "en", "bs");
                ev.LocationAddressEn = locTrans.Translation;
            }
        }
        catch (Exception ex)
        {
            ev.EventNameEn = ev.EventName;
            ev.EventDescriptionEn = ev.EventDescription;
            ev.LocationAddressEn = ev.LocationAddress;
            Console.WriteLine($"[{_sourceName} Scraper] Google limit dostignut: {ex.Message}");
        }
    }
}
