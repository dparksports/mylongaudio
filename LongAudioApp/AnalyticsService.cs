using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LongAudioApp;

/// <summary>
/// GA4 Measurement Protocol analytics with session management.
/// - client_id persists across app launches (stored in settings.json)
/// - session_id regenerates after 30 minutes of inactivity
/// - engagement_time_msec is injected into every event
/// </summary>
public static class AnalyticsService
{
    private static readonly HttpClient _httpClient; 
    private const string Endpoint = "https://www.google-analytics.com/mp/collect";
    private const string MeasurementId = "G-B387NLSSJX";
    private const string ApiSecret = "ch411kMtTRW7z_3XEUlmiw";
    private const int SessionTimeoutMinutes = 30;

    private static string _clientId = "";
    private static string _sessionId = "";
    private static DateTime _lastActivity = DateTime.UtcNow;

    static AnalyticsService()
    {
        _httpClient = new HttpClient();
        // Set User-Agent to identify the app and OS (critical for GA4 device/OS data)
        // Mimicking a browser-like string or a standard app string helps GA4 parse it.
        // We use a generic Windows User-Agent for now as we know this is a specific app.
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 LongAudioApp/1.0");
        
        // Set Accept-Language to current culture (for Location/Language demographics)
        try
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        }
        catch { /* ignore */ }
    }

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "analytics_state.json");

    /// <summary>Initialize on app startup — loads or creates client_id, starts session.</summary>
    public static void Initialize()
    {
        LoadState();
        // Trigger app_start, which will also trigger session_start if needed due to our new logic
        TrackEvent("app_start");
    }

    /// <summary>Fire-and-forget an event with optional parameters.</summary>
    public static async Task TrackEventAsync(string eventName, object? extraParams = null)
    {
        if (!IsEnabled) return;

        try
        {
            // check for session rotation
            if (CheckSession(out bool isNewSession))
            {
                SaveState();
                if (isNewSession)
                {
                    // Recursively send session_start (safe because session is now fresh)
                    await SendEventInternal("session_start");
                }
            }

            // Send legitimate event
            await SendEventInternal(eventName, extraParams);
        }
        catch
        {
            // Analytics should never crash the app
        }
    }

    private static async Task SendEventInternal(string eventName, object? extraParams = null)
    {
        // Build basic params (engagement_time_msec must be a number, not string)
        var paramsDict = new System.Collections.Generic.Dictionary<string, object>
        {
            ["session_id"] = _sessionId,
            ["engagement_time_msec"] = 100 
        };

        // Merge extra params
        if (extraParams != null)
        {
            try 
            {
                var json = JsonSerializer.Serialize(extraParams);
                var extra = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                if (extra != null)
                {
                    foreach (var kv in extra)
                        paramsDict[kv.Key] = kv.Value;
                }
            }
            catch { /* ignore param serialization errors */ }
        }

        var payload = new
        {
            client_id = _clientId,
            user_properties = new
            {
                // Rename 'country' to 'device_region' to avoid conflict with GA4 Auto-Geo
                device_region = new { value = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName ?? "XX" },
                language = new { value = System.Globalization.CultureInfo.CurrentCulture.Name },
                app_version = new { value = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown" },
                platform = new { value = "windows" }
            },
            events = new[]
            {
                new
                {
                    name = eventName,
                    @params = paramsDict
                }
            }
        };

        var url = $"{Endpoint}?measurement_id={MeasurementId}&api_secret={ApiSecret}";
        var body = JsonSerializer.Serialize(payload);
        await _httpClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

        _lastActivity = DateTime.UtcNow;
        SaveState();
    }

    /// <summary>Convenience fire-and-forget wrapper (no await needed).</summary>
    public static void TrackEvent(string eventName, object? extraParams = null)
    {
        _ = TrackEventAsync(eventName, extraParams);
    }

    // ===== Session Management =====

    /// <summary>
    /// Checks if session is expired or missing. 
    /// Returns true if state changed (new session created or loaded).
    /// </summary>
    private static bool CheckSession(out bool isNewSession)
    {
        isNewSession = false;
        if (!IsEnabled) return false;

        var now = DateTime.UtcNow;
        var elapsed = now - _lastActivity;

        if (string.IsNullOrEmpty(_sessionId) || elapsed.TotalMinutes > SessionTimeoutMinutes)
        {
            _sessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            _lastActivity = now;
            isNewSession = true;
            return true;
        }
        
        return false;
    }

    // ===== Persistence =====

    public static bool IsEnabled { get; set; } = true;

    private static void LoadState()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var state = JsonSerializer.Deserialize<AnalyticsState>(json);
                if (state != null)
                {
                    _clientId = state.ClientId ?? "";
                    _sessionId = state.SessionId ?? "";
                    _lastActivity = state.LastActivity;
                    IsEnabled = state.IsEnabled;
                }
            }
        }
        catch { }

        // Ensure client_id exists
        if (string.IsNullOrEmpty(_clientId))
        {
            _clientId = Guid.NewGuid().ToString();
            SaveState();
        }
    }

    private static void SaveState()
    {
        try
        {
            var state = new AnalyticsState
            {
                ClientId = _clientId,
                SessionId = _sessionId,
                LastActivity = _lastActivity,
                IsEnabled = IsEnabled
            };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private class AnalyticsState
    {
        public string? ClientId { get; set; }
        public string? SessionId { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
