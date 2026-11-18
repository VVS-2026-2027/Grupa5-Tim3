using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol2.Models.Enums;
using gosarajevovol3.Services.Abstract;

namespace gosarajevovol3.Services.Scrapers;

public class KupiKartuService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KupiKartuScraper _scraper;

    private readonly IEventTranslator _translator;
    private readonly TimeSpan _pauseBetweenRequests;

    // Koliko se čeka do sljedećeg čitanja stranice. Ako nije zadano, čeka se do zadanog doba dana.
    // Unit test ovdje postavlja kratak interval da ne bi čekao satima.
    internal Func<TimeSpan>? NextRunDelay { get; set; }

    public KupiKartuService(IServiceScopeFactory scopeFactory)
        : this(scopeFactory, new KupiKartuScraper(), new GoogleEventTranslator("KupiKartu"), TimeSpan.FromMilliseconds(2500))
    {
    }

    // Konstruktor za unit testove: scraper, prevodilac i pauza se zadaju izvana.
    internal KupiKartuService(IServiceScopeFactory scopeFactory, KupiKartuScraper scraper, IEventTranslator translator, TimeSpan pauseBetweenRequests)
    {
        _scopeFactory = scopeFactory;
        _scraper = scraper;
        _translator = translator;
        _pauseBetweenRequests = pauseBetweenRequests;
    }

    private async Task<bool> CheckIfKupiKartuEverScraped()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            bool hasEver = context.Events.Any(e => e.WebUrl.Contains("kupikartu.ba"));
            return !hasEver;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool IsOk = await CheckIfKupiKartuEverScraped();
        if (IsOk)
        {
            await DoScrapingAsync();
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan timeUntilExecution = NextRunDelay?.Invoke() ?? CalculateTimeUntil1215();
            await Task.Delay(timeUntilExecution, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await DoScrapingAsync();
            }
        }
    }

    private TimeSpan CalculateTimeUntil1215()
    {
        DateTime now = DateTime.Now;
        DateTime executionTime = now.Date.AddHours(0).AddMinutes(15);
        if (now > executionTime)
        {
            executionTime = executionTime.AddDays(1);
        }

        return executionTime.Subtract(now);
    }
    

    private EventType ChooseCategory(string url)
    {
        if (url.EndsWith("/2")) return EventType.Concert; 
        if (url.EndsWith("/3")) return EventType.Theater; 
        if (url.EndsWith("/4")) return EventType.Movie;   
        return EventType.Other; 
    }

    private async Task DoScrapingAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            List<string> kategorijeUrls = await _scraper.GetHamburgerMenuUrls();

            foreach (var katUrl in kategorijeUrls)
            {
                EventType bazniTip = ChooseCategory(katUrl);
                List<string> eventiUrls = await _scraper.GetEventUrls(katUrl);

                foreach (var url in eventiUrls)
                {
                    bool exists = context.Events.Any(e => e.WebUrl == url);
                    if (exists) continue;
                    Event? noviEvent = await _scraper.ScrapeEvents(url, bazniTip);
                    if (noviEvent != null)
                    {
                        await _translator.TranslateInEnAsync(noviEvent);

                        context.Events.Add(noviEvent);
                        await context.SaveChangesAsync();
                    }
                    await Task.Delay(_pauseBetweenRequests);
                }
            }
        }
    }
    
    
}