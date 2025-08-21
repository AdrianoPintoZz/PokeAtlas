using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PokeAtlas.Models;
using PokeAtlas.Services;

namespace PokeAtlas;

public partial class MainWindow : Page
{
    private readonly IPokeApiClient _api;
    private const double ZoomNavThreshold = 1.1; // limiar para mostrar navegação

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
            RegionsList.ItemsSource = region.locations;
            Title = $"PokeAtlas — Kanto: {region.locations.Count} localizações";
            UpdateNavVisibility();
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

    // Nota: Tags atuais são slugs; este método só reposicionaria se Tag estivesse "xPercent,yPercent".
    // Se não for usar percentagens, este método poderia ser removido.
    private void Hotspot_Loaded(object? sender, RoutedEventArgs? e)
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
            Hotspot_Loaded(child, null);
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

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        PreserveViewportCenter(e.OldValue, e.NewValue);
        UpdateNavVisibility();
    }

    private void MapScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => UpdateNavVisibility();

    private void NavUp_Click(object sender, RoutedEventArgs e)
        => MapScroll.ScrollToVerticalOffset(Math.Max(0, MapScroll.VerticalOffset - 40));

    private void NavDown_Click(object sender, RoutedEventArgs e)
        => MapScroll.ScrollToVerticalOffset(Math.Min(MapScroll.ExtentHeight, MapScroll.VerticalOffset + 40));

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

    // Mantém o centro do viewport aproximadamente ao alterar o zoom
    private void PreserveViewportCenter(double oldZoom, double newZoom)
    {
        if (oldZoom <= 0 || Math.Abs(newZoom - oldZoom) < 0.0001) return;
        if (MapScroll == null) return;

        // Ponto central atual em coordenadas de conteúdo (antes do novo zoom ser aplicado pelo layout pass seguinte)
        double centerXContent = MapScroll.HorizontalOffset + MapScroll.ViewportWidth / 2;
        double centerYContent = MapScroll.VerticalOffset + MapScroll.ViewportHeight / 2;

        // Fator relativo
        double factor = newZoom / oldZoom;

        // Após o layout (dispatcher) reajustar, posicionar offsets
        Dispatcher.BeginInvoke(new Action(() =>
        {
            double newCenterX = centerXContent * factor;
            double newCenterY = centerYContent * factor;

            MapScroll.ScrollToHorizontalOffset(Math.Max(0, newCenterX - MapScroll.ViewportWidth / 2));
            MapScroll.ScrollToVerticalOffset(Math.Max(0, newCenterY - MapScroll.ViewportHeight / 2));
            UpdateNavVisibility();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void NavigateToSlug(string slug)
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
            MessageBox.Show($"Localização '{slug}' não encontrada na lista.", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShapeHotspot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string slug)
            NavigateToSlug(slug);
    }
}
