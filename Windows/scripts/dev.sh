#!/usr/bin/env bash
# Windows-only development launcher, invoked by `just dev` through Git Bash.
set -euo pipefail

# Running inside Windows applies its global.json SDK selection policy.
windows_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$windows_dir"

dotnet_cmd="$(command -v dotnet || true)"
if [[ -z "$dotnet_cmd" ]]; then
    local_app_data="${LOCALAPPDATA:-}"
    if [[ -n "$local_app_data" ]]; then
        local_app_data="$(cygpath -u "$local_app_data")"
        local_sdk="$local_app_data/HarmonyBuild/dotnet/dotnet.exe"
        if [[ -f "$local_sdk" ]]; then
            dotnet_cmd="$local_sdk"
            export DOTNET_ROOT
            DOTNET_ROOT="$(cygpath -w "$(dirname -- "$local_sdk")")"
        fi
    fi
fi

if [[ -z "$dotnet_cmd" ]]; then
    printf 'The .NET 10 SDK was not found. Install it and reopen your terminal, then run just dev again.\n' >&2
    exit 1
fi

# Restore/build automatically; additional arguments are useful for --help checks.
exec "$dotnet_cmd" run --project HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj --configuration Debug "$@"
