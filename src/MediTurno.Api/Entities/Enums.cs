namespace MediTurno.Api.Entities;

public enum RolUsuario
{
    Administrador = 1,
    Medico = 2,
    Recepcionista = 3
}

public enum EstadoCita
{
    Pendiente = 1,
    Confirmada = 2,
    Atendida = 3,
    Cancelada = 4,
    Ausente = 5
}
