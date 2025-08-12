using PokeAtlas.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeAtlas.Services
{
    public interface IPokeApiClient
    {
        Task<VersionDto> GetVersionAsync(string slug = "firered", CancellationToken ct = default);
        Task<RegionDto> GetRegionAsync(string slug = "kanto", CancellationToken ct = default);
        Task<LocationDto> GetLocationAsync(int id, CancellationToken ct = default);
        Task<LocationAreaDto> GetLocationAreaAsync(int id, CancellationToken ct = default);

        Task<PokemonDto> GetPokemonAsync(string nameOrId, CancellationToken ct = default);
        Task<PokemonSpeciesDto> GetPokemonSpeciesAsync(int id, CancellationToken ct = default);
        Task<EvolutionChainDto> GetEvolutionChainAsync(int id, CancellationToken ct = default);
    }
}
