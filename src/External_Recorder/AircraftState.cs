// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
namespace MsfsInputBlackbox;

public readonly record struct AircraftState(
    string Title, double IndicatedAirspeedKnots, double AltitudeFeet, DateTimeOffset ObservedAtUtc);
