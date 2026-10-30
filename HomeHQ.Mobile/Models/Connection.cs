namespace HomeHQ.Mobile.Models;

public sealed class Connection
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool Fixed { get; set; }

    public override string ToString() => Name + "|" + Url;

    public string DisplayString => Fixed ? $"{Name} (Default)" : $"{Name} | {Url}";
}
