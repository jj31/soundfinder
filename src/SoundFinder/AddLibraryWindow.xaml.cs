using System.Windows;
using SoundFinder.Core.Models;

namespace SoundFinder;

public partial class AddLibraryWindow : Window
{
    public Library Library { get; private set; } = new();

    public AddLibraryWindow()
    {
        InitializeComponent();
        TypeBox.Items.Add("Local");
        TypeBox.Items.Add("FTP");
        TypeBox.Items.Add("Cloud");
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(PathBox.Text))
        {
            MessageBox.Show("Le nom et le chemin sont obligatoires.");
            return;
        }
        Library = new Library
        {
            Name = NameBox.Text.Trim(),
            SourceType = TypeBox.SelectedIndex switch { 1 => SourceType.Ftp, 2 => SourceType.Cloud, _ => SourceType.Local },
            RootPath = TypeBox.SelectedIndex == 0 ? PathBox.Text.Trim() : string.Empty,
            SourceUri = TypeBox.SelectedIndex == 0 ? PathBox.Text.Trim() : PathBox.Text.Trim(),
            UserName = UserBox.Text.Trim()
        };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
