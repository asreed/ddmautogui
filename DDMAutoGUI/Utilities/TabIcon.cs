using System.Windows;
using System.Windows.Media;

namespace DDMAutoGUI.Utilities
{
    /// <summary>
    /// Attached property for associating an icon with a <see cref="System.Windows.Controls.TabItem"/>
    /// so the MainTab control template can display it. Strongly typed as
    /// <see cref="ImageSource"/> so the XAML designer resolves it at design time.
    /// </summary>
    public static class TabIcon
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached(
                "Source",
                typeof(ImageSource),
                typeof(TabIcon),
                new FrameworkPropertyMetadata(null));

        public static ImageSource GetSource(DependencyObject obj)
            => (ImageSource)obj.GetValue(SourceProperty);

        public static void SetSource(DependencyObject obj, ImageSource value)
            => obj.SetValue(SourceProperty, value);
    }
}