// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
// This boundary is compiled only with the official SDK interop assembly.
#if MSFS_SIMCONNECT
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace MsfsInputBlackbox;

public sealed class SimConnectMonitor : IDisposable
{
    private const string ClientDataName = "MSFS.InputBlackbox.Telemetry.v1";
    private readonly SimConnect _simConnect;
    private bool _disposed;

    private enum ClientDataId : uint { Telemetry = 0x4B424F58 }
    private enum DefinitionId : uint { Telemetry = 0x4B424F59, AircraftState = 0x4B424F5B }
    private enum RequestId : uint { Telemetry = 0x4B424F5A, AircraftState = 0x4B424F5C }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    private struct AircraftStateData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        public double IndicatedAirspeedKnots;
        public double AltitudeFeet;
    }

    public event Action<TelemetryFrame>? FrameReceived;
    public event Action<AircraftState>? AircraftStateReceived;
    public event Action<string>? Disconnected;

    // hwnd must belong to a Windows message loop which dispatches the supplied message ID.
    public SimConnectMonitor(IntPtr hwnd, uint messageId)
    {
        MonitorStatusStore.Report("connecting", "Opening a SimConnect session.");
        _simConnect = new SimConnect("MSFS Input Blackbox Monitor", hwnd, messageId, null, 0);
        _simConnect.OnRecvClientData += OnReceiveClientData;
        _simConnect.OnRecvSimobjectData += OnReceiveAircraftState;
        _simConnect.OnRecvQuit += (_, _) => ReportDisconnected("SimConnect quit event");
        _simConnect.OnRecvException += (_, data) => ReportDisconnected($"SimConnect exception: {data.dwException}");

        _simConnect.MapClientDataNameToID(ClientDataName, ClientDataId.Telemetry);
        _simConnect.AddToClientDataDefinition(DefinitionId.Telemetry, 0,
            (uint)Marshal.SizeOf<TelemetryFrame>(), 0, 0);
        _simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, TelemetryFrame>(DefinitionId.Telemetry);
        _simConnect.RequestClientData(ClientDataId.Telemetry, RequestId.Telemetry,
            DefinitionId.Telemetry, SIMCONNECT_CLIENT_DATA_PERIOD.VISUAL_FRAME,
            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simConnect.AddToDataDefinition(DefinitionId.AircraftState, "TITLE", null,
            SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
        _simConnect.AddToDataDefinition(DefinitionId.AircraftState, "AIRSPEED INDICATED", "knots",
            SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        _simConnect.AddToDataDefinition(DefinitionId.AircraftState, "PLANE ALTITUDE", "feet",
            SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        _simConnect.RegisterDataDefineStruct<AircraftStateData>(DefinitionId.AircraftState);
        _simConnect.RequestDataOnSimObject(RequestId.AircraftState, DefinitionId.AircraftState,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND,
            SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        MonitorStatusStore.Report("connecting", "SimConnect session opened; waiting for telemetry.");
    }

    // Call from the host window's message handler for the message ID passed to the constructor.
    public void ReceiveMessage() => _simConnect.ReceiveMessage();

    private void OnReceiveClientData(SimConnect _, SIMCONNECT_RECV_CLIENT_DATA data)
    {
        if (data.dwRequestID != (uint)RequestId.Telemetry || data.dwData is not { Length: > 0 }) return;
        if (data.dwData[0] is not TelemetryFrame frame) return;
        FrameReceived?.Invoke(frame);
    }

    private void OnReceiveAircraftState(SimConnect _, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        if (data.dwRequestID != (uint)RequestId.AircraftState || data.dwData is not { Length: > 0 }) return;
        if (data.dwData[0] is not AircraftStateData state || string.IsNullOrWhiteSpace(state.Title)) return;
        if (!double.IsFinite(state.IndicatedAirspeedKnots) || !double.IsFinite(state.AltitudeFeet)) return;
        AircraftStateReceived?.Invoke(new AircraftState(state.Title.Trim(), state.IndicatedAirspeedKnots,
            state.AltitudeFeet, DateTimeOffset.UtcNow));
    }

    private void ReportDisconnected(string reason)
    {
        MonitorStatusStore.Report("disconnected", reason);
        Disconnected?.Invoke(reason);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _simConnect.Dispose();
    }
}
#endif
