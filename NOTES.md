# eNet Sender — Runtime Notes

Operational state and learnings. Architecture and build/deploy steps live in `CLAUDE.md`.

---

## Deployed version

**2.0.2 (Go)** — deployed 2026-04-19. Slug `local_enetsender`, image `ghcr.io/joergni/enetsender`, state: started.

The C# .NET 8 add-on (`eNet Anbindung/EnetSenderNet/`) has been replaced by the Go rewrite (`EnetSenderGo/`). Same slug, same image name, same REST API surface — drop-in replacement. Memory footprint dropped from ~43 MB (C# + .NET runtime) to **4.3 MB** (Go static binary in scratch container).

### Last C# version
**1.5.34** — deployed 2026-04-16.
- TCP connect timeout added (3s via `CancellationTokenSource`) — prevents indefinite hang when Mobilegate unreachable
- `/health` simplified to liveness-only (always 200) — watchdog no longer kills add-on when Mobilegate is down
- `/mobilegate` endpoint added — returns "ok"/"down" (both HTTP 200); polled by `binary_sensor.mobilegate`

---

## Mobilegate

- **IP:** 192.168.178.34, **Port:** 9050 TCP
- **Protocol version:** `"0.03"` | **Firmware:** `0.91` | **Hardware:** `73355700` | **ENET:** `45068305`
- JSON messages terminated with `\r\n\r\n`
- The Mobilegate itself has never been the root cause of any problem. When eNet appears broken, look at the add-on, HA integration, or coordinator first.

---

## Channel map

Verified via physical test + IBN rename 2026-03-19.

`DEVICES` array: `[2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,1,1,3,1,2,1,1,2,7,1,2,0,...]`

HW type is reported by the physical actor hardware during eNet RF teach-in. It is **not configurable**.

| Ch | HW | Gate name | Code name | Notes |
|---|---|---|---|---|
| 0 | 2 | (Zentral) | — | unregistered |
| 16 | 1 | Schrank | Schrank | switch |
| 17 | 2 | Rollo Straße | OfficeStreet | non-position-aware (no encoder) |
| 18 | 1 | Rollo Garage | OfficeGarage | position-aware (has encoder) |
| 19 | 1 | RaffstoreEssen | RaffstoreEssen | pos-aware |
| 20 | 3 | RaffstoreTerassenTür | RaffstoreTerassenTuer | Raffstore |
| 21 | 1 | RolloEssen | RolloEssen | pos-aware |
| 22 | 2 | RolloSchlafzimmer | RolloSchlafzimmer | non-pos |
| 23 | 1 | RolloKueche | RolloKueche | pos-aware |
| 24 | 1 | RolloLeasZimmer | RolloLeasZimmer | pos-aware |
| 25 | 2 | RolloPaulsZimmer | RolloPaulsZimmer | non-pos |
| 26 | 7 | (Zentral) | — | scene/group channel |
| 27 | 1 | LichtEsszimmer | LichtEsszimmer | dimmer (HW type 1) |
| 28 | 2 | LichtWohnbereich | LichtWohnbereich | dimmer |

---

## UNDEFINED state behavior

- **HW type 2** (non-position-aware): always returns UNDEFINED — no encoder, state is unknowable.
- **HW type 1** (position-aware): also returns UNDEFINED if not actuated recently. Normal — not a fault.

---

## HTTP endpoints (port 8080, Docker gateway 172.30.32.1)

| Endpoint | Notes |
|---|---|
| `GET /health` | Liveness only — always 200. Used by HA supervisor watchdog. |
| `GET /mobilegate` | Probes Mobilegate VERSION_REQ; returns "ok" or "down" (both HTTP 200). |
| `GET /things` | All registered things with cached state. |
| `GET /diagnostics` | Firmware info + device types + thing states. |
| `GET /version` | Add-on assembly version. |
| `GET /joblog` | In-memory job log (last 10 days); also written to `/data/enet_jobs.log`. |
| `GET /joblog/debug` | Verbose debug log (last 10000 entries, in-memory only). |
| `POST /things/{ch}/up\|down\|position/{v}\|brightness/{v}` | Command endpoints. |

**Accessing joblog when add-on is down:**
```bash
./hassh "docker cp addon_local_enetsender:/data/enet_jobs.log /tmp/enet_jobs.log && cat /tmp/enet_jobs.log | grep -v heartbeat"
```

---

## HA integration

- `binary_sensor.enet_health` — REST sensor polling `/health` every 30s
- `binary_sensor.mobilegate` — REST sensor polling `/mobilegate` every 60s
- Custom component at `/homeassistant/custom_components/enet/` — YAML-based (no config entry)
- Coordinator backs off exponentially on failure (max ~16 min). HA restart resets backoff.

### Crash loop detection (learned 2026-04-15)

The old `alert_enet_down` automation (watching `"off"` state, 6 min `for:`) never fired during crash loops because:
1. When add-on is completely down, entity goes `unavailable` not `"off"`
2. Crash loop pattern (up 90s → down → up 90s → ...) kept resetting the `for:` timer

Fixed with two automations:
- `automation.alert_enet_sender_down` — watches `"off"` OR `unavailable` for 6 min
- `automation.alert_enet_sender_crash_loop` — fires immediately when health goes bad after being good < 10 min

---

## Known quirks

- **Dimming already-on lights:** SET command ignored when light is already on. Workaround: off → on-with-value.
- **Ch27 LichtEsszimmer:** registered as type1 (blind actor) but wired as dimmer — reports UNDEFINED. Needs physical inspection.
- **TCP crash loop root cause (2026-04-15):** missing connect timeout caused indefinite block when WiFi routing was disrupted. Fixed in 1.5.34 with 3s `CancellationTokenSource`.
