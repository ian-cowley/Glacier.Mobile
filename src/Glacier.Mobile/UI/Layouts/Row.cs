namespace Glacier.Mobile.UI.Layouts;

/// <summary>
/// Horizontal declarative stack container with support for collection initializers.
/// </summary>
public class Row : StackLayout
{
    public Row(float spacing = 8f)
    {
        Orientation = StackOrientation.Horizontal;
        Spacing = spacing;
    }
}
