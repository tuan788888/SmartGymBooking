using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace SmartGymBooking.Services;

public static class ExerciseGuide
{
    public const string YouTubeChannel = "https://www.youtube.com/@muscleandstrength";
    private static readonly Dictionary<string, string> VideoIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bench Press"] = "rT7DgCr-3pg",
        ["Goblet Squat"] = "MeIiIdhvXT4",
        ["Push-Up"] = "vh72hbUqqfs",
        ["Dumbbell Lateral Raise"] = "3VcKaXpzqRo",
        ["Dumbbell Bicep Curl"] = "P1A9yXYxwAg",
        ["Close Grip Lat Pulldown"] = "Gspnzli9v2Y",
        ["Dumbbell Step-Up"] = "DFMgv0aySwk",
        ["Cable Triceps Pushdown"] = "FgXQdSOhpL0"
    };

    public static string Resolve(string exerciseName, string? videoUrl)
    {
        var searchUrl = $"{YouTubeChannel}/search?query={Uri.EscapeDataString(exerciseName)}";
        if (Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" &&
            uri.AbsoluteUri != searchUrl)
            return uri.AbsoluteUri;
        return VideoIds.TryGetValue(exerciseName, out var videoId)
            ? $"https://www.youtube.com/watch?v={videoId}"
            : searchUrl;
    }

    public static string? EmbedUrl(string videoUrl)
    {
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var youtube = uri.Host is "youtube.com" or "www.youtube.com" or "m.youtube.com";
        var videoId = (uri.Host, segments) switch
        {
            ("youtu.be", [var id]) => id,
            (_, ["watch"]) when youtube && QueryHelpers.ParseQuery(uri.Query).TryGetValue("v", out var value) => value.ToString(),
            (_, ["shorts" or "embed", var id]) when youtube => id,
            ("www.youtube-nocookie.com", ["embed", var id]) => id,
            _ => null
        };
        return videoId != null && Regex.IsMatch(videoId, @"\A[A-Za-z0-9_-]{11}\z")
            ? $"https://www.youtube-nocookie.com/embed/{videoId}"
            : null;
    }
}
