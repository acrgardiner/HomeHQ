namespace HomeHQ.Mobile.Models;

/// <summary>
/// Display model for Setup list rows (Categories, Warranty Types, Attachment Types).
/// </summary>
public sealed class SetupListItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? LeadingText { get; init; }
    public string? Subtitle { get; init; }
    public bool ShowLeading => !string.IsNullOrWhiteSpace(LeadingText);
    public bool ShowSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
}
