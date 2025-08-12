using PokeAtlas.Models;
using PokeAtlas.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PokeAtlas;

public partial class PokemonDetailView : Page
{
    private readonly IPokeApiClient _api;
    private readonly string _nameOrId;

    public PokemonDetailView(IPokeApiClient api, string nameOrId)
    {
        InitializeComponent();
        _api = api;
        _nameOrId = nameOrId;
        Loaded += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // /pokemon
        var p = await _api.GetPokemonAsync(_nameOrId);
        TitleText.Text = ToTitle(p.name) + $"  (#{p.id:000})";

        if (!string.IsNullOrWhiteSpace(p.sprites.front_default))
            Sprite.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(p.sprites.front_default));

        // Tipos (badges)
        TypesPanel.Children.Clear();
        foreach (var t in p.types.OrderBy(x => x.slot))
        {
            TypesPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(ColorFromType(t.type.name)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock { Text = ToTitle(t.type.name), Foreground = Brushes.White }
            });
        }

        // Habilidades
        AbilitiesList.ItemsSource = p.abilities
            .Select(a => $"{ToTitle(a.ability.name)}" + (a.is_hidden ? " (Hidden)" : ""))
            .ToList();

        // Stats (barras simples)
        StatsList.Items.Clear();
        foreach (var s in p.stats)
        {
            var label = ToTitle(s.stat.name.Replace("special-", "Sp. "));
            var bar = new ProgressBar { Minimum = 0, Maximum = 255, Value = s.base_stat, Height = 14, Margin = new Thickness(8, 0, 0, 6) };
            var row = new DockPanel();
            row.Children.Add(new TextBlock { Text = $"{label} {s.base_stat}", Width = 110, Foreground = Brushes.White });
            DockPanel.SetDock(bar, Dock.Right);
            row.Children.Add(bar);
            StatsList.Items.Add(row);
        }

        // /pokemon-species → descrição curta (EN) + id da cadeia de evolução
        var species = await _api.GetPokemonSpeciesAsync(p.id);

        var english = species.flavor_text_entries
            .Where(f => f.language.name == "en")
            .Select(f => Clean(f.flavor_text))
            .ToList();

        var shortText = english.OrderBy(t => t.Length).FirstOrDefault() ?? english.FirstOrDefault() ?? "";
        FlavorText.Text = Truncate(shortText, 160);

        // /evolution-chain → lista com sprites
        var evoId = int.Parse(Regex.Match(species.evolution_chain.url, @"\/(\d+)\/?$").Groups[1].Value);
        var chain = await _api.GetEvolutionChainAsync(evoId);

        var speciesNodes = Flatten(chain.chain).Select(x => x.species.name).ToList();

        _evolutions.Clear();
        foreach (var spName in speciesNodes)
        {
            var poke = await _api.GetPokemonAsync(spName); // tem sprites.front_default
            _evolutions.Add(new EvoVM
            {
                Name = ToTitle(spName),
                Slug = spName,
                SpriteUrl = poke.sprites.front_default ?? ""
            });
        }
        EvoList.ItemsSource = _evolutions;
    }

    // ——— Evoluções (VM + coleção + clique) ———
    public class EvoVM
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string SpriteUrl { get; set; } = "";
    }
    private readonly ObservableCollection<EvoVM> _evolutions = new();

    private void EvoItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is StackPanel sp && sp.DataContext is EvoVM evo)
        {
            NavigationService?.Navigate(new PokemonDetailView(_api, evo.Slug));
        }
    }

    // ——— Helpers ———
    private static string Clean(string s) => s.Replace('\n', ' ').Replace('\f', ' ').Trim();

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max].TrimEnd() + "…";

    private static string ToTitle(string s)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.Replace('-', ' '));

    private static System.Collections.Generic.IEnumerable<EvoNode> Flatten(EvoNode n)
    {
        yield return n;
        foreach (var child in n.evolves_to.SelectMany(Flatten)) yield return child;
    }

    private static Color ColorFromType(string type) => type switch
    {
        "fire" => Color.FromRgb(0xEE, 0x81, 0x57),
        "water" => Color.FromRgb(0x6A, 0x8D, 0xFF),
        "grass" => Color.FromRgb(0x63, 0xBC, 0x5A),
        "electric" => Color.FromRgb(0xF4, 0xD0, 0x45),
        "ice" => Color.FromRgb(0x8E, 0xD4, 0xD6),
        "fighting" => Color.FromRgb(0xC0, 0x3F, 0x3C),
        "poison" => Color.FromRgb(0xA5, 0x41, 0xA3),
        "ground" => Color.FromRgb(0xE2, 0xC0, 0x73),
        "flying" => Color.FromRgb(0x90, 0xA5, 0xEE),
        "psychic" => Color.FromRgb(0xFA, 0x91, 0xB2),
        "bug" => Color.FromRgb(0x92, 0xBC, 0x2C),
        "rock" => Color.FromRgb(0xB6, 0xA1, 0x52),
        "ghost" => Color.FromRgb(0x56, 0x6A, 0xBE),
        "dragon" => Color.FromRgb(0x0A, 0x6D, 0xC4),
        "dark" => Color.FromRgb(0x70, 0x5A, 0x4A),
        "steel" => Color.FromRgb(0xB7, 0xB7, 0xCE),
        "fairy" => Color.FromRgb(0xE3, 0xA3, 0xCF),
        _ => Color.FromRgb(0x88, 0x88, 0x88)
    };

    private void Back_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
}
