using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;

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

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<KeysUsuario>> GetKeysUsuario(int idUsuario)
        {
            var keys = await this.repo.GetKeysUsuarioAsync(idUsuario);
            if (keys == null)
            {
                return NotFound(new { mensaje = $"No se encontraron credenciales para el usuario {idUsuario}." });
            }
            return Ok(keys);
        }

        [HttpPut]
        [Route("[action]")]
        public async Task<ActionResult> ActualizarPasswordUsuario([FromBody] KeysUsuario keys)
        {
            var usuario = await this.repo.GetUsuarioByIdAsync(keys.IdUsuario);
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }
            await this.repo.ActualizarPassword(
                keys.IdUsuario,
                keys.Salt,
                keys.Password
            );
            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> ActualizarPerfil(int id, [FromBody] Usuario user)
        {
            var usuarioExistente = await this.repo.GetUsuarioByIdAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound(new { mensaje = "No se puede actualizar: Usuario no encontrado." });
            }

            try
            {
                await this.repo.UpdateUsuario(
                    id,
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
        public async Task<ActionResult> ActualizarEstadoUsuario(int id)
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
