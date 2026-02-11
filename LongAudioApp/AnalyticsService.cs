using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace LongAudioApp;

/// <summary>
/// GA4 Measurement Protocol analytics with full session management.
/// - client_id persists across app launches (stored in analytics_state.json)
/// - session_id regenerates after 30 minutes of inactivity
/// - engagement_time_msec ("100") is injected into every event
/// - Config loaded from firebase_config.json at runtime
/// - Debug mode uses /debug/mp/collect endpoint (DEBUG builds only)
/// - User-Agent set to browser-like string for GA4 OS/device parsing
/// - user_properties include region, language, app_version, platform, screen_resolution
/// </summary>
public static class AnalyticsService
{
    private static readonly HttpClient _httpClient;
    private const string CollectEndpoint = "https://www.google-analytics.com/mp/collect";
    private const string DebugEndpoint = "https://www.google-analytics.com/debug/mp/collect";
    private const int SessionTimeoutMinutes = 30;

#if DEBUG
    private const bool EnableDebugMode = true;
#else
    private const bool EnableDebugMode = false;
#endif

    // Loaded from firebase_config.json
    private static string _measurementId = "";
    private static string _apiSecret = "";

    // Session state
    private static string _clientId = "";
    private static string _sessionId = "";
    private static DateTime _lastActivity = DateTime.UtcNow;

    // App metadata (cached once)
    private static readonly string _appVersion;
    private static readonly string _screenResolution;
    private static readonly string _deviceRegion;
    private static readonly string _language;

    static AnalyticsService()
    {
        // Cache app metadata
        _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        _deviceRegion = GetRegionSafe();
        _language = CultureInfo.CurrentCulture.Name;

        // Screen resolution (WPF SystemParameters)
        try
        {
            _screenResolution = $"{(int)SystemParameters.PrimaryScreenWidth}x{(int)SystemParameters.PrimaryScreenHeight}";
        }
        catch
        {
            _screenResolution = "unknown";
        }

        _httpClient = new HttpClient();

        // Set User-Agent to a browser-like string so GA4 parses OS/device correctly
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 TurboScribe/{_appVersion}");

        // Set Accept-Language for language/locale demographics
        try
        {
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(_language);
        }
        catch { /* ignore */ }
    }

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "analytics_state.json");

    // ===== Public API =====

    /// <summary>Initialize on app startup — loads config, loads/creates client_id, starts session.</summary>
    public static void Initialize()
    {
        LoadConfig();
        LoadState();
        EnsureSession();

        // Send session_start followed by app_start on every launch
        _ = TrackEventAsync("session_start");
        _ = TrackEventAsync("app_start");
    }

    /// <summary>Fire-and-forget an event with optional parameters.</summary>
    public static async Task TrackEventAsync(string eventName, object? extraParams = null)
    {
        if (!IsEnabled || string.IsNullOrEmpty(_measurementId)) return;

        try
        {
            EnsureSession();
            await SendEventInternal(eventName, extraParams);
        }
        catch
        {
            // Analytics should never crash the app
        }
    }

    /// <summary>Convenience fire-and-forget wrapper (no await needed).</summary>
    public static void TrackEvent(string eventName, object? extraParams = null)
    {
        _ = TrackEventAsync(eventName, extraParams);
    }

    public static bool IsEnabled { get; set; } = true;

    // ===== Internal =====

    private static async Task SendEventInternal(string eventName, object? extraParams = null)
    {
        // Build params — engagement_time_msec MUST be a string per GA4 Measurement Protocol
        var paramsDict = new System.Collections.Generic.Dictionary<string, object>
        {
            ["session_id"] = _sessionId,
            ["engagement_time_msec"] = "100"
        };

        // Add debug_mode flag inside params so events show in GA4 DebugView
        if (EnableDebugMode)
        {
            paramsDict["debug_mode"] = 1;
        }

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
                device_region = new { value = _deviceRegion },
                language = new { value = _language },
                app_version = new { value = _appVersion },
                platform = new { value = "windows" },
                screen_resolution = new { value = _screenResolution }
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

        // Use the correct endpoint: /debug/mp/collect for debug, /mp/collect for production
        var baseUrl = EnableDebugMode ? DebugEndpoint : CollectEndpoint;
        var url = $"{baseUrl}?measurement_id={_measurementId}&api_secret={_apiSecret}";
        var body = JsonSerializer.Serialize(payload);

        var response = await _httpClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

        // In debug mode, log the validation response
        if (EnableDebugMode)
        {
            try
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Analytics] {eventName} → {response.StatusCode}: {responseBody}");
            }
            catch { /* ignore */ }
        }

        _lastActivity = DateTime.UtcNow;
        SaveState();
    }

    // ===== Session Management =====

    /// <summary>
    /// Ensures a valid session exists. Creates a new session if:
    /// - No session_id exists yet (first launch)
    /// - More than 30 minutes have elapsed since last activity
    /// </summary>
    private static void EnsureSession()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastActivity;

        if (string.IsNullOrEmpty(_sessionId) || elapsed.TotalMinutes > SessionTimeoutMinutes)
        {
            _sessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            _lastActivity = now;
            SaveState();
        }
    }

    // ===== Configuration =====

    private static void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase_config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("measurementId", out var mid))
                    _measurementId = mid.GetString() ?? "";
                if (root.TryGetProperty("apiSecret", out var sec))
                    _apiSecret = sec.GetString() ?? "";
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Analytics] firebase_config.json not found — analytics disabled.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] Failed to load config: {ex.Message}");
        }
    }

    // ===== Persistence =====

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

        // Ensure client_id exists (persists across launches for accurate user count)
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

    // ===== Helpers =====

    private static string GetRegionSafe()
    {
        try
        {
            return RegionInfo.CurrentRegion.TwoLetterISORegionName ?? "XX";
        }
        catch
        {
            return "XX";
        }
    }

    private class AnalyticsState
    {
        public string? ClientId { get; set; }
        public string? SessionId { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
