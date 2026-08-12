using MudBlazor;
using System.Text.Json;

namespace HomeHQ.Server.Components.Admin;

public partial class LogViewer
{
    private List<string> _logFiles = new();
    private readonly List<LogEntry> _allEntries = new();
    private List<LogEntry> _filteredEntries = new();
    private readonly List<string> _availableLevels = [
        "All",
        Serilog.Events.LogEventLevel.Verbose.ToString(),
        Serilog.Events.LogEventLevel.Debug.ToString(),
        Serilog.Events.LogEventLevel.Information.ToString(),
        Serilog.Events.LogEventLevel.Warning.ToString(),
        Serilog.Events.LogEventLevel.Error.ToString(),
        Serilog.Events.LogEventLevel.Fatal.ToString()
    ];
    private string _selectedLevel = "All";
    private string _selectedLogFile = string.Empty;

    private string _searchString = string.Empty;

    private const int ROWS_PER_PAGE = 100;

    private class LogEntry
    {
        public string Timestamp { get; set; } = "";
        public string Level { get; set; } = "";
        public string SourceContext { get; set; } = "";
        public string Message { get; set; } = "";
        public string Exception { get; set; } = "";
    }

    protected override async Task OnInitializedAsync()
    {
        // Get the latest log file
        var logDirectory = Path.Combine("appdata", "logs");

        _logFiles = Directory.GetFiles(logDirectory, "log-*.json").OrderByDescending(f => f).Select(s => Path.GetFileName(s)).ToList();

        _selectedLogFile = _logFiles.FirstOrDefault()!;

        await OnLogFileChange();

    }

    private async Task OnLevelChanged()
    {
        ApplyFilter();
    }

    private async Task OnLogFileChange()
    {
        //Get Data from selected log file
        if (_selectedLogFile != null)
        {
            try
            {
                _allEntries.Clear();

                var file = Path.Combine("appdata", "logs", _selectedLogFile);
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    var entry = new LogEntry
                    {
                        Timestamp = root.GetProperty("Timestamp").GetString() ?? "",
                        Level = root.GetProperty("Level").GetString() ?? "",
                        SourceContext = root.GetProperty("SourceContext").GetString() ?? "",
                        Message = root.GetProperty("Message").GetString() ?? "",
                        Exception = root.TryGetProperty("Exception", out var exProp) ? exProp.GetString() ?? "" : ""
                    };

                    _allEntries.Add(entry);
                }

                //reverse to show latest first
                _allEntries.Reverse();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _allEntries.Add(new LogEntry
                {
                    Timestamp = "",
                    Level = "Error",
                    SourceContext = "",
                    Message = $"Failed to read or parse log file: {ex.Message}",
                    Exception = ex.ToString()
                });
                ApplyFilter();
            }
        }
    }

    private void ApplyFilter()
    {
        _filteredEntries = _selectedLevel == "All"
            ? _allEntries
            : _allEntries.Where(e => e.Level == _selectedLevel).ToList();
    }

    private Func<LogEntry, bool> QuickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
        {
            return true;
        }

        if (x.Message.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (x.SourceContext.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    };
}
