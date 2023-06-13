using MonkeyFinder.Services;

namespace MonkeyFinder.ViewModel;

public partial class MonkeysViewModel : BaseViewModel
{
    MonkeyService _monkeyService;
    public ObservableCollection<Monkey> Monkeys { get; } = new();
    IConnectivity _connectivity;
    IGeolocation _geolocation;

    public MonkeysViewModel(MonkeyService monkeyService, IConnectivity connectivity, IGeolocation geolocation) 
    {
        Title = "Monkey Finder";
        _monkeyService = monkeyService;
        _connectivity = connectivity;
        _geolocation = geolocation;
    }

    [RelayCommand]
    async Task GetClosestMonkeyAsync()
    {
        if(IsBusy || Monkeys.Count == 0)
        {
            return;
        }

        try
        {
            var location = await _geolocation.GetLastKnownLocationAsync();
            if (location is null)
            {
                location = await _geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(30)
                });

            }

            if (location is null)
            {
                return;
            }

            var first = Monkeys.OrderBy(m => location.CalculateDistance(m.Latitude, m.Longitude, DistanceUnits.Miles)).FirstOrDefault();

            if (first is null)
            {
                return;
            }

            await Shell.Current.DisplayAlert("Closest Monkey", $"{first.Name} in {first.Location}", "OK");
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error", $"Unable to get closest monkey: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    async Task GoToDetailsAsync(Monkey monkey)
    {
        if (monkey is null)
            return;

        await Shell.Current.GoToAsync($"{nameof(DetailsPage)}", true, 
            new Dictionary<string, object> 
            {
                { "Monkey", monkey }
            });
    }

    [RelayCommand]
    async Task GetMonkeysAsync()
    {
        if (IsBusy)
            return;

        try
        {
            if(Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Internet Issue",
                    $"Check your internet and try again!", "Ok");
                return;
            }

            IsBusy = true;
            var monkeys = await _monkeyService.GetMonkeys();

            if (Monkeys.Count != 0)
                Monkeys.Clear();

            foreach (var monkey in monkeys)
                Monkeys.Add(monkey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error", $"Unable to get monkeys: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
