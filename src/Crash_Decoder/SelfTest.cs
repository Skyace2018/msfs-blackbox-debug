// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal static class SelfTest
{
    public static bool Run()
    {
        var dumpPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-decoder-self-test.json");
        var payload = new
        {
            schema = TelemetryDumpReader.SchemaV1, reason = "synthetic-self-test", capturedAtUtc = DateTimeOffset.UtcNow,
            frames = new[]
            {
                new { Timestamp = 10u, YokeX = 0.0f, YokeY = 0.1f, Rudder = 0.2f, Throttle = 0.3f, Switches0 = 1u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u },
                new { Timestamp = 20u, YokeX = 0.4f, YokeY = 0.5f, Rudder = 0.6f, Throttle = 0.7f, Switches0 = 2u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u }
            }
        };
        File.WriteAllText(dumpPath, JsonSerializer.Serialize(payload));
        var configPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-usercfg-self-test.opt");
        File.WriteAllText(configPath, "InstalledPackagesPath \"D:\\MSFS Packages\"");
        var dump = TelemetryDumpReader.Read(dumpPath);
        return dump.Frames.Count == 2 && dump.Frames[1].Timestamp == 20
            && string.Equals(CommunityFolderDiscovery.ExtractPackagesPath(File.ReadAllText(configPath)), "D:\\MSFS Packages", StringComparison.OrdinalIgnoreCase);
    }
}

