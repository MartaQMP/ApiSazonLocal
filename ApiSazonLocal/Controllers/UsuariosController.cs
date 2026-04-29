using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ApiSazonLocal.Helpers;
using SazonLocalModels.Dto;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public UsuariosController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        public async Task<ActionResult<List<Usuario>>> GetUsuarios()
        {
            var usuarios = await this.repo.GetUsuariosAsync();
            return Ok(usuarios);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<Usuario>> GetUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            if (usuario == null)
            {
                return NotFound();
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

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<KeysUsuario>> GetKeysUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var keys = await this.repo.GetKeysUsuarioAsync(usuario.IdUsuario);
            if (keys == null)
            {
                return NotFound(new { mensaje = $"No se encontraron credenciales para el usuario." });
            }
            return Ok(keys);
        }

        [Authorize]
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
        [Route("[action]")]
        public async Task<ActionResult> ActualizarPerfil([FromBody] Usuario user)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var usuarioExistente = await this.repo.GetUsuarioByIdAsync(usuario.IdUsuario);
            if (usuarioExistente == null)
            {
                return NotFound(new { mensaje = "No se puede actualizar: Usuario no encontrado." });
            }

            try
            {
                await this.repo.UpdateUsuario(
                    usuario.IdUsuario,
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

        [Authorize(Roles = "ADMINISTRADOR")]
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
