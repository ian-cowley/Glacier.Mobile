namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;
using Xunit;

public class LayoutAndVisualTreeTests
{
    [Fact]
    public void Column_ArrangesChildrenVerticallyWithSpacing()
    {
        var col = new Column(spacing: 10f)
        {
            new Label("Item 1") { Width = 100f, Height = 20f },
            new Label("Item 2") { Width = 100f, Height = 20f },
            new Label("Item 3") { Width = 100f, Height = 20f }
        };

        col.Measure(400f, 800f);
        col.Arrange(0f, 0f, 400f, 800f);

        Assert.Equal(3, col.Children.Count);
        Assert.Equal(0f, col.Children[0].Y);
        Assert.Equal(30f, col.Children[1].Y); // 20 + 10
        Assert.Equal(60f, col.Children[2].Y); // 30 + 20 + 10
        Assert.Equal(80f, col.ActualHeight);  // 3 * 20 + 2 * 10
    }

    [Fact]
    public void Row_ArrangesChildrenHorizontallyWithSpacing()
    {
        var row = new Row(spacing: 15f)
        {
            new Label("Col 1") { Width = 50f, Height = 30f },
            new Label("Col 2") { Width = 50f, Height = 30f }
        };

        row.Measure(400f, 800f);
        row.Arrange(0f, 0f, 400f, 800f);

        Assert.Equal(2, row.Children.Count);
        Assert.Equal(0f, row.Children[0].X);
        Assert.Equal(65f, row.Children[1].X); // 50 + 15
        Assert.Equal(115f, row.ActualWidth); // 50 + 15 + 50
    }

    [Fact]
    public void ScrollView_MeasuresContentAndCalculatesMaxScroll()
    {
        var scroll = new ScrollView
        {
            Width = 300f,
            Height = 400f,
            Content = new Column(spacing: 0f)
            {
                new Label("Long 1") { Width = 300f, Height = 500f },
                new Label("Long 2") { Width = 300f, Height = 500f }
            }
        };

        scroll.Measure(300f, 400f);
        scroll.Arrange(0f, 0f, 300f, 400f);

        Assert.Equal(300f, scroll.ActualWidth);
        Assert.Equal(400f, scroll.ActualHeight);
        Assert.Equal(1000f, scroll.Content.ActualHeight);
        Assert.Equal(600f, scroll.MaxScrollY); // 1000 - 400
    }

    [Fact]
    public void NavigationStack_PushesAndPopsPages()
    {
        var nav = new NavigationStack();
        var page1 = new Screen("Home");
        var page2 = new Screen("Details");

        nav.Push(page1);
        Assert.Equal(1, nav.Depth);
        Assert.Same(page1, nav.CurrentPage);

        nav.Push(page2);
        Assert.Equal(2, nav.Depth);
        Assert.Same(page2, nav.CurrentPage);

        var popped = nav.Pop();
        Assert.Same(page2, popped);
        Assert.Equal(1, nav.Depth);
        Assert.Same(page1, nav.CurrentPage);
    }
}
