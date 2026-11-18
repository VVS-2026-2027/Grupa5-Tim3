
using gosarajevovol3.Data;
using gosarajevovol3.Models;
using gosarajevovol3.Services.Abstract;

namespace gosarajevovol3.Services.Scrapers;

public class NPSService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NPSScraper _scraper;
    private readonly IEventTranslator _translator;
    private readonly TimeSpan _pauseBetweenRequests;

    // Koliko se čeka do sljedećeg čitanja stranice. Ako nije zadano, čeka se do zadanog doba dana.
    // Unit test ovdje postavlja kratak interval da ne bi čekao satima.
    internal Func<TimeSpan>? NextRunDelay { get; set; }

    public NPSService(IServiceScopeFactory scopeFactory)
        : this(scopeFactory, new NPSScraper(), new GoogleEventTranslator("NPS"), TimeSpan.FromMilliseconds(2500))
    {
    }

    // Konstruktor za unit testove: scraper, prevodilac i pauza se zadaju izvana.
    internal NPSService(IServiceScopeFactory scopeFactory, NPSScraper scraper, IEventTranslator translator, TimeSpan pauseBetweenRequests)
    {
        _scopeFactory = scopeFactory;
        _scraper = scraper;
        _translator = translator;
        _pauseBetweenRequests = pauseBetweenRequests;
    }

    private async Task<bool> CheckIfNpsEverScraped()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context =  scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            bool hasEver = context.Events.Any(e => e.WebUrl.Contains("nps.ba"));
            return !hasEver;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool IsOk = await CheckIfNpsEverScraped();
        if (IsOk)
        {
            await DoScrapingAsync();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan timeUntilMidnight = NextRunDelay?.Invoke() ?? CalculateTimeUntilMidnight();
            await Task.Delay(timeUntilMidnight, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await DoScrapingAsync();
            }
        }
    }
    private TimeSpan CalculateTimeUntilMidnight()
    {
        DateTime now = DateTime.Now;
        DateTime midnight = now.Date.AddDays(1);
        return midnight.Subtract(now);
    }
    
    private async Task DoScrapingAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            List<string> urls = await _scraper.GetPlayUrls();

            foreach (var url in urls)
            {
                bool exists = context.Events.Any(e => e.WebUrl == url);
                if (exists) continue;

                Event? noviEvent = await _scraper.ScrapePlayDetails(url);
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