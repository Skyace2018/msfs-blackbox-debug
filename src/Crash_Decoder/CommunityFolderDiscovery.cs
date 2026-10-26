// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.RegularExpressions;

namespace MsfsInputBlackbox.Decoder;

internal enum SimulatorVersion { Msfs2020, Msfs2024 }

internal sealed record CommunityFolderCandidate(SimulatorVersion Simulator, string Path, string Source, bool Exists)
{
    public string Display => $"{(Exists ? "✓" : "○")} {Simulator}: {Path}  ({Source})";
}

internal static partial class CommunityFolderDiscovery
{
    private static readonly (SimulatorVersion Simulator, string Label, string ConfigPath)[] KnownConfigs =
    {
        (SimulatorVersion.Msfs2020, "MSFS 2020 — Steam", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft Flight Simulator", "UserCfg.opt")),
        (SimulatorVersion.Msfs2020, "MSFS 2020 — Microsoft Store", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.FlightSimulator_8wekyb3d8bbwe", "LocalCache", "UserCfg.opt")),
        (SimulatorVersion.Msfs2024, "MSFS 2024 — Steam", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft Flight Simulator 2024", "UserCfg.opt")),
        (SimulatorVersion.Msfs2024, "MSFS 2024 — Microsoft Store", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.Limitless_8wekyb3d8bbwe", "LocalCache", "UserCfg.opt"))
    };

    public static IReadOnlyList<CommunityFolderCandidate> FindCandidates()
    {
        var candidates = new List<CommunityFolderCandidate>();
        foreach (var config in KnownConfigs)
        {
            if (!File.Exists(config.ConfigPath)) continue;
            try
            {
                var packagesPath = ExtractPackagesPath(File.ReadAllText(config.ConfigPath));
                if (packagesPath is not null) AddCandidatePaths(candidates, config.Simulator, packagesPath, config.Label + " UserCfg.opt");
            }
            catch (IOException) { /* A running installer may temporarily lock this file. */ }
            catch (UnauthorizedAccessException) { /* Show no path rather than requesting elevation. */ }
            catch (ArgumentException) { /* Ignore an invalid value in a local config. */ }
        }

        foreach (var (simulator, label, defaultPackagesPath) in DefaultPackageRoots())
            AddCandidatePaths(candidates, simulator, defaultPackagesPath, label + " default");

        return candidates
            .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.Exists).First())
            .OrderByDescending(candidate => candidate.Exists)
            .ThenBy(candidate => candidate.Simulator)
            .ToArray();
    }

    internal static string? ExtractPackagesPath(string userCfg)
    {
        var match = InstalledPackagesPathRegex().Match(userCfg);
        return match.Success ? Environment.ExpandEnvironmentVariables(match.Groups[1].Value.Trim()) : null;
    }

    private static void AddCandidatePaths(List<CommunityFolderCandidate> candidates, SimulatorVersion simulator, string packagesPath, string source)
    {
        // 2024 supports Community2024 for 2024-only packages; retain Community as a legacy-compatible fallback.
        var folderNames = simulator == SimulatorVersion.Msfs2024 ? new[] { "Community2024", "Community" } : new[] { "Community" };
        foreach (var folderName in folderNames)
        {
            var candidate = Path.Combine(packagesPath, folderName);
            candidates.Add(new CommunityFolderCandidate(simulator, candidate, source, Directory.Exists(candidate)));
        }
    }

    private static IEnumerable<(SimulatorVersion Simulator, string Label, string PackagesPath)> DefaultPackageRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return (SimulatorVersion.Msfs2020, "MSFS 2020 — Steam", Path.Combine(roaming, "Microsoft Flight Simulator", "Packages"));
        yield return (SimulatorVersion.Msfs2020, "MSFS 2020 — Microsoft Store", Path.Combine(local, "Packages", "Microsoft.FlightSimulator_8wekyb3d8bbwe", "LocalCache", "Packages"));
        yield return (SimulatorVersion.Msfs2024, "MSFS 2024 — Steam", Path.Combine(roaming, "Microsoft Flight Simulator 2024", "Packages"));
        yield return (SimulatorVersion.Msfs2024, "MSFS 2024 — Microsoft Store", Path.Combine(local, "Packages", "Microsoft.Limitless_8wekyb3d8bbwe", "LocalCache", "Packages"));
    }

    [GeneratedRegex("(?m)^\\s*InstalledPackagesPath\\s+\\\"([^\\\"]+)\\\"\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex InstalledPackagesPathRegex();
}

