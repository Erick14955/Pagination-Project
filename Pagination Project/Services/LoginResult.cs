using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public enum LoginEstado
    {
        Correcto,
        UsuarioNoExiste,
        ContrasenaIncorrecta,
        UsuarioInactivo,
        CuentaBloqueada
    }

    public class LoginResult
    {
        public LoginEstado Estado { get; set; }

        public Usuario? Usuario { get; set; }

        public bool Exitoso => Estado == LoginEstado.Correcto && Usuario is not null;
    }
}