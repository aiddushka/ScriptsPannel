using System.Windows;

namespace ScriptsPannel.Views;

public partial class PromptWindow : Window
{
    public string ResultText => Input.Text.Trim();

    public PromptWindow(string title, string hint, string? initialText = null, bool okButtonIsSave = false)
    {
        InitializeComponent();
        Title = title;
        Hint.Text = hint;
        if (!string.IsNullOrEmpty(initialText))
            Input.Text = initialText;
        OkBtn.Content = okButtonIsSave
            ? FindResource("Str.Save")
            : FindResource("Str.Create");
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
