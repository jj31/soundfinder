using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SoundFinder.Core.Audio;
using SoundFinder.Core.Models;
using SoundFinder.Services;
using SoundFinder.ViewModels;

namespace SoundFinder;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly DispatcherTimer _spectrumTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.RefreshLibraries();
        _viewModel.RunSearch();

        Waveform.SeekRequested += fraction =>
        {
            if (_viewModel.Player.IsPlaying)
            {
                _viewModel.Status = $"Position : {fraction * _viewModel.Player.Duration.TotalSeconds:0.00}s";
            }
        };

        _viewModel.Player.PlaybackStopped += (_, _) =>
        {
            _viewModel.IsPlaying = false;
            Waveform.PlayheadFraction = 0;
        };

        _spectrumTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _spectrumTimer.Tick += (_, _) => UpdateSpectrum();
        _spectrumTimer.Start();
    }

    private void UpdateSpectrum()
    {
        if (!_viewModel.Player.IsPlaying)
        {
            return;
        }
        var samples = _viewModel.Player.ReadLatestSamples(2048);
        if (samples.Length >= 512)
        {
            Spectrum.Magnitudes = Fft.Magnitudes(samples, 2048);
        }
        Waveform.PlayheadFraction = _viewModel.Player.Duration.TotalSeconds > 0
            ? _viewModel.Player.Position.TotalSeconds / _viewModel.Player.Duration.TotalSeconds
            : 0;
    }

    protected override void OnClosed(EventArgs e)
    {
        _spectrumTimer.Stop();
        _viewModel.StopPlayback();
        _viewModel.Player.Dispose();
        base.OnClosed(e);
    }

    private void OnAddLibrary(object sender, RoutedEventArgs e)
    {
        var dialog = new AddLibraryWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            AppServices.Libraries.Add(dialog.Library);
            _viewModel.RefreshLibraries();
        }
    }

    private void OnRemoveLibrary(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedLibrary is { } library &&
            MessageBox.Show($"Supprimer la bibliothèque « {library.Name} » et ses samples indexés ?",
                "Confirmer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            AppServices.Libraries.Delete(library.Id);
            _viewModel.RefreshLibraries();
        }
    }

    private async void OnBulkRename(object sender, RoutedEventArgs e)
    {
        var selected = ResultsGrid.SelectedItems.Cast<SampleFile>().Select(s => s.Id).ToList();
        if (selected.Count == 0 && _viewModel.SelectedSample is not null)
        {
            selected.Add(_viewModel.SelectedSample.Id);
        }
        if (selected.Count == 0)
        {
            MessageBox.Show("Sélectionnez d'abord des samples dans la liste.");
            return;
        }
        var dialog = new BulkRenameWindow(selected) { Owner = this };
        dialog.ShowDialog();
        _viewModel.RunSearch();
    }

    private void OnSaveMetadata(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveMetadata(WriteTagsToFile.IsChecked == true);
    }

    private async void ResultsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.SelectedSample is not { } sample)
        {
            return;
        }
        try
        {
            var path = AppServices.ResolveFullPath(sample);
            var width = Math.Max(200, (int)Waveform.ActualWidth);
            var peaks = await Task.Run(() => WaveformLoader.LoadPeaks(path, width));
            Waveform.Peaks = peaks;
        }
        catch (Exception ex)
        {
            _viewModel.Status = "Waveform indisponible : " + ex.Message;
        }
    }
}
