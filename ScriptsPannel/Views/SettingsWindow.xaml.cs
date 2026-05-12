using System.Windows;
using ScriptsPannel.ViewModels;

namespace ScriptsPannel.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
