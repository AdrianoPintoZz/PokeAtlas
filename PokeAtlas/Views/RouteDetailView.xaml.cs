using PokeAtlas.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PokeAtlas;

public partial class RouteDetailView : Page
{
    private readonly IPokeApiClient _api;
    private readonly string _locationName;
    private readonly int _locationId;

    public ObservableCollection<EncounterRow> Encounters { get; } = new();

    public RouteDetailView(IPokeApiClient api, string locationName, int locationId)
    {
        InitializeComponent();
        _api = api;
        _locationName = locationName;
        _locationId = locationId;

        EncountersGrid.ItemsSource = Encounters;
        Loaded += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Header.Text = _locationName;

        // 1) Obter a Location e percorrer as suas Areas
        var location = await _api.GetLocationAsync(_locationId);
        Encounters.Clear();

        foreach (var areaRef in location.areas)
        {
            var areaId = int.Parse(Regex.Match(areaRef.url, @"\/(\d+)\/?$").Groups[1].Value);
            var area = await _api.GetLocationAreaAsync(areaId);

            foreach (var pe in area.pokemon_encounters)
                foreach (var vd in pe.version_details.Where(v => v.version.name == "firered"))
                    foreach (var ed in vd.encounter_details)
                    {
                        Encounters.Add(new EncounterRow
                        {
                            Pokemon = ToTitle(pe.pokemon.name),
                            Method = ToTitle(ed.method.name),
                            LevelRange = ed.min_level == ed.max_level ? $"{ed.min_level}" : $"{ed.min_level}-{ed.max_level}",
                            Chance = $"{ed.chance}%",
                            TimeOfDay = ed.condition_values.Count == 0 ? "—"
                                      : string.Join(", ", ed.condition_values.Select(c => ToTitle(c.name)))
                        });
                    }
        }

        // ordenar (opcional)
        var ordered = Encounters.OrderBy(e => e.Pokemon).ThenBy(e => e.Method).ToList();
        Encounters.Clear();
        foreach (var row in ordered) Encounters.Add(row);
    }

    private void EncountersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EncountersGrid.SelectedItem is EncounterRow row)
        {
            NavigationService?.Navigate(
                new PokemonDetailView(_api, row.Pokemon.ToLower())
            );
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private static string ToTitle(string slug) =>
        System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(slug.Replace('-', ' '));
}

public class EncounterRow
{
    public string Pokemon { get; set; } = "";
    public string Method { get; set; } = "";
    public string LevelRange { get; set; } = "";
    public string Chance { get; set; } = "";
    public string TimeOfDay { get; set; } = "";
}
