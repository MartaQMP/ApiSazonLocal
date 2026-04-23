using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private IRepository repo;

        public UsuariosController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Usuario>>> GetUsuarios()
        {
            var usuarios = await this.repo.GetUsuariosAsync();
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await this.repo.GetUsuarioByIdAsync(id);
            if (usuario == null)
            {
                return NotFound(new { mensaje = $"El usuario con ID {id} no existe." });
            }
            return Ok(usuario);
        }

        [HttpGet]
        [Route("[action]/{email}")]
        public async Task<ActionResult<Usuario>> GetUsuarioByEmail(string email)
        {
            var usuario = await this.repo.GetUsuarioByEmailAsync(email);
            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No hay ningún usuario con el email {email}." });
            }
            return Ok(usuario);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Usuario>>> UsuariosSinHash()
        {
            try
            {
                var usuarios = await this.repo.GetUsuariosSinHashAsync();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NoContent();
                }

                return Ok(usuarios);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al consultar los usuarios sin credenciales.");
            }
        }

        [HttpPut]
        [Route("[action]")]
        public async Task<ActionResult> UpdatePerfil(Usuario user)
        {
            var usuarioExistente = await this.repo.GetUsuarioByIdAsync(user.IdUsuario);
            if (usuarioExistente == null)
            {
                return NotFound(new { mensaje = "No se puede actualizar: Usuario no encontrado." });
            }

            try
            {
                await this.repo.UpdateUsuario(
                    user.IdUsuario,
                    user.Nombre,
                    user.Apellidos,
                    user.Telefono,
                    user.Imagen
                );
                return Ok(new { mensaje = "Perfil actualizado correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno al actualizar el perfil.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> UpdateEstado(int id)
        {
            var usuario = await this.repo.GetUsuarioByIdAsync(id);
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado para cambiar estado." });
            }

            await this.repo.UpdateEstadoUsuarioAsync(id);
            return Ok(new { mensaje = "Estado del usuario actualizado correctamente." });
        }
    }
}
