# Client Data protocol contract

`MSFS.InputBlackbox.Telemetry.v1` is the name mapped by both endpoints to client-data ID `0x4B424F58`. The originating endpoint owns creation of the area; the reader maps the same name and requests it periodically.

## Frame: version 1

All values are little-endian and the exact frame is 36 bytes. No pointers, strings, platform-sized integers, or implicit padding are permitted.

| Offset | Type | Field |
| ---: | --- | --- |
| 0 | `uint32` | monotonic milliseconds (`timestamp`) |
| 4 | `float32` | normalized yoke X |
| 8 | `float32` | normalized yoke Y |
| 12 | `float32` | normalized rudder |
| 16 | `float32` | normalized throttle |
| 20 | `uint32` | discrete switches word 0 |
| 24 | `uint32` | discrete switches word 1 |
| 28 | `uint32` | discrete switches word 2 |
| 32 | `uint32` | discrete switches word 3 |

## SimConnect call sequence

The documented desktop Client Data API names are `SimConnect_MapClientDataNameToID`, `SimConnect_CreateClientData`, `SimConnect_AddToClientDataDefinition`, `SimConnect_SetClientData`, and `SimConnect_RequestClientData`. There is no documented `SimConnect_MapClientDataAreaToSimConnectArea` call; this repository deliberately does not perpetuate that name.

1. The sender maps the named area, creates 36 bytes with `SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT`, and adds a definition for exactly 36 bytes.
2. Each gauge tick samples controls, fills one local `TelemetryFrame`, and sets the whole definition in one call.
3. The monitor maps the same name, adds the matching definition, registers the C# struct, and requests periodic updates.

## Runtime boundary

The desktop SimConnect API is Windows-native. Whether a particular MSFS 2024 WASM gauge packaging/toolchain supports the client-data calls is a target-SDK question, not something this repository assumes. The C++ source has an explicit `MSFS_WASM_SIMCONNECT_SUPPORTED` build gate to prevent an accidental unsupported build. Verify it with the SDK documentation and the live plan before enabling it.

