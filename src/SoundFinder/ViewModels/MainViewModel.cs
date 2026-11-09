using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SoundFinder.Core.Metadata;
using SoundFinder.Core.Models;
using SoundFinder.Core.Search;
using SoundFinder.Services;

namespace SoundFinder.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Library> Libraries { get; } = new();
    public ObservableCollection<SampleFile> Results { get; } = new();

    private string _searchQuery = string.Empty;
    private Library? _selectedLibrary;
    private SampleFile? _selectedSample;
    private string _minDuration = string.Empty;
    private string _maxDuration = string.Empty;
    private string _tagFilter = string.Empty;
    private bool _isPlaying;
    private bool _loop;
    private string _status = "Prêt";

    public MainViewModel()
    {
        SearchCommand = new RelayCommand(_ => RunSearch());
        RefreshLibrariesCommand = new RelayCommand(_ => RefreshLibraries());
        IndexLibraryCommand = new RelayCommand(_ => IndexSelectedLibrary(), _ => SelectedLibrary is not null);
        PlayCommand = new RelayCommand(_ => PlaySelected(), _ => SelectedSample is not null && !IsPlaying);
        StopCommand = new RelayCommand(_ => StopPlayback(), _ => IsPlaying);
        SaveMetadataCommand = new RelayCommand(_ => SaveMetadata(), _ => SelectedSample is not null);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand RefreshLibrariesCommand { get; }
    public RelayCommand IndexLibraryCommand { get; }
    public RelayCommand PlayCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand SaveMetadataCommand { get; }

    public string SearchQuery { get => _searchQuery; set => Set(ref _searchQuery, value); }
    public Library? SelectedLibrary { get => _selectedLibrary; set { Set(ref _selectedLibrary, value); CommandManager_Invalidate(); } }
    public SampleFile? SelectedSample { get => _selectedSample; set => Set(ref _selectedSample, value); }
    public string MinDuration { get => _minDuration; set => Set(ref _minDuration, value); }
    public string MaxDuration { get => _maxDuration; set => Set(ref _maxDuration, value); }
    public string TagFilter { get => _tagFilter; set => Set(ref _tagFilter, value); }
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); CommandManager_Invalidate(); } }
    public bool Loop { get => _loop; set => Set(ref _loop, value); }
    public string Status { get => _status; set => Set(ref _status, value); }

    public void RefreshLibraries()
    {
        Libraries.Clear();
        foreach (var library in AppServices.Libraries.GetAll())
        {
            Libraries.Add(library);
        }
        Status = $"{Libraries.Count} bibliothèque(s).";
    }

    public void RunSearch()
    {
        var filter = new SearchFilter
        {
            Query = SearchQuery,
            LibraryId = SelectedLibrary?.Id,
            TagContains = string.IsNullOrWhiteSpace(TagFilter) ? null : TagFilter,
            MinDurationSeconds = double.TryParse(MinDuration, out var min) ? min : null,
            MaxDurationSeconds = double.TryParse(MaxDuration, out var max) ? max : null,
            Limit = 500
        };
        var result = AppServices.Search.Search(filter);
        Results.Clear();
        foreach (var item in result.Items)
        {
            Results.Add(item);
        }
        Status = $"{result.Total} résultat(s).";
    }

    public void IndexSelectedLibrary()
    {
        if (SelectedLibrary is null)
        {
            return;
        }
        Status = $"Indexation de « {SelectedLibrary.Name} »…";
        Task.Run(() =>
        {
            try
            {
                var report = AppServices.Indexer.IndexLibrary(SelectedLibrary.Id);
                Status = $"Indexation terminée : {report.Added} ajoutés, {report.Updated} mis à jour, {report.Removed} supprimés, {report.Errors} erreur(s).";
            }
            catch (Exception ex)
            {
                Status = "Erreur d'indexation : " + ex.Message;
            }
        });
    }

    public void PlaySelected()
    {
        if (SelectedSample is null)
        {
            return;
        }
        try
        {
            var path = AppServices.ResolveFullPath(SelectedSample);
            Player.Play(path);
            Player.SetLoop(Loop);
            IsPlaying = true;
            Status = "Lecture : " + SelectedSample.FileName;
        }
        catch (Exception ex)
        {
            Status = "Erreur de lecture : " + ex.Message;
        }
    }

    public void StopPlayback()
    {
        Player.Stop();
        IsPlaying = false;
        Status = "Arrêt.";
    }

    public void SaveMetadata(bool writeToFile)
    {
        if (SelectedSample is null)
        {
            return;
        }
        var patch = new MetadataPatch
        {
            Tags = SelectedSample.Tags,
            Bpm = SelectedSample.Bpm,
            MusicalKey = SelectedSample.MusicalKey,
            Comment = SelectedSample.Comment
        };
        var updated = AppServices.Metadata.Update(SelectedSample.Id, patch, writeToFile);
        if (updated is not null)
        {
            SelectedSample = updated;
            Status = "Métadonnées enregistrées.";
        }
    }

    public Audio.PreviewPlayer Player { get; } = new Audio.PreviewPlayer();

    private void CommandManager_Invalidate() => System.Windows.Input.CommandManager.InvalidateRequerySuggested();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
