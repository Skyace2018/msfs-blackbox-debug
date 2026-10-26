// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = RunSelfTest() ? 0 : 1;
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new DecoderForm(args.FirstOrDefault(File.Exists)));
    }

    private static bool RunSelfTest()
    {
        var path = Path.Combine(Path.GetTempPath(), "msfs-blackbox-decoder-self-test.json");
        var payload = new
        {
            schema = TelemetryDumpReader.SchemaV1, reason = "synthetic-self-test", capturedAtUtc = DateTimeOffset.UtcNow,
            frames = new[]
            {
                new { Timestamp = 10u, YokeX = 0.0f, YokeY = 0.1f, Rudder = 0.2f, Throttle = 0.3f, Switches0 = 1u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u },
                new { Timestamp = 20u, YokeX = 0.4f, YokeY = 0.5f, Rudder = 0.6f, Throttle = 0.7f, Switches0 = 2u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u }
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload));
        var dump = TelemetryDumpReader.Read(path);
        var configPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-usercfg-self-test.opt");
        File.WriteAllText(configPath, "InstalledPackagesPath \"D:\\MSFS Packages\"");
        var packagesPath = CommunityFolderDiscovery.ExtractPackagesPath(File.ReadAllText(configPath));
        return dump.Frames.Count == 2 && dump.Frames[1].Timestamp == 20 && Math.Abs(dump.Frames[1].Throttle - 0.7f) < 0.0001f
            && string.Equals(packagesPath, "D:\\MSFS Packages", StringComparison.OrdinalIgnoreCase);
    }
}

