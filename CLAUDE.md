# eNet Anbindung — Deployment Notes

## SSH Access to Home Assistant
- Host: `homeassistant.local`, Port: `22222`, User: `root`

## eNet Sender Add-on (C# backend)

**Source:** `EnetSenderNet/`
**Deployed to:** `/addons/local/enetsender/` on HA Pi

### Deploy steps
Run from `C:\Dropbox\HomeVibePortal`:
```
powershell.exe -ExecutionPolicy Bypass -File deploy_enet.ps1
```
This script: bumps patch version → runs tests → builds linux-arm64 binary → uploads binary + config.yaml to Pi via SSH → `ha store reload` + `ha apps update` → verifies startup log.

See `deploy_enet.ps1` header for full explanation of each design decision.

### Versioning
`AssemblyVersion`/`FileVersion` in `EnetSenderNet/src/EnetSenderNet.csproj` and `version` in `EnetSenderNet/ha-addon/config.yaml` are kept in sync by the deploy script automatically.

---

## eNet HA Custom Integration (Python)

**Source:** `EnetSenderNet/ha-integration/custom_components/enet/`
**Deployed to:** `/homeassistant/custom_components/enet/` on HA Pi

### Deploy steps
```
scp -P 22222 ha-integration/custom_components/enet/*.py root@homeassistant.local:/homeassistant/custom_components/enet/
```
Then restart HA to pick up changes.

### Current state (2026-03-16)
- All files deployed and working. `light.lichtwohnbereich` (ch28) confirmed on/off. `light.lichtesszimmer` (ch27) reports UNDEFINED — needs physical inspection.
