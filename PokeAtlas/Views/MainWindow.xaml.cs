using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using PokeAtlas.Models;
using PokeAtlas.Services;

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

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Carrega as localizações para a lista lateral
            var region = await _api.GetRegionAsync("kanto");
            RegionsList.ItemsSource = region.locations;
            Title = $"PokeAtlas — Kanto: {region.locations.Count} localizações";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro PokéAPI", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RegionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RegionsList.SelectedItem is NamedAPIResource loc)
        {
            var id = ExtractId(loc.url);
            var title = ToTitle(loc.name);
            NavigationService?.Navigate(new RouteDetailView(_api, title, id));
        }
    }

    private static int ExtractId(string url)
        => int.Parse(Regex.Match(url, @"\/(\d+)\/?$").Groups[1].Value);

    private static string ToTitle(string slug)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));

    private void Hotspot_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && RegionImage.ActualWidth > 0 && RegionImage.ActualHeight > 0)
        {
            var parts = tag.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double percentX) &&
                double.TryParse(parts[1], out double percentY))
            {
                double x = percentX * RegionImage.ActualWidth;
                double y = percentY * RegionImage.ActualHeight;
                Canvas.SetLeft(btn, x);
                Canvas.SetTop(btn, y);
            }
        }
    }

    private void RegionImage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        foreach (var child in MapCanvas.Children.OfType<Button>())
        {
            Hotspot_Loaded(child, null);
        }
    }

    private void Hotspot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string slug)
        {
            var loc = (RegionsList.ItemsSource as System.Collections.IEnumerable)?
                .Cast<NamedAPIResource>()
                .FirstOrDefault(l => l.name == slug);

            if (loc != null)
            {
                var id = ExtractId(loc.url);
                var title = ToTitle(loc.name);
                NavigationService?.Navigate(new RouteDetailView(_api, title, id));
            }
            else
            {
                MessageBox.Show($"Localização '{slug}' não encontrada na lista.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
