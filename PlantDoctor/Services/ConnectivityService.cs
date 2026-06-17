namespace PlantDoctor.Services
{
    public class ConnectivityService
    {
        public bool IsConnected =>
            Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}