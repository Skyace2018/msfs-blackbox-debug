// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Runtime.InteropServices;

namespace MsfsInputBlackbox;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TelemetryFrame
{
    public const int SizeInBytes = 36;
    public uint Timestamp;
    public float YokeX;
    public float YokeY;
    public float Rudder;
    public float Throttle;
    public uint Switches0;
    public uint Switches1;
    public uint Switches2;
    public uint Switches3;
}

