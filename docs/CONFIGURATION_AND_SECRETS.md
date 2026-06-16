# Configuration And Secrets

Current date: 2026-06-15.

## Rule

Do not commit provider credentials, API tokens, OAuth secrets, or production connection strings.

## Current Local Config Pattern

Tracked config:

```text
src/P2W.Cards.Api/appsettings.json
src/P2W.Cards.Api/appsettings.Development.json
```

Ignored local override config:

```text
src/P2W.Cards.Api/appsettings.Local.json
src/P2W.Cards.Api/appsettings.Development.local.json
```

Example local override:

```text
src/P2W.Cards.Api/appsettings.Local.example.json
```

The API loads local overrides in `Program.cs`:

```text
appsettings.Local.json
appsettings.{Environment}.local.json
```

## Local Provider Credentials

For local development, copy:

```powershell
Copy-Item src/P2W.Cards.Api/appsettings.Local.example.json src/P2W.Cards.Api/appsettings.Local.json
```

Then put local provider credentials in:

```text
src/P2W.Cards.Api/appsettings.Local.json
```

That file is ignored by git.

Verify before committing:

```powershell
git check-ignore -v src/P2W.Cards.Api/appsettings.Local.json
```

## Provider Config Keys

Current provider config sections:

```text
Providers:Ebay:ClientId
Providers:Ebay:ClientSecret
Providers:Ebay:OAuthBaseUrl
Providers:Ebay:MarketplaceId
Providers:Ebay:BaseUrl
Providers:JustTcg:ApiKey
Providers:PokemonTCG:ApiKey
Providers:PriceCharting:ApiToken
```

Use environment variables in production or CI. ASP.NET Core maps nested config with double underscores:

```powershell
$env:Providers__Ebay__ClientId="..."
$env:Providers__Ebay__ClientSecret="..."
$env:Providers__JustTcg__ApiKey="..."
```

## Safe GitHub Push Checklist

Before pushing:

```powershell
git status -sb
git diff -- src/P2W.Cards.Api/appsettings.json
git check-ignore -v src/P2W.Cards.Api/appsettings.Local.json
```

Also scan for obvious provider tokens:

```powershell
rg -n "tcg_|PRD-|SBX-|ClientSecret|ApiKey|ApiToken" --glob "!src/P2W.Cards.Api/appsettings.Local.json"
```

Expected result:

- Real keys may exist only in ignored local files.
- Tracked appsettings should have blank provider credential values.
- Example files should contain placeholders only.

## If A Secret Was Committed

1. Rotate the exposed provider key in the provider dashboard.
2. Remove the secret from tracked files.
3. Rewrite git history only if the secret reached a remote or public branch and rotation is not enough.
4. Add or update ignore rules and examples.
5. Re-test the local app with local override config.

## Current Push Blockers

At the time this doc was created:

- GitHub CLI auth was expired locally.
- The local machine had working provider credentials in ignored local config.
- Tracked `appsettings.json` was sanitized.
