using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object? _currentPage;
    private string _pageTitle = "Today";
    private string _pageSubtitle = "Your daily energy and hydration at a glance";
    private bool _isLoading = true;
    private string _startupError = string.Empty;

    private readonly AppState _state;
    private readonly FoodCatalogService _catalog;

    public MainViewModel(
        AppState state,
        FoodCatalogService catalog,
        OpenFoodFactsService openFoodFacts)
    {
        _state = state;
        _catalog = catalog;

        Dashboard = new DashboardViewModel(state);
        Foods = new FoodsViewModel(state, catalog, openFoodFacts);
        Measurements = new MeasurementsViewModel(state);
        Insights = new InsightsViewModel(state);
        Settings = new SettingsViewModel(state);

        NavigateDashboardCommand = new RelayCommand(NavigateDashboard);
        NavigateFoodsCommand = new RelayCommand(NavigateFoods);
        NavigateMeasurementsCommand = new RelayCommand(NavigateMeasurements);
        NavigateInsightsCommand = new RelayCommand(NavigateInsights);
        NavigateSettingsCommand = new RelayCommand(NavigateSettings);

        _currentPage = Dashboard;
    }

    public DashboardViewModel Dashboard { get; }

    public FoodsViewModel Foods { get; }

    public MeasurementsViewModel Measurements { get; }

    public InsightsViewModel Insights { get; }

    public SettingsViewModel Settings { get; }

    public RelayCommand NavigateDashboardCommand { get; }

    public RelayCommand NavigateFoodsCommand { get; }

    public RelayCommand NavigateMeasurementsCommand { get; }

    public RelayCommand NavigateInsightsCommand { get; }

    public RelayCommand NavigateSettingsCommand { get; }

    public object? CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsFoodsActive));
                OnPropertyChanged(nameof(IsMeasurementsActive));
                OnPropertyChanged(nameof(IsInsightsActive));
                OnPropertyChanged(nameof(IsSettingsActive));
            }
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public string PageSubtitle
    {
        get => _pageSubtitle;
        private set => SetProperty(ref _pageSubtitle, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string StartupError
    {
        get => _startupError;
        private set
        {
            if (SetProperty(ref _startupError, value))
            {
                OnPropertyChanged(nameof(HasStartupError));
            }
        }
    }

    public bool HasStartupError => !string.IsNullOrWhiteSpace(StartupError);

    public bool IsDashboardActive => ReferenceEquals(CurrentPage, Dashboard);

    public bool IsFoodsActive => ReferenceEquals(CurrentPage, Foods);

    public bool IsMeasurementsActive => ReferenceEquals(CurrentPage, Measurements);

    public bool IsInsightsActive => ReferenceEquals(CurrentPage, Insights);

    public bool IsSettingsActive => ReferenceEquals(CurrentPage, Settings);

    public async Task InitializeAsync()
    {
        try
        {
            await _state.InitializeAsync();
            await _catalog.InitializeAsync();
            Dashboard.Initialize();
            Foods.Initialize();
            Measurements.Initialize();
            Insights.Initialize();
            Settings.Initialize();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StartupError = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NavigateDashboard()
    {
        Dashboard.Refresh();
        SetPage(Dashboard, "Today", "Your daily energy and hydration at a glance");
    }

    private void NavigateFoods()
    {
        Foods.Refresh();
        SetPage(Foods, "Add food", "Australian staples and supermarket products");
    }

    private void NavigateMeasurements()
    {
        Measurements.Refresh();
        SetPage(
            Measurements,
            "Measurements",
            "Track weight, body measurements and any custom metric");
    }

    private void NavigateInsights()
    {
        Insights.Refresh();
        SetPage(Insights, "Insights", "Trends that help you stay consistent");
    }

    private void NavigateSettings()
    {
        Settings.Refresh();
        SetPage(Settings, "Settings", "Goals, privacy and your data");
    }

    private void SetPage(object page, string title, string subtitle)
    {
        CurrentPage = page;
        PageTitle = title;
        PageSubtitle = subtitle;
    }
}
