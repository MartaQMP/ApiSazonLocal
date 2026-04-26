using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DireccionesController : ControllerBase
    {
        private IRepository repo;

        public DireccionesController(IRepository repo)
        {
            this.repo = repo;
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

        [HttpGet]
        [Route("[action]/Usuario/{idUsuario}")]
        public async Task<ActionResult<List<Direccion>>> GetDirecciones(int idUsuario)
        {
            var direcciones = await this.repo.GetDireccionesUsuarioAsync(idUsuario);
            return Ok(direcciones);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] DireccionDto direccion)
        {
            try
            {
                await this.repo.InsertarDireccionAsync(
                    direccion.IdUsuario, direccion.NombreEtiqueta, direccion.CalleNumero, direccion.Piso, direccion.Puerta, direccion.CodigoPostal,
                    direccion.Municipio, direccion.Provincia, direccion.NotasAdicionales, direccion.Latitud, direccion.Longitud, direccion.EsPrincipal);
                return Ok(new { mensaje = "Dirección creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la dirección.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] DireccionDto direccion)
        {
            var direccionId = await this.repo.GetDireccionByIdAsync(id);
            if (direccionId == null)
            {
                return NotFound(new { mensaje = "Dirección no encontrada." });
            }

            try
            {
                await this.repo.ActualizarDireccionAsync(
                    id, direccion.NombreEtiqueta, direccion.CalleNumero, direccion.Piso, direccion.Puerta, direccion.CodigoPostal,
                    direccion.Municipio, direccion.Provincia, direccion.NotasAdicionales, direccion.Latitud, direccion.Longitud, direccion.EsPrincipal, direccion.IdUsuario);
                return Ok(new { mensaje = "Dirección actualizada correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la dirección.");
            }
        }

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
