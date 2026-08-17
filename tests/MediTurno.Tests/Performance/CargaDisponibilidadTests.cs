using System.Diagnostics;
using System.Net;
using FluentAssertions;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Performance;

public class CargaDisponibilidadTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const int PeticionesConcurrentes = 50;
    private const int UmbralPercentil95Ms = 500;

    [Fact]
    public async Task Disponibilidad_ConCincuentaPeticionesConcurrentes_DebeMantenerElPercentil95BajoElUmbral()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var fecha = DateOnly.FromDateTime(Fechas.ProximoDiaHabil(new TimeOnly(9, 0)));
        var ruta = $"/api/medicos/1/disponibilidad?fecha={fecha:yyyy-MM-dd}";

        await cliente.GetAsync(ruta);

        var tareas = Enumerable.Range(0, PeticionesConcurrentes).Select(async _ =>
        {
            var cronometro = Stopwatch.StartNew();
            var respuesta = await cliente.GetAsync(ruta);
            cronometro.Stop();

            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            return cronometro.Elapsed.TotalMilliseconds;
        });

        var tiempos = (await Task.WhenAll(tareas)).OrderBy(t => t).ToArray();

        var indice = (int)Math.Ceiling(tiempos.Length * 0.95) - 1;
        var percentil95 = tiempos[indice];

        percentil95.Should().BeLessThan(UmbralPercentil95Ms);
    }
}
