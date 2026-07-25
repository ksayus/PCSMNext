using Microsoft.Win32;
using PCSMNext.Core;
using PCSMNext.Core.Models;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PCSMNext.Services;

public class JavaService
{
    /// <summary>
    /// To scan installed Java version in this computer.
    /// 
    /// Scan strategy
    /// 1.JAVA_HOME environment variable.
    /// 2.Windows regedit (Java install info).
    /// 3.The java.exe in PATH environment variable.
    /// 4.From known directory.
    /// 5.User custom path (read from config).
    /// </summary>
    /// <returns>A list of JavaInfo.</returns>
    public List<JavaInfo> ScanInstalledJava()
    {
        var found = new Dictionary<string, JavaInfo>(StringComparer.OrdinalIgnoreCase);

        // 1.JAVA_HOME environment variable.
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            AddIfValid(found, Path.Combine(javaHome, "bin", "java.exe"));
        }

        // 2.Windows regedit scan.
        ScanRegistry(found);

        // 3.The java.exe in PATH environment variable.
        ScanPath(found);

        // 4.From known directory.
        ScanKnownPaths(found);

        var javaList = found.Values.ToList();

        // Duplicate warning: have multiple install path at same version.
        var duplicates = javaList
            .GroupBy(j => j.MajorVersion)
            .Where(g => g.Count() > 1);
        foreach (var dup in duplicates)
        {
            Log.Warning("Found {Count} JDK {Version} installed: {Paths}.",
                dup.Count(), dup.Key,
                string.Join(", ", dup.Select(j => j.InstallPath)));
        }

        Log.Information("Scan {Count} JDK installed.", javaList.Count);
        return javaList;
    }

    /// <summary>
    /// Scan Java install from Windows regedit.
    /// Windows regedit path:
    ///     HKLM\SOFTWARE\JavaSoft\Java Development Kit\{version}\JavaHome
    ///     HKLM\SOFTWARE\JavaSoft\JDK\{version}\JavaHome (JDK 9+ 新路径)
    ///     HKLM\SOFTWARE\JavaSoft\Runtime Environment\{version}\JavaHome
    /// </summary>
    /// <param name="found"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416", Justification = "<挂起>")]
    private void ScanRegistry(Dictionary<string, JavaInfo> found)
    {
        var registryPaths = new[]
        {
            @"SOFTWARE\JavaSoft\Java Development Kit",
            @"SOFTWARE\JavaSoft\JDK",
            @"SOFTWARE\JavaSoft\Runtime Environment",
        };

        foreach (var regPath in registryPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key == null) continue;

                foreach (var versionName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var versionKey = key.OpenSubKey(versionName);
                        var javaHome = versionKey?.GetValue("""JavaHome""") as string;
                        if (!string.IsNullOrEmpty(javaHome))
                        {
                            AddIfValid(found, Path.Combine(javaHome, "bin", "java.exe"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Failed to read regedit son: {Path}\\{Version} - {Error}.",
                            regPath, versionName, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to read regedi: {Path} - {Error}.",
                    regPath, ex.Message);
            }
        }
    }

    /// <summary>
    /// Search the java.exe in PATH environment variable.
    /// </summary>
    /// <param name="found"></param>
    private void ScanPath(Dictionary<string, JavaInfo> found)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var pathDir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(pathDir)) continue;
            AddIfValid(found, Path.Combine(pathDir, "java.exe"));
        }
    }

    /// <summary>
    /// Scan known Java installation directory.
    /// </summary>
    /// <param name="found"></param>
    private void ScanKnownPaths(Dictionary<string, JavaInfo> found)
    {
        var searchPaths = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Java"),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Programs", "Eclipse Adoptium"),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft"),
            // Oracle common installation path.
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonProgramFiles), "Oracle", "Java"),
        };

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path)) continue;

            foreach (var dir in Directory.GetDirectories(path))
            {
                AddIfValid(found, Path.Combine(dir, "bin", "java.exe"));
            }
        }
    }

    /// <summary>
    /// Verify and add to result set. (auto remove duplicate)
    /// </summary>
    /// <param name="found"></param>
    /// <param name="javaExePath"></param>
    private void AddIfValid(Dictionary<string, JavaInfo> found, string javaExePath)
    {
        if (string.IsNullOrEmpty(javaExePath)) return;
        if (found.ContainsKey(javaExePath)) return;

        var info = GetJavaInfo(javaExePath);
        if (info != null && info.IsValid)
        {
            found[javaExePath] = info;
        }
    }

    /// <summary>
    /// Get the version info of Java.
    /// </summary>
    /// <param name="javaExePath"></param>
    /// <returns>A JavaInfo obj or a null value.</returns>
    private JavaInfo? GetJavaInfo(string javaExePath)
    {
        if (!File.Exists(javaExePath)) return null;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaExePath,
                    Arguments = "-version",
                    // Output the Java version info to stderr, not stdout.
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
                Log.Warning("'Java -version' timed out: {Path}", javaExePath);
                return null;
            }

            if (process.ExitCode != 0)
            {
                Log.Debug("'java -version' return not zero exit code: {Path} exit={Exit}.",
                    javaExePath, process.ExitCode);
                return null;
            }

            // Use the regular expression extract the version num.
            var match = Regex.Match(output, @"version\s+""([^""]+)""");
            if (!match.Success) return null;

            var fullVersion = match.Groups[1].Value;
            var versionMatch = Regex.Match(fullVersion, @"^(\d+)");
            if (!versionMatch.Success) return null;

            var majorVersionStr = versionMatch.Success ? versionMatch.Groups[1].Value : "";

            var info = new JavaInfo
            {
                Version = fullVersion,
                MajorVersion = majorVersionStr,
                Path = javaExePath,
                InstallPath = Path.GetDirectoryName(
                    Path.GetDirectoryName(javaExePath)) ?? "",
                IsValid = true
            };

            var clean = fullVersion.Split('_')[0]; // Remove update
            if (Version.TryParse(clean, out var parsed))
                info.ParsedVersion = parsed;

            if (int.TryParse(majorVersionStr, out var majorNum))
                info.MajorVersionNumber = majorNum;

            return info;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get Java Version: {Path}.", javaExePath);
            return null;
        }
    }

    /// <summary>
    /// According to Minecraft version match best JDK.
    /// </summary>
    /// <param name="mcVersion"></param>
    /// <param name="installedJavas"></param>
    /// <returns>A JavaInfo obj or a null value.</returns>
    public JavaInfo? MatchJavaForMinecraft(string mcVersion, List<JavaInfo> installedJavas)
    {
        // Search the Java main version for required in Mapping.
        string? requiredJava = null;
        foreach (var (boundary, javaVersion) in Constants.MinecraftJavaMapping)
        {
            if (CompareVersions(mcVersion, boundary) >= 0)
            {
                requiredJava = javaVersion;
            }
        }

        if (requiredJava == null)
        {
            Log.Warning("Can`t match the Java version to Minecraft {Version}.", mcVersion);
            return null;
        }

        if (!int.TryParse(requiredJava, out var requiredMajor))
        {
            Log.Warning("Invalid required Java major version: {Java}", requiredJava);
            return null;
        }

        var validJavas = installedJavas.Where(j => j.IsValid).ToList();

        // Precise match: the major version are complete same.
        var exactMatch = installedJavas
            .Where(j => j.MajorVersion == requiredJava && j.IsValid)
            .OrderByDescending(j => j.ParsedVersion)
            .FirstOrDefault();

        if (exactMatch != null)
        {
            Log.Information("Minecraft {MC} -> JDK {Java} -> {Path}.",
                mcVersion, requiredJava, exactMatch.Path);
        }

        // Upward compatible: use higher version JDK.
        var compatibleMatch = installedJavas
            .Where(j => int.TryParse(j.MajorVersion, out var v) && v > requiredMajor && j.IsValid)
            .OrderBy(j => j.MajorVersionNumber)
            .ThenByDescending(j => j.Version)
            .FirstOrDefault();

        if (compatibleMatch != null)
        {
            Log.Information("Minecraft {MC} needed JDK {Java}，not found precise match，" +
                "use upward compatible JDK {Actual} -> {Path}.",
                mcVersion, requiredJava, compatibleMatch.MajorVersion, compatibleMatch.Path);
            return compatibleMatch;
        }

        // Complete haven`t Java which can use.
        Log.Warning("Minecraft {MC} needed JDK {Java}+, " +
            "but no any compatible Java installation was found in this system. Installed: {Installed}",
            mcVersion, requiredJava,
            installedJavas.Any()
                ? string.Join(", ", installedJavas.Select(j => $"JDK{j.MajorVersion}({j.Path})"))
                : "none");

        return null;
    }

    /// <summary>
    /// Simple semantic version comparion.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>A int type to display the compare result.</returns>
    private static int CompareVersions(string a, string b)
    {
        var partsA = a.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var partsB = b.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        for (int i = 0; i < Math.Max(partsA.Length, partsB.Length); i++)
        {
            var va = i < partsA.Length ? partsA[i] : 0;
            var vb = i < partsB.Length ? partsB[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }
}
