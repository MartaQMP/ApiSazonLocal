using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

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
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<Direccion>>> GetDireccionesUsuario(int idUsuario)
        {
            var direcciones = await this.repo.GetDireccionesUsuarioAsync(idUsuario);
            return Ok(direcciones);
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] int idUsuario,
            [FromQuery] string? etiqueta,
            [FromQuery] string calleNumero,
            [FromQuery] string? piso,
            [FromQuery] string? puerta,
            [FromQuery] string cp,
            [FromQuery] string municipio,
            [FromQuery] string provincia,
            [FromQuery] string? notasAdicionales,
            [FromQuery] decimal latitud,
            [FromQuery] decimal longitud,
            [FromQuery] bool esPrincipal)
        {
            try
            {
                await this.repo.InsertarDireccionAsync(
                    idUsuario, etiqueta, calleNumero, piso, puerta, cp,
                    municipio, provincia, notasAdicionales, latitud, longitud, esPrincipal);
                return Ok(new { mensaje = "Dirección creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la dirección.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Actualizar(
            int id,
            [FromQuery] string? etiqueta,
            [FromQuery] string calleNumero,
            [FromQuery] string? piso,
            [FromQuery] string? puerta,
            [FromQuery] string cp,
            [FromQuery] string municipio,
            [FromQuery] string provincia,
            [FromQuery] string? notasAdicionales,
            [FromQuery] decimal latitud,
            [FromQuery] decimal longitud,
            [FromQuery] bool esPrincipal,
            [FromQuery] int idUsuario)
        {
            var direccion = await this.repo.GetDireccionByIdAsync(id);
            if (direccion == null)
            {
                return NotFound(new { mensaje = "Dirección no encontrada." });
            }

            try
            {
                await this.repo.ActualizarDireccionAsync(
                    id, etiqueta, calleNumero, piso, puerta, cp,
                    municipio, provincia, notasAdicionales, latitud, longitud, esPrincipal, idUsuario);
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
