using AlAnsariPrivacyShield.Models;

namespace AlAnsariPrivacyShield.Services;

public static class PrivacyCatalog
{
    public const string Telemetry = "Telemetry & diagnostics";
    public const string Advertising = "Advertising & suggestions";
    public const string Search = "Search & Cortana";
    public const string Activity = "Activity & clipboard";
    public const string Location = "Location & remote access";

    public static List<PrivacySetting> CreateSettings()
    {
        const string dataCollection = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
        const string sqm = @"SOFTWARE\Policies\Microsoft\SQMClient\Windows";
        const string siuf = @"SOFTWARE\Microsoft\Siuf\Rules";
        const string privacy = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy";
        const string advertisingInfo = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
        const string delivery = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        const string explorerAdvanced = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        const string searchSettings = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings";
        const string windowsSearch = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
        const string explorerPolicy = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";
        const string systemPolicy = @"SOFTWARE\Policies\Microsoft\Windows\System";
        const string locationPolicy = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors";
        const string remoteAssistance = @"SYSTEM\CurrentControlSet\Control\Remote Assistance";

        return new List<PrivacySetting>
        {
            new()
            {
                Id = "telemetry-policy",
                Title = "Disable diagnostic data",
                Description = "Sets Windows diagnostic data to the lowest level (Policy AllowTelemetry = 0).",
                Category = Telemetry,
                Hint = "Some Enterprise/Education editions manage this value by group policy.",
                RegistryValues = new[] { new RegistrySpec(true, dataCollection, "AllowTelemetry", 0) }
            },
            new()
            {
                Id = "diagtrack",
                Title = "Disable the telemetry service",
                Description = "Stops and disables the Connected User Experiences and Telemetry service (DiagTrack).",
                Category = Telemetry,
                Hint = "Requires a restart to be fully released.",
                ServiceName = "DiagTrack"
            },
            new()
            {
                Id = "dmwappush",
                Title = "Disable the WAP push service",
                Description = "Stops and disables Device Management WAP Push (dmwappushservice).",
                Category = Telemetry,
                ServiceName = "dmwappushservice"
            },
            new()
            {
                Id = "ceip",
                Title = "Disable the Customer Experience Improvement Program",
                Description = "Turns off CEIP telemetry (Policy CEIPEnable = 0).",
                Category = Telemetry,
                RegistryValues = new[] { new RegistrySpec(true, sqm, "CEIPEnable", 0) }
            },
            new()
            {
                Id = "feedback",
                Title = "Disable feedback requests",
                Description = "Stops Windows from asking for feedback (Siuf NumberOfSIUFInPeriod = 0).",
                Category = Telemetry,
                RegistryValues = new[] { new RegistrySpec(false, siuf, "NumberOfSIUFInPeriod", 0) }
            },
            new()
            {
                Id = "tailored",
                Title = "Disable tailored experiences",
                Description = "Prevents Windows from using diagnostic data to personalise tips and ads.",
                Category = Telemetry,
                RegistryValues = new[]
                {
                    new RegistrySpec(false, privacy, "TailoredExperiencesWithDiagnosticDataEnabled", 0)
                }
            },
            new()
            {
                Id = "wer-service",
                Title = "Disable Windows Error Reporting",
                Description = "Stops and disables the WerSvc service.",
                Category = Telemetry,
                Hint = "Advanced: crash reports will no longer be generated.",
                ServiceName = "WerSvc"
            },

            new()
            {
                Id = "ad-id",
                Title = "Disable the advertising ID",
                Description = "Turns off the Windows advertising identifier used by apps.",
                Category = Advertising,
                RegistryValues = new[] { new RegistrySpec(false, advertisingInfo, "Enabled", 0) }
            },
            new()
            {
                Id = "suggestions",
                Title = "Disable suggestions and tips",
                Description = "Removes app suggestions, tips and lock screen overlays from Windows.",
                Category = Advertising,
                RegistryValues = new[]
                {
                    new RegistrySpec(false, delivery, "SubscribedContent-338388Enabled", 0),
                    new RegistrySpec(false, delivery, "SubscribedContent-338389Enabled", 0),
                    new RegistrySpec(false, delivery, "SubscribedContent-353694Enabled", 0),
                    new RegistrySpec(false, delivery, "SubscribedContent-353696Enabled", 0),
                    new RegistrySpec(false, delivery, "SystemPaneSuggestionsEnabled", 0),
                    new RegistrySpec(false, delivery, "SoftLandingEnabled", 0),
                    new RegistrySpec(false, delivery, "RotatingLockScreenOverlayEnabled", 0)
                }
            },
            new()
            {
                Id = "start-tracking",
                Title = "Disable app launch tracking",
                Description = "Stops Windows from tracking which applications you start to personalise Start.",
                Category = Advertising,
                RegistryValues = new[] { new RegistrySpec(false, explorerAdvanced, "Start_TrackProgs", 0) }
            },
            new()
            {
                Id = "search-highlights",
                Title = "Disable search highlights",
                Description = "Removes rotating highlights and suggestions from the search box.",
                Category = Advertising,
                RegistryValues = new[] { new RegistrySpec(false, searchSettings, "IsDynamicSearchBoxEnabled", 0) }
            },

            new()
            {
                Id = "cortana",
                Title = "Disable Cortana",
                Description = "Blocks the Cortana assistant through policy (AllowCortana = 0).",
                Category = Search,
                RegistryValues = new[] { new RegistrySpec(true, windowsSearch, "AllowCortana", 0) }
            },
            new()
            {
                Id = "bing",
                Title = "Disable web results in Start search",
                Description = "Keeps Start menu searches local (DisableSearchBoxSuggestions = 1).",
                Category = Search,
                RegistryValues = new[] { new RegistrySpec(false, explorerPolicy, "DisableSearchBoxSuggestions", 1) }
            },

            new()
            {
                Id = "activity-history",
                Title = "Disable activity history",
                Description = "Stops Windows from publishing and uploading your activity timeline.",
                Category = Activity,
                RegistryValues = new[]
                {
                    new RegistrySpec(true, systemPolicy, "PublishUserActivities", 0),
                    new RegistrySpec(true, systemPolicy, "UploadUserActivities", 0)
                }
            },
            new()
            {
                Id = "clipboard-sync",
                Title = "Disable clipboard history and sync",
                Description = "Turns off clipboard history and cross-device clipboard synchronization.",
                Category = Activity,
                RegistryValues = new[]
                {
                    new RegistrySpec(true, systemPolicy, "AllowClipboardHistory", 0),
                    new RegistrySpec(true, systemPolicy, "AllowCrossDeviceClipboard", 0)
                }
            },

            new()
            {
                Id = "location",
                Title = "Disable location services",
                Description = "Blocks location access through policy (DisableLocation = 1).",
                Category = Location,
                RegistryValues = new[] { new RegistrySpec(true, locationPolicy, "DisableLocation", 1) }
            },
            new()
            {
                Id = "remote-assistance",
                Title = "Disable Remote Assistance",
                Description = "Blocks unsolicited Remote Assistance invitations (fAllowToGetHelp = 0).",
                Category = Location,
                RegistryValues = new[] { new RegistrySpec(true, remoteAssistance, "fAllowToGetHelp", 0) }
            }
        };
    }

    public static List<PrivacyProfile> CreateProfiles() => new()
    {
        new("Balanced",
            "Disables diagnostic data, telemetry service, advertising, suggestions, search web results, activity history and clipboard sync.",
            new[]
            {
                "telemetry-policy", "diagtrack", "ad-id", "suggestions", "start-tracking",
                "search-highlights", "tailored", "feedback", "bing", "activity-history", "clipboard-sync"
            }),
        new("Strict",
            "Everything in Balanced plus WAP push, CEIP, Cortana and location services.",
            new[]
            {
                "telemetry-policy", "diagtrack", "dmwappush", "ceip", "feedback", "tailored",
                "ad-id", "suggestions", "start-tracking", "search-highlights",
                "cortana", "bing", "activity-history", "clipboard-sync", "location"
            }),
        new("Maximum",
            "All protections, including Windows Error Reporting and Remote Assistance.",
            new[]
            {
                "telemetry-policy", "diagtrack", "dmwappush", "ceip", "feedback", "tailored", "wer-service",
                "ad-id", "suggestions", "start-tracking", "search-highlights",
                "cortana", "bing", "activity-history", "clipboard-sync", "location", "remote-assistance"
            })
    };
}
