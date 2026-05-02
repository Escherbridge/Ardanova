# NU1301 — Unable to load the service index for `https://api.nuget.org/v3/index.json`

This is a **network / TLS / proxy** problem, not a bug in ArdaNova’s C# code. The .NET SDK must download the NuGet v3 service index before restore can run.

---

## 1. Confirm the feed is reachable

From the repo:

```bash
./ardanova-backend-api-mcp/scripts/verify-nuget-feed.sh
```

Or manually:

```bash
curl -fsS https://api.nuget.org/v3/index.json | head -c 200
```

You should see JSON starting with `{"version":"3.0.0",...}`. If `curl` fails, fix connectivity first (Wi‑Fi, VPN, corporate firewall, DNS).

---

## 2. Clear NuGet HTTP cache (stale or corrupted cache)

```bash
dotnet nuget locals http-cache --clear
dotnet nuget locals temp --clear
```

Then restore again:

```bash
cd ardanova-backend-api-mcp
dotnet restore api-server/src/ArdaNova.API/ArdaNova.API.csproj
```

---

## 3. Corporate proxy

If you must use an HTTP(S) proxy:

```bash
export HTTPS_PROXY=http://user:pass@proxy.example.com:8080
export HTTP_PROXY=http://user:pass@proxy.example.com:8080
```

Configure NuGet to use it (adjust URL and credentials as required):

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
# If your org documents a different NuGet mirror, add that source instead or in addition.
```

---

## 4. Linux: TLS / CA certificates

If `curl` works but `dotnet restore` fails, or you see SSL errors:

- Install/update CA bundle (e.g. Debian/Ubuntu: `sudo apt-get update && sudo apt-get install -y ca-certificates`).
- Ensure system time is correct (`date`).

---

## 5. IPv6-only broken network

If `localhost` / IPv6 routing is broken, NuGet may still use IPv4 to `api.nuget.org` in most setups. If you suspect IPv6 issues, try another network or disable IPv6 temporarily only for diagnosis (not as a permanent repo change).

---

## 6. Offline or air-gapped environments

You need a **local or internal NuGet feed** that mirrors required packages, or a pre-populated global package folder. This repo does not ship offline packages; coordinate with your team.

---

## 7. NuGet configuration used here

The backend uses [`ardanova-backend-api-mcp/api-server/NuGet.Config`](../ardanova-backend-api-mcp/api-server/NuGet.Config) with the official **nuget.org** v3 endpoint. No change to that file is required when the feed is reachable.

---

## Related

- [LOCAL_DEVELOPMENT_SMOKE.md](./LOCAL_DEVELOPMENT_SMOKE.md) — full local stack
