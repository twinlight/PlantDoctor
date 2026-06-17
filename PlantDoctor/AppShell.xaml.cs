namespace PlantDoctor
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Views.CapturePage), typeof(Views.CapturePage));
            Routing.RegisterRoute(nameof(Views.ResultPage), typeof(Views.ResultPage));
            Routing.RegisterRoute(nameof(Views.HistoryPage), typeof(Views.HistoryPage));
        }
    }
}