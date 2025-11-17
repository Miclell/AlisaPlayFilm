namespace Tray.App.Models;

public class AppState
{
    private bool _isRunning;

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value) return;

            _isRunning = value;
            StatusChanged?.Invoke();
        }
    }

    public string Status => IsRunning ? "Server: Running" : "Server: Stopped";
    public DateTime StartTime { get; set; }
    public string? LastError { get; set; }
    public int ErrorCount { get; set; }
    public string? ServerUrl { get; set; }

    public event Action? StatusChanged;
}