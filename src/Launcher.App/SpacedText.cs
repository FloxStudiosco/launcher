using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FloxStudios.Launcher.App;

public sealed class SpacedText : FrameworkElement
{
    private const FrameworkPropertyMetadataOptions LAYOUT =
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender;

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SpacedText), new FrameworkPropertyMetadata("", LAYOUT));

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(SpacedText), new FrameworkPropertyMetadata(0.0, LAYOUT));

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(SpacedText), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, LAYOUT | FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(SpacedText), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, LAYOUT | FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
        typeof(SpacedText), new FrameworkPropertyMetadata(FontWeights.Normal, LAYOUT | FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(SpacedText), new FrameworkPropertyMetadata(Brushes.Black,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    private readonly List<FormattedText> _glyphs = new List<FormattedText>();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        BuildGlyphs();
        double width = 0;
        for (int i = 0; i < _glyphs.Count; i++)
        {
            width += _glyphs[i].WidthIncludingTrailingWhitespace;
            if (i < _glyphs.Count - 1)
            {
                width += Spacing;
            }
        }
        return new Size(width, FontFamily.LineSpacing * FontSize);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        BuildGlyphs();
        double x = 0;
        foreach (FormattedText glyph in _glyphs)
        {
            drawingContext.DrawText(glyph, new Point(x, 0));
            x += glyph.WidthIncludingTrailingWhitespace + Spacing;
        }
    }

    private void BuildGlyphs()
    {
        _glyphs.Clear();
        string text = Text ?? "";
        var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal);
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        foreach (char symbol in text)
        {
            _glyphs.Add(new FormattedText(symbol.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                typeface, FontSize, Foreground, pixelsPerDip));
        }
    }
}
