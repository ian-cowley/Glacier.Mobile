namespace Glacier.Mobile.UI.Controls;

/// <summary>
/// Styled title/header label for section dividers and page titles.
/// </summary>
public sealed class Header : Label
{
    public Header(string title) : base(title)
    {
        FontSize = 22f;
        IsBold = true;
        TextColor = Color4.GlacierBlue;
        Margin = new Thickness(0f, 4f, 0f, 8f);
    }
}
