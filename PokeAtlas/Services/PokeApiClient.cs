using Microsoft.Extensions.DependencyInjection;
using PokeAtlas.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PokeAtlas.Services
{
    public sealed class PokeApiClient : IPokeApiClient
    {
        private readonly HttpClient _http;

        public PokeApiClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("PokeAPI");
        }

        public Task<VersionDto> GetVersionAsync(string slug = "firered", CancellationToken ct = default)
        => GetAsync<VersionDto>($"version/{slug}/", ct);

        public Task<RegionDto> GetRegionAsync(string slug = "kanto", CancellationToken ct = default)
            => GetAsync<RegionDto>($"region/{slug}/", ct);

        public Task<LocationDto> GetLocationAsync(int id, CancellationToken ct = default)
            => GetAsync<LocationDto>($"location/{id}/", ct);

        public Task<LocationAreaDto> GetLocationAreaAsync(int id, CancellationToken ct = default)
            => GetAsync<LocationAreaDto>($"location-area/{id}/", ct);

        public Task<PokemonDto> GetPokemonAsync(string nameOrId, CancellationToken ct = default)
        => GetAsync<PokemonDto>($"pokemon/{nameOrId}/", ct);

        public Task<PokemonSpeciesDto> GetPokemonSpeciesAsync(int id, CancellationToken ct = default)
            => GetAsync<PokemonSpeciesDto>($"pokemon-species/{id}/", ct);

        public Task<EvolutionChainDto> GetEvolutionChainAsync(int id, CancellationToken ct = default)
            => GetAsync<EvolutionChainDto>($"evolution-chain/{id}/", ct);

        private async Task<T> GetAsync<T>(string path, CancellationToken ct)
        {
            using var res = await _http.GetAsync(path, ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
                   ?? throw new InvalidOperationException($"Resposta vazia em {path}");
        }
    }
    public sealed class HttpClientFactory(IServiceProvider sp)
    {
        private readonly IHttpClientFactory _factory = sp.GetRequiredService<IHttpClientFactory>();
        public HttpClient CreateClient(string name) => _factory.CreateClient(name);
    }
}
