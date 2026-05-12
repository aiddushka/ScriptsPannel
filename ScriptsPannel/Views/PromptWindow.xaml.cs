using System.Windows;

namespace ScriptsPannel.Views;

public partial class PromptWindow : Window
{
    public string ResultText => Input.Text.Trim();

    public PromptWindow(string title, string hint)
    {
        InitializeComponent();
        Title = title;
        Hint.Text = hint;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ResultText))
            return;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
