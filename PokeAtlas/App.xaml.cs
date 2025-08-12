using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Navigation;

namespace PokeAtlas;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.AddHttpClient("PokeAPI", c =>
                {
                    c.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
                    c.DefaultRequestHeaders.UserAgent.ParseAdd("PokeAtlas/1.0");
                    c.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                });

                s.AddTransient<Services.IPokeApiClient, Services.PokeApiClient>();
                s.AddTransient<MainWindow>();          
                s.AddTransient<RouteDetailView>();
            })
            .Build();

        AppHost.Start();

        // NavigationWindow sem UI (ou defina true para mostrar setas voltar/avançar)
        var nav = new NavigationWindow { ShowsNavigationUI = false, Title = "PokeAtlas" };

        // Resolver a MainPage via DI e navegar
        var main = AppHost.Services.GetRequiredService<MainWindow>();
        nav.Navigate(main);
        nav.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (AppHost != null) await AppHost.StopAsync();
        AppHost?.Dispose();
    }
}
