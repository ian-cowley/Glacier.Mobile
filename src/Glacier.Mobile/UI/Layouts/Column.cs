namespace Glacier.Mobile.UI.Layouts;

/// <summary>
/// Vertical declarative stack container with support for collection initializers.
/// </summary>
public class Column : StackLayout
{
    public Column(float spacing = 8f)
    {
        Orientation = StackOrientation.Vertical;
        Spacing = spacing;
    }
}
