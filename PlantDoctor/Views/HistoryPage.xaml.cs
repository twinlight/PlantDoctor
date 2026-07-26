using PlantDoctor.Helpers;
using PlantDoctor.ViewModels;

namespace PlantDoctor.Views
{
    public partial class HistoryPage : ContentPage
    {
        private readonly HistoryViewModel _viewModel;

        public HistoryPage()
        {
            InitializeComponent();
            _viewModel = ServiceHelper.GetRequiredService<HistoryViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        }
    }
}
