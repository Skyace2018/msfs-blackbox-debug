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
    private enum DefinitionId : uint { Telemetry = 0x4B424F59 }
    private enum RequestId : uint { Telemetry = 0x4B424F5A }

    public event Action<TelemetryFrame>? FrameReceived;
    public event Action<string>? Disconnected;

    // hwnd must belong to a Windows message loop which dispatches the supplied message ID.
    public SimConnectMonitor(IntPtr hwnd, uint messageId)
    {
        MonitorStatusStore.Report("connecting", "Opening a SimConnect session.");
        _simConnect = new SimConnect("MSFS Input Blackbox Monitor", hwnd, messageId, null, 0);
        _simConnect.OnRecvClientData += OnReceiveClientData;
        _simConnect.OnRecvQuit += (_, _) => ReportDisconnected("SimConnect quit event");
        _simConnect.OnRecvException += (_, data) => ReportDisconnected($"SimConnect exception: {data.dwException}");

        _simConnect.MapClientDataNameToID(ClientDataName, (uint)ClientDataId.Telemetry);
        _simConnect.AddToClientDataDefinition((uint)DefinitionId.Telemetry, 0,
            (uint)Marshal.SizeOf<TelemetryFrame>(), 0, 0);
        _simConnect.RegisterStruct<TelemetryFrame>((uint)DefinitionId.Telemetry);
        _simConnect.RequestClientData((uint)ClientDataId.Telemetry, (uint)RequestId.Telemetry,
            (uint)DefinitionId.Telemetry, SIMCONNECT_CLIENT_DATA_PERIOD.SIMCONNECT_CLIENT_DATA_PERIOD_VISUAL_FRAME,
            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_CHANGED, 0, 0, 0);
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
