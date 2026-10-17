using System.Collections.ObjectModel;
using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class FoodsViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly FoodCatalogService _catalog;
    private readonly OpenFoodFactsService _openFoodFacts;
    private readonly AsyncRelayCommand _searchOnlineCommand;
    private string _query = string.Empty;
    private string _selectedCategory = "All categories";
    private string _selectedRetailer = "All retailers";
    private FoodItem? _selectedFood;
    private string _amount = string.Empty;
    private MealType _selectedMeal = MealType.Snack;
    private bool _isSearchingOnline;
    private string _onlineStatus = string.Empty;
    private string _statusMessage = string.Empty;
    private string _customName = string.Empty;
    private string _customCalories = string.Empty;
    private string _customServing = "100";
    private int _selectedTabIndex;

    public FoodsViewModel(
        AppState state,
        FoodCatalogService catalog,
        OpenFoodFactsService openFoodFacts)
    {
        _state = state;
        _catalog = catalog;
        _openFoodFacts = openFoodFacts;
        _state.Changed += (_, _) => RefreshLocalResults();

        _searchOnlineCommand = new AsyncRelayCommand(
            SearchOnlineAsync,
            () => !IsSearchingOnline);
        SearchOnlineCommand = _searchOnlineCommand;
        AddFoodCommand = new AsyncRelayCommand(AddSelectedFoodAsync);
        CreateCustomFoodCommand = new AsyncRelayCommand(CreateCustomFoodAsync);
    }

    public ObservableCollection<FoodItem> LocalResults { get; } = [];

    public ObservableCollection<FoodItem> OnlineResults { get; } = [];

    public ObservableCollection<string> Categories { get; } = [];

    public IReadOnlyList<string> Retailers { get; } =
        ["All retailers", "Coles", "Woolworths", "ALDI", "IGA"];

    public IReadOnlyList<MealType> Meals { get; } = Enum.GetValues<MealType>();

    public AsyncRelayCommand SearchOnlineCommand { get; }

    public AsyncRelayCommand AddFoodCommand { get; }

    public AsyncRelayCommand CreateCustomFoodCommand { get; }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                RefreshLocalResults();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                RefreshLocalResults();
            }
        }
    }

    public string SelectedRetailer
    {
        get => _selectedRetailer;
        set => SetProperty(ref _selectedRetailer, value);
    }

    public FoodItem? SelectedFood
    {
        get => _selectedFood;
        set
        {
            if (SetProperty(ref _selectedFood, value))
            {
                Amount = value?.DefaultServingAmount.ToString("0.#") ?? string.Empty;
                StatusMessage = string.Empty;
                OnPropertyChanged(nameof(HasSelectedFood));
                OnPropertyChanged(nameof(PreviewCalories));
                OnPropertyChanged(nameof(PreviewKilojoules));
            }
        }
    }

    public bool HasSelectedFood => SelectedFood is not null;

    public string Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                OnPropertyChanged(nameof(PreviewCalories));
                OnPropertyChanged(nameof(PreviewKilojoules));
            }
        }
    }

    public MealType SelectedMeal
    {
        get => _selectedMeal;
        set => SetProperty(ref _selectedMeal, value);
    }

    public decimal PreviewCalories =>
        SelectedFood is not null && InputParser.TryPositiveDecimal(Amount, out var amount)
            ? NutritionMath.ForAmount(SelectedFood.CaloriesPer100, amount)
            : 0;

    public decimal PreviewKilojoules =>
        NutritionMath.KilocaloriesToKilojoules(PreviewCalories);

    public bool IsSearchingOnline
    {
        get => _isSearchingOnline;
        private set
        {
            if (SetProperty(ref _isSearchingOnline, value))
            {
                _searchOnlineCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string OnlineStatus
    {
        get => _onlineStatus;
        private set
        {
            if (SetProperty(ref _onlineStatus, value))
            {
                OnPropertyChanged(nameof(HasOnlineStatus));
            }
        }
    }

    public bool HasOnlineStatus => !string.IsNullOrWhiteSpace(OnlineStatus);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string CustomName
    {
        get => _customName;
        set => SetProperty(ref _customName, value);
    }

    public string CustomCalories
    {
        get => _customCalories;
        set => SetProperty(ref _customCalories, value);
    }

    public string CustomServing
    {
        get => _customServing;
        set => SetProperty(ref _customServing, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public void Initialize()
    {
        Categories.Clear();
        Categories.Add("All categories");
        foreach (var category in _catalog.Categories)
        {
            Categories.Add(category);
        }

        RefreshLocalResults();
    }

    public void Refresh() => RefreshLocalResults();

    private void RefreshLocalResults()
    {
        if (!_catalog.IsInitialized)
        {
            return;
        }

        var currentId = SelectedFood?.Id;
        LocalResults.Clear();
        foreach (var food in _catalog.Search(Query, SelectedCategory))
        {
            LocalResults.Add(food);
        }

        if (currentId is not null)
        {
            SelectedFood = LocalResults.FirstOrDefault(food => food.Id == currentId)
                ?? SelectedFood;
        }
    }

    private async Task SearchOnlineAsync()
    {
        if (Query.Trim().Length < 2)
        {
            OnlineStatus = "Enter at least two characters or a barcode.";
            return;
        }

        IsSearchingOnline = true;
        SelectedTabIndex = 1;
        OnlineStatus = "Searching Australian supermarket products...";
        try
        {
            var results = await _openFoodFacts.SearchAsync(Query, SelectedRetailer);
            OnlineResults.Clear();
            foreach (var food in results)
            {
                OnlineResults.Add(food);
            }

            OnlineStatus = results.Count == 0
                ? "No matching products with usable nutrition data were found."
                : $"{results.Count} products found. Select one to review its serving.";
        }
        catch (HttpRequestException exception)
        {
            OnlineStatus = $"Online search failed: {exception.Message}";
        }
        catch (TaskCanceledException)
        {
            OnlineStatus = "Online search timed out. Check your connection and try again.";
        }
        finally
        {
            IsSearchingOnline = false;
        }
    }

    private async Task AddSelectedFoodAsync()
    {
        if (SelectedFood is null)
        {
            StatusMessage = "Select a food first.";
            return;
        }

        if (!InputParser.TryPositiveDecimal(Amount, out var amount) || amount > 5000)
        {
            StatusMessage = "Enter an amount between 0 and 5,000.";
            return;
        }

        try
        {
            if (!_catalog.IsBundledFood(SelectedFood.Id))
            {
                await _state.UpsertFoodAsync(SelectedFood);
            }

            await _state.AddEntryAsync(new FoodLogEntry
            {
                FoodId = SelectedFood.Id,
                FoodName = SelectedFood.Name,
                Brand = SelectedFood.Brand,
                Meal = SelectedMeal,
                LoggedAt = DateTime.Now,
                Amount = amount,
                MeasureUnit = SelectedFood.MeasureUnit,
                Calories = NutritionMath.ForAmount(SelectedFood.CaloriesPer100, amount),
                Protein = NutritionMath.ForAmount(SelectedFood.ProteinPer100, amount),
                Carbohydrates = NutritionMath.ForAmount(
                    SelectedFood.CarbohydratesPer100,
                    amount),
                Fat = NutritionMath.ForAmount(SelectedFood.FatPer100, amount)
            });

            StatusMessage = $"{SelectedFood.Name} added to today.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save the food: {exception.Message}";
        }
    }

    private async Task CreateCustomFoodAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomName))
        {
            StatusMessage = "Enter a name for the custom food.";
            return;
        }

        if (!InputParser.TryPositiveDecimal(CustomCalories, out var calories) ||
            calories > 1000)
        {
            StatusMessage = "Enter calories per 100 g between 1 and 1,000.";
            return;
        }

        if (!InputParser.TryPositiveDecimal(CustomServing, out var serving) ||
            serving > 5000)
        {
            StatusMessage = "Enter a serving amount between 1 and 5,000 g.";
            return;
        }

        var food = new FoodItem
        {
            Id = $"custom:{Guid.NewGuid():N}",
            Name = CustomName.Trim(),
            Category = "Custom",
            CaloriesPer100 = calories,
            DefaultServingAmount = serving,
            ServingDescription = $"{serving:0.#} g",
            Source = "User created"
        };

        try
        {
            await _state.UpsertFoodAsync(food);
            Query = food.Name;
            SelectedFood = food;
            CustomName = string.Empty;
            CustomCalories = string.Empty;
            CustomServing = "100";
            StatusMessage = "Custom food saved. Review the serving, then add it.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save the custom food: {exception.Message}";
        }
    }
}
