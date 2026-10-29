using AlAnsariPrivacyShield.Infrastructure;

namespace AlAnsariPrivacyShield.Models;

public sealed class CategorySummary : ObservableObject
{
    private int _applied;
    private int _total;

    public CategorySummary(string name) => Name = name;

    public string Name { get; }

    public int Applied
    {
        get => _applied;
        set
        {
            if (!SetProperty(ref _applied, value)) return;
            OnPropertyChanged(nameof(Detail));
            OnPropertyChanged(nameof(Percent));
        }
    }

    public int Total
    {
        get => _total;
        set
        {
            if (!SetProperty(ref _total, value)) return;
            OnPropertyChanged(nameof(Detail));
            OnPropertyChanged(nameof(Percent));
        }
    }

    public string Detail => $"{Applied} / {Total}";
    public double Percent => Total == 0 ? 0 : Applied * 100.0 / Total;
}
