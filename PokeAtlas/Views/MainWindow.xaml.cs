using PokeAtlas.Models;
using PokeAtlas.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PokeAtlas;

public partial class MainWindow : Page
{
    private readonly IPokeApiClient _api;

    public MainWindow(IPokeApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, System.Windows.RoutedEventArgs e)
    {
        var region = await _api.GetRegionAsync("kanto");
        RegionsList.ItemsSource = region.locations; 
    }

    private void RegionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (RegionsList.SelectedItem is NamedAPIResource loc)
    {
        var id = int.Parse(Regex.Match(loc.url, @"\/(\d+)\/?$").Groups[1].Value);
        var title = ToTitle(loc.name);
            NavigationService?.Navigate(new RouteDetailView(_api, title, id));
        }
}

    private static string ToTitle(string s) =>
        System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(s.Replace('-', ' '));
}
