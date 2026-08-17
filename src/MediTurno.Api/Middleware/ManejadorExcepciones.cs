namespace MediTurno.Api.Middleware;

public class ManejadorExcepciones(RequestDelegate siguiente, ILogger<ManejadorExcepciones> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, "Error no controlado en {Ruta}", contexto.Request.Path);

            contexto.Response.Clear();
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            contexto.Response.ContentType = "application/json";

            await contexto.Response.WriteAsJsonAsync(new
            {
                mensaje = "Ocurrió un error inesperado. Contacte al administrador del sistema."
            });
        }
    }
}
