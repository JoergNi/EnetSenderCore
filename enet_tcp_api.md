# eNet Mobilegate TCP API — Technical Documentation

> **Sources:**
> - ✅ **Verified** — observed live from our Mobilegate (192.168.178.34) or in our codebase
> - ⚠️ **To verify** — found in public repos (ioBroker.enet, homebridge-gira-enet) but not tested locally
>
> **Public repo sources:** https://github.com/stoffel7/ioBroker.enet, https://github.com/SebastianSchultz/ioBroker.enet

---

## 1. Transport

| Property | Value |
|---|---|
| Protocol | TCP |
| Port | 9050 |
| Encoding | ASCII |
| Message framing | Each message terminated with `\r\n\r\n` (CRLF CRLF) |
| Connection model | New TCP connection per request; or persistent connection for subscriptions |
| Max response buffer | 65536 bytes observed sufficient |

**Connection pattern for a command/response:**
1. Open TCP connection
2. Send request (JSON + `\r\n\r\n`)
3. Read until socket timeout or EOF
4. Close connection

**Connection pattern for subscriptions (sign-in/out):**
1. Open TCP connection
2. Send `ITEM_VALUE_SIGN_IN_REQ` twice (both copies, confirmed in our code)
3. Receive acknowledgment after each
4. Send control command (`ITEM_VALUE_SET`)
5. Receive update
6. Send `ITEM_VALUE_SIGN_OUT_REQ`
7. Shutdown and close

---

## 2. Message Structure

Every message is a JSON object. Required fields present in all messages:

```json
{
  "CMD": "<command name>",
  "PROTOCOL": "0.03",
  "TIMESTAMP": "<unix timestamp or arbitrary string>"
}
```

- `TIMESTAMP` is not validated by the Mobilegate — static values like `"1421948265"` work fine ✅
- Field order in JSON does not matter ✅

---

## 3. Commands — Reference

### 3.1 `VERSION_REQ` — Get firmware version

**Request:**
```json
{"CMD":"VERSION_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265"}
```

**Response:** ✅
```json
{
  "CMD":"VERSION_RES",
  "PROTOCOL":"0.03",
  "TIMESTAMP":"08154711",
  "FIRMWARE":"0.91",
  "HARDWARE":"73355700",
  "ENET":"45068305"
}
```
- `FIRMWARE`: firmware version string ✅
- `HARDWARE`: hardware identifier (matches `HARDWARE` field in `GET_CHANNEL_INFO_ALL_RES`) ✅
- `ENET`: eNet stack identifier ✅

---

### 3.2 `ITEM_VALUE_SIGN_IN_REQ` — Subscribe to channel state

Subscribes to one or more channels and immediately returns the current state.

**Request:**
```json
{"CMD":"ITEM_VALUE_SIGN_IN_REQ","PROTOCOL":"0.03","ITEMS":[22],"TIMESTAMP":"1421948265"}
```

- `ITEMS`: array of integer channel numbers ✅
- Send the message **twice** to reliably get the initial state back (observed pattern in our code) ✅

**Response step 1 — acknowledgment:** ✅
```json
{"CMD":"ITEM_VALUE_SIGN_IN_RES","PROTOCOL":"0.03","TIMESTAMP":"08154711","ITEMS":[22]}
```
- `ITEMS` echoes back the subscribed channels
- Arrives before `ITEM_UPDATE_IND`

**Response step 2 — current state:**
```json
{"CMD":"ITEM_UPDATE_IND","PROTOCOL":"0.03","TIMESTAMP":"...","VALUES":[{"NUMBER":"22","STATE":"OFF","VALUE":"50"}]}
```

**Response (position-aware blind, moving):**
```json
{"CMD":"ITEM_UPDATE_IND","PROTOCOL":"0.03","TIMESTAMP":"...","VALUES":[{"NUMBER":"22","STATE":"...","VALUE":"-1"}]}
```
- `VALUE: -1` means the blind is currently in motion ✅

**Response (non-position-aware blind):**
```json
{"CMD":"ITEM_UPDATE_IND","PROTOCOL":"0.03","TIMESTAMP":"...","VALUES":[{"NUMBER":"20","STATE":"OFF","VALUE":"101"}]}
```
- `VALUE: 101` = stopped-down / `VALUE: 102` = stopped-up for non-position-aware actors ✅

**`ITEM_UPDATE_IND` VALUE fields:**

| Field | Meaning |
|---|---|
| `NUMBER` | Channel number (string) |
| `STATE` | State string (see below) |
| `VALUE` | Position/brightness; -1=moving, 101/102=non-position-aware |
| `SETPOINT` | Target brightness 0–255 for dimmers ⚠️ |

**STATE values:**

| State string | Meaning |
|---|---|
| `OFF` / `ALL_OFF` | Blind fully up / switch off |
| `ON` / `ALL_ON` | Blind fully down / switch on |
| `VALUE_BLINDS` | Blind at a specific position value |
| `VALUE_DIMM` | Dimmer at a specific brightness value ⚠️ |

---

### 3.3 `ITEM_VALUE_SIGN_OUT_REQ` — Unsubscribe from channel

**Request:**
```json
{"CMD":"ITEM_VALUE_SIGN_OUT_REQ","PROTOCOL":"0.03","ITEMS":[22],"TIMESTAMP":"1421948265"}
```

**Response:** ⚠️
```json
{"CMD":"ITEM_VALUE_SIGN_OUT_RES","PROTOCOL":"0.03","TIMESTAMP":"08154711"}
```
No payload beyond CMD echo.

---

### 3.4 `ITEM_VALUE_SET` — Set channel state

Used for blinds, switches, and dimmers.

#### Blind (position or open/close):
```json
{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948266","VALUES":[{"STATE":"VALUE_BLINDS","VALUE":50,"NUMBER":22}]}
```
- `VALUE`: 0 = fully open (up), 100 = fully closed (down) ✅
- Non-position-aware blinds ignore `VALUE` and go fully up or down based on direction ✅
- Must be subscribed (`SIGN_IN`) before sending `ITEM_VALUE_SET` for reliable update events ✅

#### Switch (on/off):
```json
{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948266","VALUES":[{"NUMBER":16,"STATE":"ON"}]}
```
- `STATE`: `"ON"` or `"OFF"` ✅

#### Dimmer:
```json
{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948266","VALUES":[{"NUMBER":27,"STATE":"ON","VALUE":128}]}
```
- `VALUE`: 0–255 (brightness level) ⚠️
- `STATE`: `"ON"` (our codebase) or `"VALUE_DIMM"` (ioBroker) — both appear valid ⚠️

Alternative using `VALUE_DIMM` STATE: ⚠️
```json
{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948266","VALUES":[{"STATE":"VALUE_DIMM","VALUE":128,"NUMBER":27}]}
```

#### Long press / hold simulation: ⚠️
```json
{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948266","VALUES":[{"STATE":"ON","LONG_CLICK":"ON","NUMBER":16}]}
```
- `LONG_CLICK`: `"ON"` — simulates a long button press; likely used for hold-to-move on blinds or scene triggers

**Response to `ITEM_VALUE_SET`:** ⚠️
```json
{"CMD":"ITEM_VALUE_RES","PROTOCOL":"0.03","TIMESTAMP":"...","VALUES":[{"NUMBER":16,"STATE":"OFF"}]}
```
- Confirms the command was accepted; echoes the resulting STATE

---

### 3.5 `GET_CHANNEL_INFO_ALL_REQ` — Get device type for all channels

Returns hardware type codes for all 40 channels (0-indexed).

**Request:**
```json
{"CMD":"GET_CHANNEL_INFO_ALL_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948266"}
```

**Response (our Mobilegate):** ✅ verified via unit test
```json
{
  "CMD":"GET_CHANNEL_INFO_ALL_RES",
  "PROTOCOL":"0.03",
  "TIMESTAMP":"08154711",
  "DEVICES":[2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,1,1,3,1,2,1,1,2,7,1,2,0,0,0,0,0,0,0,0,0,0,0]
}
```

**Origin of HW type codes:** ✅
The `DEVICES` code is **reported by the physical actor hardware** during the eNet RF teach-in process (`IBN_PB_DEVICES` indication). It reflects the actor's actual capabilities (e.g. whether the motor has a position encoder). It is **not** configurable via software — it is stored by the Mobilegate as received from the device. It is distinct from the `TYPE` string in `PROJECT_LIST_GET` (e.g. "Jalousie"), which is an installer-assigned label set via `IBN_EDIT_REQ`.

**Device type codes:**

| Code | Meaning |
|---|---|
| `0` | No device / unregistered channel |
| `1` | Actor with position feedback (position-aware blind, or switch actor) |
| `2` | Actor without position feedback (non-position-aware blind, or dimmer actor) |
| `3` | Raffstore / tilting blind actor — non-position-aware ✅ |
| `7` | Unknown — appears in ch26 ("EG Zentral" group), likely a group/scene controller ✅ |

**Our channel map (from DEVICES array + live verification):** ✅

| Ch | HW type | Gate name | Code name | Position-aware |
|---|---|---|---|---|
| 0 | 2 | (Zentral, unregistered) | — | no |
| 16 | 1 | Schrank | `Schrank` (switch) | — |
| 17 | 2 | Rollo Straße | `OfficeStreet` | ❌ no — motor has no encoder |
| 18 | 1 | Rollo Garage | `OfficeGarage` | ✅ yes — motor has encoder |
| 19 | 1 | Essen | `RaffstoreDining` | ✅ yes |
| 20 | 3 | Schiebetüre | `RaffstoreLiving` | ❌ no |
| 21 | 1 | Essen Straße | `DiningRoom` | ✅ yes |
| 22 | 2 | Garage | `SleepingRoom` | ❌ no |
| 23 | 1 | Küche | `Kitchen` | ✅ yes |
| 24 | 1 | Jalousie Gäste | `LeasRoom` | ✅ yes |
| 25 | 2 | Jalousie Leer | `PaulsRoom` | ❌ no |
| 26 | 7 | EG Zentral | — | — |
| 27 | 1 | Spots Essen | `LivingEsszimmer` (dimmer) | — |
| 28 | 2 | Spots Wohnen | `LivingWohnbereich` (dimmer) | — |

> **Gate name corrections (2026-03-19):** ch17 renamed from "Rollladen Garag" → "Rollo Straße" and ch18 from "Straße" → "Rollo Garage". The original gate names were swapped during commissioning. Code names in `ThingRegistry.cs` were already correct. ✅

> ⚠️ Note: ch27 (`Spots Essen`) reports HW type 1 (same code as position-aware blind actors) but is a dimmer. The type code meaning is context-dependent: for dimmer channels type 1 likely means "supports dimming/feedback" rather than position-awareness specifically.

---

### 3.6 `PROJECT_LIST_GET` — Get full project (rooms + device names)

Returns the complete project: rooms with channel ordering **and** all channels with their names and types. This is **the** command for device discovery.

**Request:**
```json
{"CMD":"PROJECT_LIST_GET","PROTOCOL":"0.03","TIMESTAMP":"1421948265"}
```

**Response (our Mobilegate):** ✅
```json
{
  "CMD":"PROJECT_LIST_RES",
  "PROTOCOL":"0.03",
  "TIMESTAMP":"08154711",
  "PROJECT_ID":"186",
  "FIRMWARE":"0.91",
  "LISTS":[
    {"NUMBER":0,"NAME":"Wohnzimmer","ICON":259,"ITEMS_ORDER":[16,21,28,20,27,19],"VISIBLE":true},
    {"NUMBER":1,"NAME":"Büro",      "ICON":35, "ITEMS_ORDER":[17,18],"VISIBLE":true},
    {"NUMBER":2,"NAME":"Wc",        "ICON":1,  "VISIBLE":true},
    {"NUMBER":3,"NAME":"HWR",       "ICON":259,"VISIBLE":true},
    {"NUMBER":4,"NAME":"Küche",     "ICON":3,  "ITEMS_ORDER":[23],"VISIBLE":true},
    {"NUMBER":5,"NAME":"Schlafzimmer","ICON":2,"ITEMS_ORDER":[22],"VISIBLE":true},
    {"NUMBER":6,"NAME":"Gästezimmer","ICON":34,"ITEMS_ORDER":[24],"VISIBLE":true},
    {"NUMBER":7,"NAME":"Paul",      "ICON":34, "ITEMS_ORDER":[25],"VISIBLE":true},
    {"NUMBER":8,"NAME":"Zentral",   "ICON":257,"ITEMS_ORDER":[0,26],"VISIBLE":true},
    ...rooms 9-19 visible:false, NAME:"Raum N"...
  ],
  "ITEMS":[
    {"NUMBER":0, "NAME":"Zentral ab",    "TYPE":"Scene",  "DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":16,"NAME":"Schrank",       "TYPE":"Binaer", "DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":17,"NAME":"Rollladen Garag","TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":18,"NAME":"Straße",        "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":19,"NAME":"Essen",         "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":20,"NAME":"Schiebetüre",   "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":21,"NAME":"Essen Straße",  "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":22,"NAME":"Garage",        "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":23,"NAME":"Küche",         "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":24,"NAME":"Jalousie Gäste","TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":25,"NAME":"Jalousie Leer", "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":26,"NAME":"EG Zentral",    "TYPE":"Jalousie","DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":27,"NAME":"Spots Essen",   "TYPE":"Dimmer", "DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":28,"NAME":"Spots Wohnen",  "TYPE":"Dimmer", "DIMMABLE":false,"WRITEABLE":false,"READABLE":false},
    {"NUMBER":41,"NAME":"Alles Aus",     "TYPE":"Scene",  "PROGRAMMABLE":"false"},
    {"NUMBER":42,"NAME":"MasterDimmen",  "TYPE":"Binaer", "PROGRAMMABLE":"false"},
    ...channels 29-39 NAME:"Kanal N", TYPE:"NONE" (unused slots)...
  ]
}
```

**LISTS fields:**
- `NUMBER`: room index (0–19) ✅
- `NAME`: room name ✅
- `ICON`: icon ID ✅
- `ITEMS_ORDER`: channel numbers in this room, display order ✅
- `VISIBLE`: false = unused/hidden room ✅

**ITEMS fields:**
- `NUMBER`: channel number ✅
- `NAME`: device name as configured in the Mobilegate ✅ — **this is the canonical source for device names**
- `TYPE`: device type string — `"Jalousie"`, `"Binaer"`, `"Dimmer"`, `"Scene"`, `"NONE"` ✅
- `DIMMABLE` / `WRITEABLE` / `READABLE`: capability flags (all false in fw 0.91) ✅
- `SUFFIX` / `MIN` / `MAX` / `STEP`: range metadata ✅
- `PROGRAMMABLE`: present on special scene channels ✅

---

### 3.7 `BLOCK_LIST_REQ` — Get block directory

Returns a directory of data blocks stored on the Mobilegate.

**Request:**
```json
{"CMD":"BLOCK_LIST_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","LIST-RANGE":1}
```
- `LIST-RANGE`: ioBroker always sends `1` ⚠️; omitting it still returns both ranges on our unit ✅

**Response (our Mobilegate):**
```json
{"CMD":"BLOCK_LIST_RES","STATE":0,"LIST-RANGE":1,"LIST-SIZE":[36,233,67,35,51,317,97,13,0,0],"DATA-IDS":[1,19,1,1,1,11,1,1,0,0]}
{"CMD":"BLOCK_LIST_RES","STATE":0,"LIST-RANGE":2,"LIST-SIZE":[0,0,0,0,0,0,0,0,0,0],"DATA-IDS":[0,0,0,0,0,0,0,0,0,0]}
```

- `LIST-SIZE`: byte size of each block ✅
- `DATA-IDS`: revision/type IDs for each block ✅
- Use `BLOCK_RESTORE_REQ` (section 3.8) to read block contents ✅

---

### 3.8 `BLOCK_RESTORE_REQ` — Read block data

Reads one of the 8 app-config blocks stored on the Mobilegate. The official eNet app writes its own configuration (icons, paths, custom types) into the Mobilegate's block storage and reads it back on fresh install.

**Request:**
```json
{"CMD":"BLOCK_RESTORE_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","DATA-NUM":5}
```
- `DATA-NUM`: block index 0–7 ✅

**Response (our Mobilegate, all blocks):** ✅

**Block 0** — Model version / page directory:
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":0,"STATE":0,"DATA-ID":1,
 "DATA":{"MODEL_VERSION":4,"PAGES_USED":[1]}}
```

**Block 1** — Channel icon assignments (ChannelConfig):
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":1,"STATE":0,"DATA-ID":19,
 "DATA":{
   "ICONS_CHANNELS":[259,257,257,257,257,257,257,257,257,257,257,259,259,0,0,0,0,0,0,0,0,0,0,0],
   "ICONS_SCENES":[257,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],
   "ICONS_SPECIAL":[260,0],
   "TINT_COLORS":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]
 }}
```
- `ICONS_CHANNELS`: icon IDs for channels 0–23 ✅
- `ICONS_SCENES`: icon IDs for scenes 0–15 ✅

**Block 2** — Multi-gate config:
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":2,"STATE":0,"DATA-ID":1,
 "DATA":{"ENABLED":false,"SLAVE_STANDALONE":true,"MASTER_GATE":45968008203}}
```
- `MASTER_GATE`: MAC address of the master Mobilegate (decimal) ✅

**Block 3** — Camera config:
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":3,"STATE":0,"DATA-ID":1,
 "DATA":{"URLS_EXT_PAGE":0,"NUM_CAMERAS":0}}
```

**Block 4** — URL paths (20 slots, camera/ext page URLs):
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":4,"STATE":0,"DATA-ID":1,
 "DATA":{"PATHS":[0,0,...20 zeros...]}}
```

**Block 5** — Extended channel config (ChannelExtConfig):
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":5,"STATE":0,"DATA-ID":11,
 "DATA":{
   "CH_TEXT_NAMES":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],
   "CSTM_TYPE":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,1,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,50],
   "CSTM_GROUP":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],
   "START_VALUES":[-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,100,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1]
 }}
```
- `CH_TEXT_NAMES`: indices into predefined text strings; all 0 = no custom labels set ✅
- `CSTM_TYPE`: custom type overrides per channel (41 entries); non-zero at ch17,18,22,23,26 ✅
- `START_VALUES`: initial values for channels; ch11=100, rest=-1 (no override) ✅

**Block 6** — Extended URL paths (43 slots):
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":6,"STATE":0,"DATA-ID":1,
 "DATA":{"PATHS":[0,0,...43 zeros...]}}
```

**Block 7** — Block storage version:
```json
{"CMD":"BLOCK_RESTORE_RES","DATA-NUM":7,"STATE":0,"DATA-ID":1,
 "DATA":{"VERSION":1}}
```

> **Note:** The Mobilegate enforces rate limiting on TCP connections. Opening connections faster than ~3 s apart results in `ECONNREFUSED`. ✅

---

### 3.8 `INDICATION` (unsolicited / error response)

Returned when an unknown command is sent.

```json
{"PROTOCOL":"0.03","TIMESTAMP":"08154711","CMD":"INDICATION","STATE":-12}
```

- `STATE: -12` = unknown/unsupported command ✅
- Sent in response to any unrecognized CMD ✅

---

## 4. UDP Discovery Protocol

Separate from the TCP API. Used by the official app to auto-discover Mobilegates on the LAN. ⚠️

- **Send:** UDP broadcast to `255.255.255.255:3112`
- **Payload:** `"Ich wusste, dass Sie zurueck kommen wuerden...\0\x02"`
- **Listen on:** UDP port `2906`
- **Response:** binary packet (≥15 bytes):
  - 1 byte: length
  - 2 bytes: magic number `65199` (little-endian)
  - 4 bytes: device IP address
  - variable: device name (null-terminated string)
  - 6 bytes: MAC address
  - 1 byte: state
  - 1 byte: manufacturer

Not used by our project — gateway IP is hardcoded.

---

## 5. Unsupported / Notes

### Commands confirmed unsupported on fw 0.91

| CMD | Result |
|---|---|
| `ITEM_LIST_GET` | `INDICATION STATE:-12` ✅ |
| `DEVICE_LIST_GET` | `INDICATION STATE:-12` ✅ |
| `PROJECT_GET` | `INDICATION STATE:-12` ✅ |
| `PROJECT_LIST_REQ` | `INDICATION STATE:-12` (correct name is `PROJECT_LIST_GET`) ✅ |

---

## 6. Gap Analysis — What Our Project Does Not Implement

Comparing known API surface against `EnetSenderNet` codebase:

| Feature | API | Our project | Notes |
|---|---|---|---|
| `ITEM_VALUE_SIGN_IN_RES` handling | Gateway→Client ACK ✅ | ❌ ignored | Response is discarded; works fine in practice |
| `ITEM_VALUE_SIGN_OUT_RES` handling | Gateway→Client ACK | ❌ ignored | Same — no impact |
| `ITEM_VALUE_RES` handling | Gateway→Client after SET | ❌ ignored | SET confirmation discarded |
| `SETPOINT` field in updates | Extra field in `ITEM_UPDATE_IND` | ❌ not parsed | May be relevant for dimmer state |
| `VALUE_DIMM` STATE | Alternative dimmer SET | ❌ uses `"ON"` | Both seem valid; untested which fw 0.91 prefers |
| `LONG_CLICK` in SET | Long press simulation | ❌ not implemented | May unlock blind hold-move or scenes |
| `VERSION_REQ` | Firmware/hardware info | ❌ not called | Could be useful for diagnostics |
| `BLOCK_LIST_REQ` with `LIST-RANGE` | Block directory | ⚠️ sends without param | Works but ioBroker sends `LIST-RANGE:1` |
| UDP discovery | Auto-discover gateway on LAN | ❌ not implemented | Hardcoded IP; not needed currently |
| `GET_CHANNEL_INFO_ALL_REQ` | Channel type codes | ✅ called at startup | Used for diagnostics only, not stored |
| `PROJECT_LIST_GET` | Room/channel mapping + **device names** | ✅ called at startup | ITEMS array has names; not used at runtime |
| `BLOCK_RESTORE_REQ` | Read app-config blocks | ❌ not called | Contains icon/custom-type config, not device names |

**Highest-value gaps:**
1. **Device names from `PROJECT_LIST_GET`** — ITEMS array has canonical names; ThingRegistry names should be kept in sync
2. **`SETPOINT` parsing** — may improve dimmer state accuracy
3. **`VALUE_DIMM` STATE** — worth testing to confirm which pattern fw 0.91 prefers

---

## 7. Observed Mobilegate Behaviour

- Multiple simultaneous TCP connections are supported ✅
- The Mobilegate sends `ITEM_UPDATE_IND` proactively to signed-in clients when a channel changes ✅
- The static timestamp `"1421948265"` (Jan 2015) is accepted without issue — timestamp is not validated ✅
- Firmware version on our unit: `0.91` / Hardware: `73355700` / ENET: `45068305` ✅
- Mobilegate IP: `192.168.178.34`, Port: `9050` ✅
- **Connection rate limit:** Opening new TCP connections faster than ~3 s apart causes `ECONNREFUSED`. Space rapid sequential queries by at least 3 s. ✅
- `PROJECT_LIST_GET` returns both LISTS (rooms) and ITEMS (all channels with names and types) in one response ✅
