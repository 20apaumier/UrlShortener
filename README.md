# UrlShortener

Minimal URL shortener built with ASP.NET Core and Orleans

## Prerequisites
- .NET SDK 8+

## Run locally
```powershell
dotnet restore
dotnet run
```

The app will print the local URL it is listening on (for example, `http://localhost:5000`).

## Endpoints
- `GET /` returns a simple health response.
- `GET /shorten?url={FULL_URL}` creates a shortened URL.
- `GET /go/{SHORT_CODE}` redirects to the original URL.

### Example
```powershell
# Create a short URL
curl "http://localhost:5000/shorten?url=https://www.microsoft.com"

# Follow the redirect (replace SHORT_CODE with the returned value)
curl -i "http://localhost:5000/go/SHORT_CODE"
```

## Configuration
This app currently uses in-memory Orleans storage, so shortened URLs are lost when the app restarts.

## Secrets
If you add secrets (like API keys) for local development, use .NET user-secrets:
```powershell
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_KEY_HERE"
```

In production, configure secrets via environment variables, e.g. `OpenAI__ApiKey`.
