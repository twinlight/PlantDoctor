using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantDoctor.Models;
using PlantDoctor.Services;
using PlantDoctor.Views;
using System.Collections.ObjectModel;

namespace PlantDoctor.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private ObservableCollection<ScanHistory> _scans = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEmpty = true;

        public bool HasScans => !IsEmpty;

        public HistoryViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        partial void OnIsEmptyChanged(bool value) => OnPropertyChanged(nameof(HasScans));

        [RelayCommand]
        public async Task LoadHistoryAsync()
        {
            IsLoading = true;

            try
            {
                var history = await _databaseService.GetHistoryAsync();
                Scans.Clear();
                foreach (var scan in history)
                    Scans.Add(scan);

                IsEmpty = Scans.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HISTORY] Load error: {ex.Message}");
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlertAsync("Error", $"Could not load history: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task DeleteScanAsync(ScanHistory scan)
        {
            if (scan == null) return;

            await _databaseService.DeleteScanAsync(scan.Id);
            Scans.Remove(scan);
            IsEmpty = Scans.Count == 0;
        }

        [RelayCommand]
        public async Task ClearAllAsync()
        {
            if (Scans.Count == 0) return;

            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Clear History",
                "Are you sure you want to delete all scan history? This cannot be undone.",
                "Delete All",
                "Cancel");

            if (!confirmed) return;

            await _databaseService.ClearHistoryAsync();
            Scans.Clear();
            IsEmpty = true;
        }

        [RelayCommand]
        public async Task ScanNowAsync()
        {
            await Shell.Current.GoToAsync(nameof(CapturePage));
        }
    }
}
