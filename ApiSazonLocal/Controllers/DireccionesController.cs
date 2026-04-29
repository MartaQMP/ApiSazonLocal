using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using ApiSazonLocal.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DireccionesController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public DireccionesController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Direccion>> GetDireccion(int id)
        {
            var direccion = await this.repo.GetDireccionByIdAsync(id);
            if (direccion == null)
            {
                return NotFound(new { mensaje = $"La dirección con ID {id} no existe." });
            }
            return Ok(direccion);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]/Usuario")]
        public async Task<ActionResult<List<Direccion>>> GetDirecciones()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var direcciones = await this.repo.GetDireccionesUsuarioAsync(usuario.IdUsuario);
            return Ok(direcciones);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] DireccionDto direccion)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            try
            {
                await this.repo.InsertarDireccionAsync(
                    usuario.IdUsuario, direccion.NombreEtiqueta, direccion.CalleNumero, direccion.Piso, direccion.Puerta, direccion.CodigoPostal,
                    direccion.Municipio, direccion.Provincia, direccion.NotasAdicionales, direccion.Latitud, direccion.Longitud, direccion.EsPrincipal);
                return Ok(new { mensaje = "Dirección creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la dirección.");
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] DireccionDto direccion)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var direccionId = await this.repo.GetDireccionByIdAsync(id);
            if (direccionId == null)
            {
                return NotFound(new { mensaje = "Dirección no encontrada." });
            }

            try
            {
                await this.repo.ActualizarDireccionAsync(
                    id, direccion.NombreEtiqueta, direccion.CalleNumero, direccion.Piso, direccion.Puerta, direccion.CodigoPostal,
                    direccion.Municipio, direccion.Provincia, direccion.NotasAdicionales, direccion.Latitud, direccion.Longitud, direccion.EsPrincipal, usuario.IdUsuario);
                return Ok(new { mensaje = "Dirección actualizada correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la dirección.");
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            var direccion = await this.repo.GetDireccionByIdAsync(id);
            if (direccion == null)
            {
                return NotFound(new { mensaje = "Dirección no encontrada." });
            }

            try
            {
                await this.repo.EliminarDireccionAsync(id);
                return Ok(new { mensaje = "Dirección eliminada correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al eliminar la dirección.");
            }
        }
    }
}
