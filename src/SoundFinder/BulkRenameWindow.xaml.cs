using System.Collections.ObjectModel;
using System.Windows;
using SoundFinder.Core.Models;
using SoundFinder.Core.Rename;
using SoundFinder.Core.Sources;
using SoundFinder.Services;

namespace SoundFinder;

public partial class BulkRenameWindow : Window
{
    private readonly List<long> _sampleIds;
    private readonly ObservableCollection<RenamePreviewItem> _previews = new();

    public BulkRenameWindow(List<long> sampleIds)
    {
        _sampleIds = sampleIds;
        InitializeComponent();
        PreviewList.ItemsSource = _previews;
    }

    private void OnPreview(object sender, RoutedEventArgs e)
    {
        _previews.Clear();
        var request = new RenameRequest { SampleIds = _sampleIds, Pattern = PatternBox.Text, OnlyPreview = true };
        foreach (var item in AppServices.BulkRename.Preview(request))
        {
            _previews.Add(item);
        }
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        var request = new RenameRequest { SampleIds = _sampleIds, Pattern = PatternBox.Text, OnlyPreview = false };
        var applied = AppServices.BulkRename.Apply(request, MoveFile);
        _previews.Clear();
        foreach (var item in applied)
        {
            _previews.Add(item);
        }
        var errors = applied.Count(a => a.Error is not null);
        MessageBox.Show($"{applied.Count - errors} fichier(s) renommé(s), {errors} erreur(s).");
    }

    private static bool MoveFile(SampleFile sample, string newRelativePath)
    {
        var library = AppServices.Libraries.GetById(sample.LibraryId);
        if (library is null || library.SourceType != SourceType.Local)
        {
            return false;
        }
        var source = new LocalFileSource(library.RootPath);
        var oldFull = source.GetFullPath(sample.Path);
        var newFull = source.GetFullPath(newRelativePath);
        if (!File.Exists(oldFull))
        {
            return false;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(newFull)!);
        if (File.Exists(newFull) && !string.Equals(oldFull, newFull, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        File.Move(oldFull, newFull);
        return true;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
