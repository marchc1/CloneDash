using Nucleus.Util;
using System;
using System.Reflection;
using Velopack;
using Velopack.Locators;

namespace CloneDash;

public struct GameVersion
{
    public static GameVersion FromBuildInfo(string extra)
    {
        DateTime dt = DateTime.Parse(BuildInfo.BuildDate).ToUniversalTime();
        return new($"{dt.Year}", $"{dt.Month:00}", $"{dt.Day:00}", extra);
    }

    public static GameVersion GetCurrentVersion()
    {
        try
        {
            // Try to pull the active version from Velopack if installed
            var vpkVersion = VelopackLocator.Current?.CurrentlyInstalledVersion;
            if (vpkVersion != null)
            {
                var parts = vpkVersion.ToString().Split('.');

                string year = parts.Length > 0 ? parts[0] : "0";
                // Pad the month and day back out to keep the MM/DD style
                string month = parts.Length > 1 ? int.Parse(parts[1]).ToString("00") : "00";
                string day = parts.Length > 2 ? int.Parse(parts[2]).ToString("00") : "00";

                // Grab the 4th part (the github run number) for the extra field
                string extra = parts.Length > 3 ? parts[3] : "installed";

                return new GameVersion(year, month, day, extra);
            }
        }
        catch
        {
            // Ignore Velopack complaints if not using installed ver.
        }

        // Fallback for portable zip builds or local dev
        return FromBuildInfo("portable");
    }

    public static readonly GameVersion Current = GetCurrentVersion();

    public string Year;
    public string Month;
    public string Day;
    public string? Extra;

    public GameVersion(string year, string month, string day, string? extra = null)
    {
        this.Year = year;
        this.Month = month;
        this.Day = day;
        this.Extra = extra;
    }

    public override string ToString() => $"{Year}.{Month}.{Day}" + (Extra == null ? "" : $" {Extra}");
}