using Orleans.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Configure Orleans for in-process clustering and in-memory grain storage.
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
});

using var app = builder.Build();

// Basic health endpoint.
app.MapGet("/", () => "Hello World!");

// Create a shortened URL from a full URL.
app.MapGet("/shorten",
    static async (IGrainFactory grains, HttpRequest request, string url) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        // Validate the URL query string.
        if (string.IsNullOrWhiteSpace(url) &&
            Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
        {
            return Results.BadRequest($"""
                The URL query string is required and needs to be well formed.
                Consider, ${host}/shorten?url=https://www.microsoft.com.
                """);
        }

        // Create a unique, short ID for the requested URL.
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Create and persist a grain with the shortened ID and full URL.
        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        await shortenerGrain.SetUrl(url);

        // Return the shortened URL for later use.
        var resultBuilder = new UriBuilder(host)
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(resultBuilder.Uri);
    });

// Handle redirects for shortened URLs.
app.MapGet("/go/{shortenedRouteSegment:required}",
    static async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID to the original URL.
        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        var url = await shortenerGrain.GetUrl();

        // Handles missing schemes, defaults to "http://".
        var redirectBuilder = new UriBuilder(url);

        return Results.Redirect(redirectBuilder.Uri.ToString());
    });

app.Run();

// Grain contract for storing and retrieving URLs by short code.
public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);

    Task<string> GetUrl();
}

// Grain implementation backed by Orleans persistent state.
public sealed class UrlShortenerGrain(
    [PersistentState(
        stateName: "url",
        storageName: "urls")]
        IPersistentState<UrlDetails> state)
    : Grain, IUrlShortenerGrain
{
    private KeyValuePair<string, string> _cache;

    // Save the full URL for this grain key.
    public async Task SetUrl(string fullUrl)
    {
        state.State = new()
        {
            ShortenedRouteSegment = this.GetPrimaryKeyString(),
            FullUrl = fullUrl
        };

        await state.WriteStateAsync();
    }

    // Return the stored URL for this grain key.
    public Task<string> GetUrl() =>
        Task.FromResult(_cache.Value);
}

[GenerateSerializer]
public sealed record class UrlDetails
{
    // The full original URL.
    [Id(0)]
    public string FullUrl { get; set; } = "";

    // The shortened route segment used as the grain key.
    [Id(1)]
    public string ShortenedRouteSegment { get; set; } = "";
}

