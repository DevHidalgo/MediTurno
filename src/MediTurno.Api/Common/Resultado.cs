namespace MediTurno.Api.Common;

public enum TipoError
{
    Ninguno,
    Validacion,
    NoEncontrado,
    Conflicto,
    NoAutorizado,
    Prohibido
}

public class Resultado<T>
{
    public bool Exitoso { get; private init; }
    public T? Valor { get; private init; }
    public TipoError Error { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;

    public static Resultado<T> Ok(T valor) =>
        new() { Exitoso = true, Valor = valor, Error = TipoError.Ninguno };

    public static Resultado<T> Falla(TipoError error, string mensaje) =>
        new() { Exitoso = false, Error = error, Mensaje = mensaje };
}
