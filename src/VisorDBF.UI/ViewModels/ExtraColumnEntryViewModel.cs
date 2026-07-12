using VisorDBF.Core.Models;

namespace VisorDBF.UI.ViewModels;

public sealed class ExtraColumnEntryViewModel : ViewModelBase
{
    private string _columnName = string.Empty;
    private ExtraColumnType _type;
    private string _rawValue = string.Empty;

    public string ColumnName
    {
        get => _columnName;
        set
        {
            if (SetField(ref _columnName, value))
                OnPropertyChanged(nameof(HasErrors));
        }
    }

    public ExtraColumnType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
                OnPropertyChanged(nameof(HasErrors));
        }
    }

    public string RawValue
    {
        get => _rawValue;
        set
        {
            if (SetField(ref _rawValue, value))
                OnPropertyChanged(nameof(HasErrors));
        }
    }

    public bool HasErrors =>
        string.IsNullOrWhiteSpace(ColumnName) ||
        string.IsNullOrWhiteSpace(RawValue);
}
