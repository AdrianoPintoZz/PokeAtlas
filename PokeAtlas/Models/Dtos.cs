using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeAtlas.Models
{
    public record NamedAPIResource(string name ,string url);
    public record VersionDto(string name, NamedAPIResource version_group);

    public record RegionDto(string name, List<NamedAPIResource> locations);

    public record LocationDto(string name, List<NamedAPIResource> areas);

    public record LocationAreaDto(string name, List<PokemonEncounter> pokemon_encounters);


    public record PokemonEncounter(
    NamedAPIResource pokemon,
    List<VersionEncounterDetail> version_details
    );

    public record VersionEncounterDetail(
    NamedAPIResource version,
    List<EncounterDetail> encounter_details
    );

    public record EncounterDetail(
        NamedAPIResource method,
        int min_level,
        int max_level,
        int chance,
        List<NamedAPIResource> condition_values
    );

    public record PokemonDto(
    int id,
    string name,
    Sprites sprites,
    List<TypeSlot> types,
    List<StatSlot> stats,
    List<AbilitySlot> abilities
);

    public record Sprites(string front_default);

    public record TypeSlot(int slot, NamedAPIResource type);

    public record StatSlot(NamedAPIResource stat, int base_stat);

    public record AbilitySlot(NamedAPIResource ability, bool is_hidden);

    public record PokemonSpeciesDto(
    int id,
    string name,
    List<FlavorText> flavor_text_entries,
    NamedAPIResource evolution_chain
);

    public record FlavorText(string flavor_text, NamedAPIResource language);

    public record EvolutionChainDto(EvoNode chain);

    public record EvoNode(NamedAPIResource species, List<EvoNode> evolves_to);
}
