using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace ToDoApp.WinUI.Controls;

/// <summary>
/// Simple wrap panel for toolbar / filter bar responsive layout.
/// </summary>
public class WrapPanel : Panel
{
    public static readonly DependencyProperty ItemSpacingProperty =
        DependencyProperty.Register(
            nameof(ItemSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public static readonly DependencyProperty LineSpacingProperty =
        DependencyProperty.Register(
            nameof(LineSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    public double LineSpacing
    {
        get => (double)GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var spacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        double x = 0;
        double y = 0;
        double lineHeight = 0;
        double maxWidth = 0;

        foreach (UIElement child in Children)
        {
            child.Measure(availableSize);
            var size = child.DesiredSize;

            if (x > 0 && x + size.Width > availableSize.Width && !double.IsInfinity(availableSize.Width))
            {
                maxWidth = Math.Max(maxWidth, x - spacing);
                x = 0;
                y += lineHeight + lineSpacing;
                lineHeight = 0;
            }

            lineHeight = Math.Max(lineHeight, size.Height);
            x += size.Width + spacing;
        }

        maxWidth = Math.Max(maxWidth, Math.Max(0, x - spacing));
        return new Size(maxWidth, y + lineHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        double x = 0;
        double y = 0;
        double lineHeight = 0;

        foreach (UIElement child in Children)
        {
            var size = child.DesiredSize;

            if (x > 0 && x + size.Width > finalSize.Width)
            {
                x = 0;
                y += lineHeight + lineSpacing;
                lineHeight = 0;
            }

            child.Arrange(new Rect(x, y, size.Width, size.Height));
            lineHeight = Math.Max(lineHeight, size.Height);
            x += size.Width + spacing;
        }

        return finalSize;
    }
}
