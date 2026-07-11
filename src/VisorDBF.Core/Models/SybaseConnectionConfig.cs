namespace VisorDBF.Core.Models;

public sealed record SybaseConnectionConfig
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 5000;
    public string Database { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public bool IsValid => !string.IsNullOrWhiteSpace(Host)
        && Port > 0
        && !string.IsNullOrWhiteSpace(Database)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(TableName);

    public SybaseConnectionConfig WithoutPassword() => this with { Password = string.Empty };

    public string BuildConnectionString(bool maskPassword = false) =>
        "DRIVER={Adaptive Server Enterprise};" +
        $"Server={Host};" +
        $"Port={Port};" +
        $"Database={Database};" +
        $"UID={Username};" +
        $"PWD={(maskPassword ? "***" : Password)};" +
        "Connection Timeout=30;";
}