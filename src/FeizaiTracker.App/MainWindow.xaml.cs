using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using FeizaiTracker.App.Services;
using FeizaiTracker.App.ViewModels;

namespace FeizaiTracker.App;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var store = new JsonAppDataStore(JsonAppDataStore.GetDefaultPath());
        var state = new AppState(store);
        var catalog = new FoodCatalogService(state);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("FeizaiTracker", "0.1"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(github.com/soeviturek/FeizaiTracker)"));

        var openFoodFacts = new OpenFoodFactsService(_httpClient);
        _viewModel = new MainViewModel(state, catalog, openFoodFacts);
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _httpClient.Dispose();
        base.OnClosed(e);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) =>
        await _viewModel.InitializeAsync();
}
