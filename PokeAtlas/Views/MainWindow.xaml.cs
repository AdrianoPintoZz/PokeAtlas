using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using PokeAtlas.Models;
using PokeAtlas.Services;

namespace PokeAtlas;

public partial class MainWindow : Page
{
    private readonly IPokeApiClient _api;
    private const double ZoomNavThreshold = 1.1;
    private ICollectionView? _locationsView;
    private bool _mapScrollLoaded;              // Flag indicando que MapScroll está pronto
    private double _lastZoom = 1.0;             // Guarda último zoom aplicado (para evitar chamadas redundantes)

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
            var region = await _api.GetRegionAsync("kanto");
            _locationsView = CollectionViewSource.GetDefaultView(region.locations);
            RegionsList.ItemsSource = _locationsView;
            ApplySearchFilter();
            Title = $"PokeAtlas — Kanto: {region.locations.Count} localizações";
            UpdateNavVisibility();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro PokéAPI", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MapScroll_Loaded(object sender, RoutedEventArgs e)
    {
        _mapScrollLoaded = true;
        // Primeiro ajuste de zoom (garante centralização se já alterado antes de carregar o ScrollViewer)
        _lastZoom = ZoomSlider.Value;
    }

    // === PESQUISA DINÂMICA ===
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearchFilter();

    private void ApplySearchFilter()
    {
        if (_locationsView == null) return;
        var term = SearchBox.Text.Trim().ToLowerInvariant();
        var normalizedTerm = term.Replace("-", "").Replace(" ", "");

        _locationsView.Filter = o =>
        {
            if (o is not NamedAPIResource r) return false;
            if (string.IsNullOrEmpty(term)) return true;
            var name = r.name.ToLowerInvariant();
            var normalizedName = name.Replace("-", "").Replace(" ", "");
            return name.Contains(term) || normalizedName.Contains(normalizedTerm);
        };

        _locationsView.Refresh();

        if (RegionsList.SelectedItem is NamedAPIResource sel &&
            !_locationsView.Cast<object>().Contains(sel))
            RegionsList.SelectedItem = null;
    }
    // === FIM PESQUISA ===

    private void RegionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RegionsList.SelectedItem is NamedAPIResource loc)
        {
            var id = ExtractId(loc.url);
            var title = ToTitle(loc.name);
            NavigationService?.Navigate(new RouteDetailView(_api, title, id));
        }
    }

    private static int ExtractId(string url) =>
        int.Parse(Regex.Match(url, @"\/(\d+)\/?$").Groups[1].Value);

    private static string ToTitle(string slug) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));

    private void Hotspot_Loaded(object? sender, RoutedEventArgs? e)
    {
        // Só reposiciona se Tag tiver percentuais "x,y"
        if (sender is Button btn && btn.Tag is string tag && RegionImage.ActualWidth > 0 && RegionImage.ActualHeight > 0)
        {
            var parts = tag.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double px) &&
                double.TryParse(parts[1], out double py))
            {
                Canvas.SetLeft(btn, px * RegionImage.ActualWidth);
                Canvas.SetTop(btn, py * RegionImage.ActualHeight);
            }
        }
    }

    private void RegionImage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        foreach (var child in MapCanvas.Children.OfType<Button>())
            Hotspot_Loaded(child, null);
    }

    private void Hotspot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string slug)
            NavigateToSlug(slug);
    }

    private void ShapeHotspot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string slug)
            NavigateToSlug(slug);
    }

    private void NavigateToSlug(string slug)
    {
        var loc = (_locationsView ?? RegionsList.ItemsSource)?
            .Cast<NamedAPIResource>()
            .FirstOrDefault(l => l.name == slug);

        if (loc == null)
        {
            MessageBox.Show($"Localização '{slug}' não encontrada.", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var id = ExtractId(loc.url);
        var title = ToTitle(loc.name);
        NavigationService?.Navigate(new RouteDetailView(_api, title, id));
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Guarda valor mas evita NRE se ainda não carregou o ScrollViewer
        if (!_mapScrollLoaded || MapScroll == null)
        {
            _lastZoom = e.NewValue;
            return;
        }

        if (Math.Abs(e.NewValue - e.OldValue) < 0.0001) return;

        PreserveViewportCenter(e.OldValue, e.NewValue);
        _lastZoom = e.NewValue;
        UpdateNavVisibility();
    }

    private void MapScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateNavVisibility();

    private void NavUp_Click(object sender, RoutedEventArgs e) =>
        MapScroll.ScrollToVerticalOffset(Math.Max(0, MapScroll.VerticalOffset - 40));

    private void NavDown_Click(object sender, RoutedEventArgs e) =>
        MapScroll.ScrollToVerticalOffset(Math.Min(MapScroll.ExtentHeight, MapScroll.VerticalOffset + 40));

    private void UpdateNavVisibility()
    {
        if (MapScroll == null) return;
        bool zoomed = ZoomSlider.Value > ZoomNavThreshold;
        bool overflowY = MapScroll.ExtentHeight - MapScroll.ViewportHeight > 2;

        if (!(zoomed && overflowY))
        {
            NavUp.Visibility = Visibility.Collapsed;
            NavDown.Visibility = Visibility.Collapsed;
            return;
        }

        NavUp.Visibility = MapScroll.VerticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;
        double maxOffset = MapScroll.ExtentHeight - MapScroll.ViewportHeight - 1;
        NavDown.Visibility = MapScroll.VerticalOffset < maxOffset ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PreserveViewportCenter(double oldZoom, double newZoom)
    {
        // Proteções contra chamadas prematuras
        if (!_mapScrollLoaded || MapScroll == null) return;
        if (oldZoom <= 0 || Math.Abs(newZoom - oldZoom) < 0.0001) return;

        double centerX = MapScroll.HorizontalOffset + MapScroll.ViewportWidth / 2;
        double centerY = MapScroll.VerticalOffset + MapScroll.ViewportHeight / 2;
        double factor = newZoom / oldZoom;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (MapScroll == null) return; // segurança extra
            MapScroll.ScrollToHorizontalOffset(Math.Max(0, centerX * factor - MapScroll.ViewportWidth / 2));
            MapScroll.ScrollToVerticalOffset(Math.Max(0, centerY * factor - MapScroll.ViewportHeight / 2));
            UpdateNavVisibility();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }
}
