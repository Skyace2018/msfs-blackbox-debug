// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
//
// This source is intentionally gated: validate that the target MSFS 2024 SDK
// supports Client Data calls from gauge WASM before defining MSFS_WASM_SIMCONNECT_SUPPORTED.
#include <stdint.h>
#include <string.h>
#include <MSFS/MSFS.h>
#include <MSFS/Legacy/gauges.h>

#if !defined(MSFS_WASM_SIMCONNECT_SUPPORTED)
#error "Define MSFS_WASM_SIMCONNECT_SUPPORTED only after validating Client Data support in the target MSFS SDK."
#endif

#include <SimConnect.h>

static constexpr const char* kClientDataName = "MSFS.InputBlackbox.Telemetry.v1";
static constexpr DWORD kClientDataId = 0x4B424F58;
static constexpr DWORD kClientDataDefinition = 0x4B424F59;

#pragma pack(push, 1)
struct TelemetryFrame {
    uint32_t timestamp;
    float yokeX;
    float yokeY;
    float rudder;
    float throttle;
    uint32_t switches[4];
};
#pragma pack(pop)
static_assert(sizeof(TelemetryFrame) == 36, "The v1 client-data frame is exactly 36 bytes.");

static HANDLE g_simconnect = nullptr;
static bool g_ready = false;

static float ReadNormalized(const char* expression) {
    double value = 0.0;
    execute_calculator_code(expression, &value, nullptr, nullptr);
    return static_cast<float>(value);
}

static TelemetryFrame ReadFrame() {
    TelemetryFrame frame{};
    frame.timestamp = static_cast<uint32_t>(GetTickCount64());
    frame.yokeX = ReadNormalized("(A:AILERON POSITION, position)");
    frame.yokeY = ReadNormalized("(A:ELEVATOR POSITION, position)");
    frame.rudder = ReadNormalized("(A:RUDDER PEDAL POSITION, position)");
    frame.throttle = ReadNormalized("(A:GENERAL ENG THROTTLE LEVER POSITION:1, percent) 0.01 *");
    frame.switches[0] = ReadNormalized("(A:MASTER BATTERY, bool)") != 0.0f ? 1u : 0u;
    return frame;
}

extern "C" MSFS_CALLBACK void module_init() {
    if (FAILED(SimConnect_Open(&g_simconnect, "MSFS Input Blackbox WASM", nullptr, 0, nullptr, 0))) return;
    if (FAILED(SimConnect_MapClientDataNameToID(g_simconnect, kClientDataName, kClientDataId))) return;
    if (FAILED(SimConnect_CreateClientData(g_simconnect, kClientDataId, sizeof(TelemetryFrame), SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT))) return;
    if (FAILED(SimConnect_AddToClientDataDefinition(g_simconnect, kClientDataDefinition, 0, sizeof(TelemetryFrame)))) return;
    g_ready = true;
}

// Invoke from the panel service/timer callback approved by the target SDK.
extern "C" MSFS_CALLBACK void blackbox_service_tick() {
    if (!g_ready) return;
    const TelemetryFrame frame = ReadFrame();
    SimConnect_SetClientData(g_simconnect, kClientDataId, kClientDataDefinition,
        SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT, 0, sizeof(frame), &frame);
}

extern "C" MSFS_CALLBACK void module_deinit() {
    g_ready = false;
    if (g_simconnect != nullptr) { SimConnect_Close(g_simconnect); g_simconnect = nullptr; }
}

