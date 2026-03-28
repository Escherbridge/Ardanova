#!/usr/bin/env bash
# Quick check that NuGet.org is reachable (same URL the .NET SDK uses for restore).
set -euo pipefail
URL="https://api.nuget.org/v3/index.json"
echo "GET $URL"
code=$(curl -sS -o /dev/null -w "%{http_code}" --connect-timeout 15 "$URL" || echo "000")
if [[ "$code" == "200" ]]; then
  echo "OK (HTTP $code) — NuGet feed is reachable; if dotnet restore still fails, try: dotnet nuget locals http-cache --clear"
  exit 0
fi
echo "FAIL (HTTP $code) — fix network/DNS/firewall/proxy/certs before dotnet restore. See documentation/NUGET_NU1301.md"
exit 1
