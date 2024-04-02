using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FileExplorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void HandleEvent(object? sender, RoutedEventArgs e)
        {
            ToolTip.SetIsOpen((Control)sender, false);
            ((FileExplorer)DataContext).Navigate(((TextBlock)((StackPanel)((Button)sender).Content).Children[1]).Text);
        }
    }
}