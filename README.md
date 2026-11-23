# AwwTogether

Website for **AwwTogether**, a UGC creator duo. It's a one-page portfolio with a contact form that brands use to send inquiries.

| Part | Tech |
| --- | --- |
| `client/` | React 19 + TypeScript, built with Vite |
| `server/AwwTogether.Api/` | ASP.NET Core 10 minimal API, EF Core + SQLite |
| `tests/AwwTogether.Api.Tests/` | xUnit integration tests for the API |

## Run it locally

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js 20+](https://nodejs.org).

Start the API in one terminal (it runs at http://localhost:5158):

```bash
dotnet run --project server/AwwTogether.Api
```

Start the site in a second terminal (it runs at http://localhost:5173 and forwards `/api` calls to the API):

```bash
cd client && npm install && npm run dev
```

## Editing the site's words, prices and photos

All copy lives in **`server/AwwTogether.Api/Data/content.json`**. That covers names, the about text, stats, portfolio items, testimonials, prices and contact handles. The server reloads the file as soon as you save it, so you don't need to rebuild.

- Anything in `[brackets]` is a placeholder to replace.
- **Photos and videos:** put files in `client/public/images/`. Then set `photoUrl` or `mediaUrl` to `"/images/your-file.jpg"`, or use `.mp4` for a video. Until you do, the site shows a labelled placeholder frame.
- Set `link` on a portfolio item to show a "View project →" link.

## Reading contact-form inquiries

Inquiries are saved to a SQLite database at `server/AwwTogether.Api/App_Data/awwtogether.db`. To list the latest 200, call the private endpoint with your admin key:

```bash
curl -H "X-Admin-Key: dev-only-admin-key" http://localhost:5158/api/inquiries
```

`dev-only-admin-key` is set in `appsettings.Development.json` and only works locally. **In production, set a long random key** through the `Admin__ApiKey` environment variable. If no key is set, the endpoint stays switched off.

Spam protection on the form:

- Each IP address can send at most 5 submissions every 10 minutes.
- A hidden "honeypot" field catches bots.
- The server re-validates every field.

## Tests and checks

```bash
dotnet test
```

```bash
cd client && npm run lint && npm run build
```

## Build for production

`npm run build` compiles the React app into `server/AwwTogether.Api/wwwroot/`. ASP.NET then serves the site and the API from a single app:

```bash
cd client && npm run build
```

```bash
dotnet publish server/AwwTogether.Api -c Release -o publish
```

Alternatively, use the included **Dockerfile**, which builds everything in one step:

```bash
docker build -t awwtogether .
```

```bash
docker run -p 8080:8080 -v aww-data:/data -e Admin__ApiKey=change-me awwtogether
```

Mount a volume at `/data` so saved inquiries survive redeploys. The app reads `X-Forwarded-*` headers, so it works behind hosts like Azure App Service, Render, Fly.io or nginx. Health check: `GET /health`.

## API

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/content` | All site copy (from `content.json`) |
| POST | `/api/inquiries` | Contact form submission (validated, rate-limited) |
| GET | `/api/inquiries` | List inquiries; needs the `X-Admin-Key` header |
| GET | `/health` | Health check |
| GET | `/openapi/v1.json` | OpenAPI spec (development only) |

## Still to do before launch

- Replace the placeholders in `content.json` and add real photos and videos.
- Add sources for the "Why UGC?" stats, or swap them out.
- Choose a host and domain, and set `Admin__ApiKey` there.
- Optional: email notifications for new inquiries (an SMTP or Resend/SendGrid key would be needed).
