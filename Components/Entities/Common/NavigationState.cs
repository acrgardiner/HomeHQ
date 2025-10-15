namespace projectaardvarkx2.Components.Entities.Common;

public class ReturnState
{
    public string? Destination { get; set; }
    public NavigationParameters? Parameters { get; set; }
}

public class NavigationParameters
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }
}

