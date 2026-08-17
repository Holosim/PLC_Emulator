---
name: pattern_global_json_sdk_rollforward
description: global.json rollForward "latestFeature" vs "latestMajor" field defect (DELIV-900, issue #27) and the isolated-single-SDK reproduction technique
metadata:
  type: project
---

## The defect

`global.json` pinned `"version": "8.0.100"` with `"rollForward": "latestFeature"`.
`latestFeature` only rolls forward *within the same major* (8.x). A client
workstation with only `9.0.317`/`10.0.303` installed (no `8.x` at all) hit
`NETSDK1141` in Visual Studio on every project — reported against issue #27
after the project's v1.0.2 release had already shipped and every RTVM row
was `Verified`. This is a real regression against a previously-`Verified`
DELIV-900, not a new unrelated ask — Systems Engineer flipped DELIV-900 back
to `In Implementation` for it rather than opening a fresh RTVM row.

**Fix:** change only `rollForward` to `"latestMajor"`, keep `"version":
"8.0.100"` as the floor. Matches Microsoft's own guidance ("build with
whatever's newest, don't require an exact match") and is a one-line diff.

## Why CI never caught this

`.github/workflows/build-and-test.yml` pre-provisions the exact `8.0.x` SDK
via `actions/setup-dotnet` before every build — so both the `ubuntu-latest`
and `windows-latest` CI legs always have an `8.x` SDK present regardless of
`global.json`. A clean client workstation has no such pre-provisioning step,
so this class of defect is structurally invisible to this project's CI no
matter how many times it's re-run. Worth remembering as a general lesson:
CI passing green is not evidence a fresh-clone/fresh-machine SDK-resolution
path works — CI's runner image and a client's workstation are different
populations, not just different OSes ([[pattern_nfr501_consolidation_review]]
covers the parallel OS-behavior case, this is the SDK-provisioning case).

## Reproduction technique (no admin rights needed, don't uninstall SDKs)

This pipeline's own build environment always has every SDK major installed
(8.x through 10.x), so it can't naturally reproduce "only a newer major is
present." Built an isolated `dotnet` install instead of touching the real
one:

```bash
mkdir -p /tmp/dotnetiso
for d in host shared packs templates LICENSE.txt ThirdPartyNotices.txt; do
  ln -s /usr/share/dotnet/$d /tmp/dotnetiso/$d
done
ln -s /usr/share/dotnet/dotnet /tmp/dotnetiso/dotnet
mkdir -p /tmp/dotnetiso/sdk /tmp/dotnetiso/sdk-manifests
ln -s /usr/share/dotnet/sdk/9.0.316 /tmp/dotnetiso/sdk/9.0.316
ln -s /usr/share/dotnet/sdk-manifests/9.0.100 /tmp/dotnetiso/sdk-manifests/9.0.100

cd /path/to/project
DOTNET_ROOT=/tmp/dotnetiso DOTNET_MULTILEVEL_LOOKUP=0 \
  PATH=/tmp/dotnetiso:$PATH NUGET_PACKAGES=/tmp/nugetcache_iso \
  /tmp/dotnetiso/dotnet build PlcEmulator.sln -c Release
```

`dotnet --list-sdks` under that root shows only `9.0.316` — real proof the
fix resolves under the client's exact SDK population, not just a "this
seems right per the docs" claim. Confirmed both directions: the broken
`global.json` reproduces the client's exact `NETSDK1141` error text
verbatim under this isolated install, and the fixed one resolves cleanly
and builds/tests (119/119) under it.

## Scope of the "check for other runner-specific assumptions" ask

When asked (as here) to sweep for anything else that might behave
differently on a clean client machine vs. this pipeline's pre-provisioned
image, checked and found clean: no `RuntimeIdentifier` pins, no hardcoded
OS-specific path literals, no `OSPlatform`/`RuntimeInformation` checks.
`TargetFramework net8.0` in every `.csproj` is *not* an SDK pin — it's a
target-framework moniker, and newer-major SDKs (9.x/10.x) build `net8.0`
projects fine, confirmed by the build above. Don't conflate TFM with SDK
version when doing this kind of sweep.
