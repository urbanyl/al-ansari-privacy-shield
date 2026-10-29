using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using AlAnsariPrivacyShield.Infrastructure;
using AlAnsariPrivacyShield.Models;
using AlAnsariPrivacyShield.Services;
using MessageBox = System.Windows.MessageBox;

namespace AlAnsariPrivacyShield.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly BackupStore _backup = new();
    private readonly PrivacyService _service;

    private PrivacyProfile? _selectedProfile;
    private string _statusMessage = "Ready.";
    private bool _statusIsError;
    private string _logText = string.Empty;
    private string _backupSummary = string.Empty;
    private bool _isInitialized;

    public MainViewModel()
    {
        _service = new PrivacyService(_backup);

        Settings = new ObservableCollection<PrivacySetting>(PrivacyCatalog.CreateSettings());
        Profiles = new ObservableCollection<PrivacyProfile>(PrivacyCatalog.CreateProfiles());
        _selectedProfile = Profiles[0];

        foreach (var setting in Settings)
        {
            setting.PropertyChanged += OnSettingPropertyChanged;
        }

        SettingsView = CollectionViewSource.GetDefaultView(Settings);
        SettingsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PrivacySetting.Category)));

        foreach (var category in Settings.Select(s => s.Category).Distinct())
            Categories.Add(new CategorySummary(category));

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ApplyCommand = new AsyncRelayCommand(ApplyChangesAsync);
        RestoreAllCommand = new AsyncRelayCommand(RestoreAllAsync);
        ApplyProfileCommand = new RelayCommand(LoadProfile);
        ProtectAllCommand = new RelayCommand(SelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        ExportReportCommand = new RelayCommand(ExportReport);
        OpenFolderCommand = new RelayCommand(OpenFolder);

        RefreshStates(syncDesired: true);
        RefreshBackupInfo();
    }

    public ObservableCollection<PrivacySetting> Settings { get; }
    public ICollectionView SettingsView { get; }
    public ObservableCollection<PrivacyProfile> Profiles { get; }
    public ObservableCollection<CategorySummary> Categories { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ApplyCommand { get; }
    public AsyncRelayCommand RestoreAllCommand { get; }
    public RelayCommand ApplyProfileCommand { get; }
    public RelayCommand ProtectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand ExportReportCommand { get; }
    public RelayCommand OpenFolderCommand { get; }

    public PrivacyProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value)) return;
            OnPropertyChanged(nameof(ProfileDescription));
        }
    }

    public string ProfileDescription => SelectedProfile?.Description ?? string.Empty;

    public double Score => Settings.Count == 0
        ? 0
        : Settings.Count(s => s.IsApplied) * 100.0 / Settings.Count;

    public string ScoreText => $"{Score:0}%";

    public int ActiveCount => Settings.Count(s => s.IsApplied);
    public int TotalCount => Settings.Count;
    public int PendingCount => Settings.Count(s => s.IsPending);

    public string ScoreDetail => $"{ActiveCount} of {TotalCount} protections active";
    public string PendingText => PendingCount == 0 ? "No pending change" : $"{PendingCount} pending change(s)";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool StatusIsError
    {
        get => _statusIsError;
        private set => SetProperty(ref _statusIsError, value);
    }

    public string BackupSummary
    {
        get => _backupSummary;
        private set => SetProperty(ref _backupSummary, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public string BackupFolder => _backup.DirectoryPath;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        await Task.CompletedTask;
    }

    private void OnSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PrivacySetting.IsProtected) or nameof(PrivacySetting.IsApplied))
            RefreshCounters();
    }

    private void RefreshCounters()
    {
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(ScoreText));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ScoreDetail));
        OnPropertyChanged(nameof(PendingText));

        foreach (var category in Categories)
        {
            var items = Settings.Where(s => s.Category == category.Name).ToList();
            category.Applied = items.Count(s => s.IsApplied);
            category.Total = items.Count;
        }

        BackupSummary =
            $"{_backup.Data.Values.Count} original value(s) saved  ·  " +
            $"{ActiveCount} of {TotalCount} protections active  ·  " +
            $"{PendingCount} pending change(s)";
    }

    private void RefreshStates(bool syncDesired)
    {
        foreach (var setting in Settings)
        {
            setting.SetApplied(_service.IsApplied(setting));
            if (syncDesired) setting.SyncDesiredToApplied();
        }

        RefreshCounters();
    }

    private async Task RefreshAsync()
    {
        await Task.Run(() => { });
        RefreshStates(syncDesired: true);
        RefreshBackupInfo();
        SetStatus("State refreshed from Windows.");
    }

    private void RefreshBackupInfo()
    {
        LogText = _backup.ReadLog();
        RefreshCounters();
    }

    private async Task ApplyChangesAsync()
    {
        var pending = Settings.Where(s => s.IsPending).ToList();
        if (pending.Count == 0)
        {
            SetStatus("No pending change to apply.");
            return;
        }

        SetStatus($"Applying {pending.Count} change(s)…");

        var results = await Task.Run(() => pending
            .Select(s => s.IsProtected ? _service.Apply(s) : _service.Restore(s))
            .ToList());

        var failures = results.Count(r => !r.Ok);
        var firstError = results.FirstOrDefault(r => !r.Ok).Message;

        RefreshStates(syncDesired: true);
        RefreshBackupInfo();

        SetStatus(failures == 0
            ? $"{pending.Count} change(s) applied."
            : $"{failures} change(s) failed. {firstError}",
            failures > 0);

        _backup.Log($"Apply changes: {pending.Count} requested, {failures} failed.");
    }

    private async Task RestoreAllAsync()
    {
        var answer = MessageBox.Show(
            "Restore every original value saved by Privacy Shield?\n\n" +
            "All protections will be turned off and Windows settings will return to their previous state.",
            "Al-Ansari Privacy Shield",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes) return;

        var targets = Settings.Where(_service.HasBackup).ToList();
        if (targets.Count == 0)
        {
            SetStatus("No backup exists yet — there is nothing to restore.");
            return;
        }

        SetStatus($"Restoring {targets.Count} setting(s)…");

        var results = await Task.Run(() => targets.Select(s => _service.Restore(s)).ToList());
        var failures = results.Count(r => !r.Ok);

        RefreshStates(syncDesired: true);
        RefreshBackupInfo();

        SetStatus(failures == 0
            ? $"All original values restored ({targets.Count} setting(s))."
            : $"{failures} of {targets.Count} setting(s) could not be restored (access denied).",
            failures > 0);

        _backup.Log("Restore all requested.");
    }

    private void LoadProfile()
    {
        if (SelectedProfile is not { } profile) return;

        var enabled = new HashSet<string>(profile.EnabledIds, StringComparer.OrdinalIgnoreCase);
        foreach (var setting in Settings)
            setting.IsProtected = enabled.Contains(setting.Id);

        RefreshCounters();
        SetStatus($"Profile \"{profile.Label}\" loaded — {PendingCount} pending change(s). Click Apply changes.");
    }

    private void SelectAll()
    {
        foreach (var setting in Settings) setting.IsProtected = true;
        RefreshCounters();
        SetStatus("All protections selected. Click Apply changes.");
    }

    private void ClearSelection()
    {
        foreach (var setting in Settings) setting.IsProtected = false;
        RefreshCounters();
        SetStatus("All protections unselected. Applying now restores each original value.");
    }

    private void ExportReport()
    {
        try
        {
            Directory.CreateDirectory(_backup.DirectoryPath);

            var builder = new StringBuilder();
            builder.AppendLine("Al-Ansari Privacy Shield — privacy report");
            builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine(new string('-', 64));
            builder.AppendLine($"Score        : {ScoreText}");
            builder.AppendLine($"Active       : {ActiveCount} / {TotalCount}");
            builder.AppendLine($"Pending      : {PendingCount}");
            builder.AppendLine(new string('-', 64));

            foreach (var group in Settings.GroupBy(s => s.Category))
            {
                builder.AppendLine(group.Key + ":");
                foreach (var setting in group)
                {
                    builder.AppendLine(
                        $"  [{(setting.IsApplied ? "x" : " ")}] {setting.Title} — {setting.StatusText}");
                }
                builder.AppendLine();
            }

            builder.AppendLine(new string('-', 64));
            builder.AppendLine("Recent log:");
            builder.AppendLine(_backup.ReadLog());

            var file = Path.Combine(_backup.DirectoryPath, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(file, builder.ToString());
            SetStatus("Report exported: " + file);
        }
        catch (Exception ex)
        {
            SetStatus("Unable to export the report: " + ex.Message, isError: true);
        }
    }

    private void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(_backup.DirectoryPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_backup.DirectoryPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetStatus("Unable to open the folder: " + ex.Message, isError: true);
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
