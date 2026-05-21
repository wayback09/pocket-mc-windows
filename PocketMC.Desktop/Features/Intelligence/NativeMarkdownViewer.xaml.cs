using System;
using System.Windows;
using System.Windows.Controls;
using PocketMC.Desktop.Infrastructure;

namespace PocketMC.Desktop.Features.Intelligence
{
    /// <summary>
    /// WPF-native markdown viewer using FlowDocumentScrollViewer.
    /// No WebView2, no external browser processes, zero memory overhead.
    /// Works reliably in any WPF container state (Collapsed, Visible, Tab, etc.)
    /// </summary>
    public partial class NativeMarkdownViewer : UserControl
    {
        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(NativeMarkdownViewer),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        public NativeMarkdownViewer()
        {
            InitializeComponent();
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeMarkdownViewer viewer)
            {
                viewer.RenderMarkdown(e.NewValue as string ?? string.Empty);
            }
        }

        private void RenderMarkdown(string rawMarkdown)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawMarkdown))
                {
                    DocumentViewer.Document = null;
                    return;
                }

                bool isDarkMode = true;
                try
                {
                    isDarkMode = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme()
                                 == Wpf.Ui.Appearance.ApplicationTheme.Dark;
                }
                catch
                {
                    // Fallback to dark
                }

                var doc = MarkdownFlowDocumentConverter.Convert(rawMarkdown, isDarkMode);
                DocumentViewer.Document = doc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NativeMarkdownViewer render error: {ex.Message}");
            }
        }
    }
}
