using System.Windows.Input;
using AdoNetCore.AseClient;
using VisorDBF.Core.Models;
namespace VisorDBF.UI.ViewModels;

public class SybaseConnectionViewModel : ViewModelBase
{
    private string _host = string.Empty;
    private int _port = 5000;
    private string _database = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _tableName = string.Empty;
    private string _testMessage = string.Empty;
    private bool _isTestSuccessful;
    private bool _isTesting;

    public string Host
    {
        get => _host;
        set
        {
            if (SetField(ref _host, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (SetField(ref _port, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Database
    {
        get => _database;
        set
        {
            if (SetField(ref _database, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string TableName
    {
        get => _tableName;
        set => SetField(ref _tableName, value);
    }

    public string TestMessage
    {
        get => _testMessage;
        private set => SetField(ref _testMessage, value);
    }

    public bool IsTestSuccessful
    {
        get => _isTestSuccessful;
        private set => SetField(ref _isTestSuccessful, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        private set => SetField(ref _isTesting, value);
    }

    public string ConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database)
                || string.IsNullOrWhiteSpace(Username))
                return string.Empty;
            return $"Data Source={Host}:{Port};Database={Database};User Id={Username};Password=***;";
        }
    }

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }

    private readonly Action<SybaseConnectionConfig> _saveAction;

    public SybaseConnectionViewModel(
        SybaseConnectionConfig? current,
        Action<SybaseConnectionConfig> saveAction)
    {
        _saveAction = saveAction;

        if (current != null)
        {
            _host = current.Host;
            _port = current.Port;
            _database = current.Database;
            _username = current.Username;
            _password = current.Password;
            _tableName = current.TableName;
        }

        TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => !IsTesting);
        SaveCommand = new RelayCommand(_ => Save(), _ => BuildConfig().IsValid);
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestMessage = "Probando conexion...";
        IsTestSuccessful = false;

        try
        {
            var config = BuildConfig();
            var cs = $"Data Source={config.Host}:{config.Port};Database={config.Database};User Id={config.Username};Password={config.Password};";

            await using var connection = new AseConnection(cs);
            await connection.OpenAsync();

            IsTestSuccessful = true;
            TestMessage = "Conexion exitosa.";
        }
        catch (Exception ex)
        {
            IsTestSuccessful = false;
            TestMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private SybaseConnectionConfig BuildConfig()
    {
        return new SybaseConnectionConfig
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            TableName = TableName
        };
    }

    public void SetPasswordFromDialog(string password)
    {
        _password = password;
        OnPropertyChanged(nameof(Password));
    }

    private void Save()
    {
        _saveAction(BuildConfig());
    }
}