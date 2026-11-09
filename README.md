# SoundFinder

Application Windows (WPF / .NET 8) de recherche et de prévisualisation dans des bibliothèques de samples audio, inspirée des outils de type SoundMiner.

## Fonctionnalités

- **Plusieurs bibliothèques** indexées simultanément (dossiers locaux, FTP, cloud en plug-in)
- **Indexation incrémentale** en SQLite (FTS5) : nom, durée, sample rate, canaux, format, tags lus dans les fichiers audio
- **Recherche full-text** sur nom, tags et commentaires, avec filtres (bibliothèque, durée min/max, tag, BPM)
- **Prévisualisation audio** (lecture, stop, boucle) avec forme d'onde zoomable et spectre **FFT** en temps réel
- **Métadonnées éditables** (tags, BPM, tonalité, commentaire), avec écriture optionnelle dans les tags du fichier
- **Renommage en masse** par pattern (`{name}`, `{tags}`, `{bpm}`, `{key}`, `{comment}`, `{ext}`…) avec prévisualisation et détection de collisions
- **Sources externes** : FTP intégré ; cloud (Dropbox/Drive/OneDrive) extensible via `ISampleSource`
- **IA locale** : aucune fonctionnalité d'IA n'est incluse dans cette version (désactivée par choix)

## Architecture

- `src/SoundFinder.Core` (net8.0, testable multiplateforme) : modèles, base SQLite, indexation, recherche, métadonnées, renommage, FFT/waveform, sources de fichiers
- `src/SoundFinder` (net8.0-windows, WPF) : interface, lecture NAudio, contrôles waveform/spectre
- `tests/SoundFinder.Core.Tests` : tests xUnit du core

## Build et test

```powershell
dotnet restore SoundFinder.sln
dotnet build SoundFinder.sln -c Release
dotnet test SoundFinder.sln -c Release
```

Un workflow GitHub Actions (`.github/workflows/build.yml`, runner `windows-latest`) compile et teste à chaque push/PR.

## Base de données

Stockée dans `%AppData%\SoundFinder\soundfinder.db`.
