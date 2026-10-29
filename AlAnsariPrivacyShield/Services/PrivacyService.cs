using System.Diagnostics;
using System.Globalization;
using AlAnsariPrivacyShield.Models;
using Microsoft.Win32;

namespace AlAnsariPrivacyShield.Services;

public sealed class PrivacyService
{
    private readonly BackupStore _backup;

    public PrivacyService(BackupStore backup) => _backup = backup;

    public bool HasBackup(PrivacySetting setting)
    {
        foreach (var spec in setting.RegistryValues)
        {
            if (_backup.HasValue(RegistryBackupKey(spec))) return true;
        }

        return !string.IsNullOrWhiteSpace(setting.ServiceName) &&
               _backup.HasValue("svc|" + setting.ServiceName);
    }

    public bool IsApplied(PrivacySetting setting)
    {
        foreach (var spec in setting.RegistryValues)
        {
            if (!IsRegistryProtected(spec)) return false;
        }

        if (!string.IsNullOrWhiteSpace(setting.ServiceName) &&
            ReadServiceStart(setting.ServiceName) != 4)
            return false;

        return true;
    }

    public (bool Ok, string Message) Apply(PrivacySetting setting)
    {
        try
        {
            foreach (var spec in setting.RegistryValues)
            {
                _backup.Remember(RegistryBackupKey(spec), ReadRawValue(spec));
                WriteDword(spec, spec.ProtectedValue);
            }

            if (!string.IsNullOrWhiteSpace(setting.ServiceName))
            {
                var service = setting.ServiceName;
                var backupKey = "svc|" + service;
                var start = ReadServiceStart(service);
                _backup.Remember(backupKey, start?.ToString(CultureInfo.InvariantCulture) ?? "absent");

                SetServiceStart(service, 4);
                TrySc($"stop \"{service}\"");
            }

            _backup.Log($"Enabled: {setting.Title}");
            return (true, $"{setting.Title}: applied.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Administrator rights are required.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Ok, string Message) Restore(PrivacySetting setting)
    {
        try
        {
            var touched = false;

            foreach (var spec in setting.RegistryValues)
            {
                var backupKey = RegistryBackupKey(spec);
                var original = _backup.GetValue(backupKey);
                if (original is null) continue;

                if (string.Equals(original, "absent", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteValue(spec);
                }
                else if (int.TryParse(original, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    WriteDword(spec, value);
                }

                touched = true;
            }

            if (!string.IsNullOrWhiteSpace(setting.ServiceName))
            {
                var service = setting.ServiceName;
                var original = _backup.GetValue("svc|" + service);

                if (original is not null)
                {
                    if (int.TryParse(original, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
                    {
                        SetServiceStart(service, start);
                        if (start != 4) TrySc($"start \"{service}\"");
                    }

                    touched = true;
                }
            }

            if (!touched)
                return (false, "No backup exists for this setting.");

            _backup.Log($"Restored: {setting.Title}");
            return (true, $"{setting.Title}: original value restored.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Administrator rights are required.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string RegistryBackupKey(RegistrySpec spec)
        => $"reg|{(spec.Machine ? "HKLM" : "HKCU")}|{spec.KeyPath}|{spec.ValueName}";

    private static RegistryKey BaseKey(bool machine)
        => machine ? Registry.LocalMachine : Registry.CurrentUser;

    private static int? ReadValue(RegistrySpec spec)
    {
        try
        {
            using var baseKey = BaseKey(spec.Machine);
            using var key = baseKey.OpenSubKey(spec.KeyPath);
            var raw = key?.GetValue(spec.ValueName);
            if (raw is null) return null;

            return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static string ReadRawValue(RegistrySpec spec)
    {
        var value = ReadValue(spec);
        return value?.ToString(CultureInfo.InvariantCulture) ?? "absent";
    }

    private static bool IsRegistryProtected(RegistrySpec spec)
        => ReadValue(spec) == spec.ProtectedValue;

    private static void WriteDword(RegistrySpec spec, int value)
    {
        using var baseKey = BaseKey(spec.Machine);
        using var key = baseKey.CreateSubKey(spec.KeyPath, writable: true);
        key.SetValue(spec.ValueName, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(RegistrySpec spec)
    {
        using var baseKey = BaseKey(spec.Machine);
        using var key = baseKey.OpenSubKey(spec.KeyPath, writable: true);
        key?.DeleteValue(spec.ValueName, throwOnMissingValue: false);
    }

    private static int? ReadServiceStart(string service)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{service}");
            var raw = key?.GetValue("Start");
            return raw is null ? null : Convert.ToInt32(raw, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static void SetServiceStart(string service, int value)
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{service}", writable: true);
        key?.SetValue("Start", value, RegistryValueKind.DWord);
    }

    private static void TrySc(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(6000);
        }
        catch
        {
            // Service control is best-effort; the registry value is what matters.
        }
    }
}
