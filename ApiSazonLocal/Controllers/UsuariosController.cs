using ApiSazonLocal.Helpers;
using ApiSazonLocal.Repositories;
using ApiSazonLocal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;
        private BlobService service;
        private static string[] extensionesValidas = { ".jpg", ".jpeg", ".png" };
        private string containerName = "usuarios-sl";

        public UsuariosController(IRepository repo, HelperToken helper, BlobService service)
        {
            this.repo = repo;
            this.helper = helper;
            this.service = service;
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Usuario>>> GetUsuarios()
        {
            List<Usuario> usuarios = await this.repo.GetUsuariosAsync();
            foreach (Usuario usuario in usuarios)
            {
                if (!string.IsNullOrEmpty(usuario.Imagen))
                {
                    usuario.Imagen = this.service.GetBlobSasUrl(containerName, usuario.Imagen);
                }
            }
            return Ok(usuarios);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<Usuario>> GetUsuarioById()
        {
            UsuarioLogin usuarioLogin = this.helper.GetUsuario();
            Usuario usuario = await this.repo.GetUsuarioByIdAsync(usuarioLogin.IdUsuario);
            if (usuario == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(usuario.Imagen))
            {
                usuario.Imagen = this.service.GetBlobSasUrl(containerName, usuario.Imagen);
            }
            return Ok(usuario);
        }

        [HttpGet]
        [Route("[action]/{email}")]
        public async Task<ActionResult<Usuario>> GetUsuarioByEmail(string email)
        {
            Usuario usuario = await this.repo.GetUsuarioByEmailAsync(email);
            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No hay ningún usuario con el email {email}." });
            }
            if (!string.IsNullOrEmpty(usuario.Imagen))
            {
                usuario.Imagen = this.service.GetBlobSasUrl(containerName, usuario.Imagen);
            }
            return Ok(usuario);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<KeysUsuario>> GetKeysUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            KeysUsuario keys = await this.repo.GetKeysUsuarioAsync(usuario.IdUsuario);
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
            Usuario usuario = await this.repo.GetUsuarioByIdAsync(keys.IdUsuario);
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

        [Authorize]
        [HttpPut]
        [Route("[action]")]
        public async Task<ActionResult> ActualizarPerfil([FromForm] UsuarioDto user, IFormFile imagen)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            Usuario usuarioExistente = await this.repo.GetUsuarioByIdAsync(usuario.IdUsuario);
            if (usuarioExistente == null)
            {
                return NotFound(new { mensaje = "No se puede actualizar: Usuario no encontrado." });
            }

            try
            {
                string? urlImagen = null;
                string nombreImagenFinal = usuarioExistente.Imagen;
                if (imagen != null)
                {
                    string extension = Path.GetExtension(imagen.FileName).ToLower();
                    if (extensionesValidas.Contains(extension))
                    {
                        string nombreLimpio = HelperTextCleaner.LimpiarTexto(user.Nombre);
                        string apellidoLimpio = HelperTextCleaner.LimpiarTexto(user.Apellidos);
                        nombreImagenFinal = $"{usuario.IdUsuario}_{nombreLimpio}_{apellidoLimpio}_{DateTime.Now.Ticks}{extension}";

                        using (var stream = imagen.OpenReadStream())
                        {
                            if (usuarioExistente.Imagen != "usuario-generico.png")
                            {
                                await this.service.DeleteBlobAsync(containerName, usuarioExistente.Imagen);
                            }
                            await this.service.UploadBlobAsync(containerName, nombreImagenFinal, stream);
                        }
                    }
                }
                await this.repo.UpdateUsuario(
                    usuario.IdUsuario,
                    user.Nombre,
                    user.Apellidos,
                    user.Telefono,
                    nombreImagenFinal
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
            Usuario usuario = await this.repo.GetUsuarioByIdAsync(id);
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado para cambiar estado." });
            }

            await this.repo.UpdateEstadoUsuarioAsync(id);
            return Ok(new { mensaje = "Estado del usuario actualizado correctamente." });
        }
    }
}
