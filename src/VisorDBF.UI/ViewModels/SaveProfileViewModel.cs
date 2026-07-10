using System.Windows.Input;
namespace VisorDBF.UI.ViewModels;

public class SaveProfileViewModel : ViewModelBase
{
    private string _profileName = string.Empty;
    private string? _errorMessage;

    public string ProfileName
    {
        get => _profileName;
        set
        {
            if (SetField(ref _profileName, value))
                Validate();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool CanSave { get; private set; }
    public string? OriginalName { get; }

    public ICommand SaveCommand { get; }

    public SaveProfileViewModel(string? originalName, IReadOnlyList<string> existingProfileNames)
    {
        OriginalName = originalName;
        ProfileName = originalName ?? string.Empty;
        SaveCommand = new RelayCommand(_ => { }, _ => CanSave);
        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ErrorMessage = "El nombre no puede estar vacio.";
            CanSave = false;
            return;
        }

        if (ProfileName.Length > 100)
        {
            ErrorMessage = "El nombre no puede superar los 100 caracteres.";
            CanSave = false;
            return;
        }

        ErrorMessage = null;
        CanSave = true;
    }
}
