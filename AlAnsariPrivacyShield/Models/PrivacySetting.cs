using AlAnsariPrivacyShield.Infrastructure;

namespace AlAnsariPrivacyShield.Models;

public sealed record RegistrySpec(bool Machine, string KeyPath, string ValueName, int ProtectedValue);

public sealed class PrivacySetting : ObservableObject
{
    private bool _isProtected;
    private bool _isApplied;

    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public string? Hint { get; init; }
    public IReadOnlyList<RegistrySpec> RegistryValues { get; init; } = Array.Empty<RegistrySpec>();
    public string? ServiceName { get; init; }

    public bool IsProtected
    {
        get => _isProtected;
        set
        {
            if (!SetProperty(ref _isProtected, value)) return;
            OnPropertyChanged(nameof(IsPending));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool IsApplied
    {
        get => _isApplied;
        private set
        {
            if (!SetProperty(ref _isApplied, value)) return;
            OnPropertyChanged(nameof(IsPending));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool IsPending => IsProtected != IsApplied;

    public string StatusText => IsPending
        ? (IsProtected ? "Pending: enable" : "Pending: disable")
        : (IsApplied ? "Active" : "Not applied");

    public void SetApplied(bool applied) => IsApplied = applied;

    public void SyncDesiredToApplied() => IsProtected = IsApplied;
}
