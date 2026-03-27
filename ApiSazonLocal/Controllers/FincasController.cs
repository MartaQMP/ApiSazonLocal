using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FincasController : ControllerBase
    {
        private IRepository repo;

        public FincasController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasActivas()
        {
            var fincas = await this.repo.GetFincasActivasAsync();
            return Ok(fincas);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasPendientes()
        {
            var fincas = await this.repo.GetFincasPendientesAsync();
            return Ok(fincas);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasRechazadas()
        {
            var fincas = await this.repo.GetFincasRechazadasAsync();
            return Ok(fincas);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<Finca>>> GetFincasUsuario(int idUsuario)
        {
            var fincas = await this.repo.GetFincasUsuarioAsync(idUsuario);
            return Ok(fincas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Finca>> GetFinca(int id)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = $"La finca con ID {id} no existe." });
            }
            return Ok(finca);
        }

        [HttpGet]
        [Route("[action]/{estado}")]
        public async Task<ActionResult<List<Finca>>> GetFincasAdmin(int? estado)
        {
            var fincas = await this.repo.GetFincasAdminAsync(estado);
            return Ok(fincas);
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] string direccion,
            [FromQuery] string municipio,
            [FromQuery] string provincia,
            [FromQuery] decimal latitud,
            [FromQuery] decimal longitud,
            [FromQuery] int idUsuario)
        {
            try
            {
                await this.repo.InsertarFincaAsync(0, nombre, direccion, municipio, provincia, latitud, longitud, idUsuario);
                return Ok(new { mensaje = "Finca creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la finca.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> Actualizar(
            int id,
            [FromQuery] string nombre,
            [FromQuery] string direccion,
            [FromQuery] string municipio,
            [FromQuery] string provincia,
            [FromQuery] decimal latitud,
            [FromQuery] decimal longitud,
            [FromQuery] int idUsuario)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.ActualizarFincaAsync(id, nombre, direccion, municipio, provincia, latitud, longitud, idUsuario);
                return Ok(new { mensaje = "Finca actualizada correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la finca.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoFincaAsync(id);
                return Ok(new { mensaje = "Estado de la finca actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}/{nuevoEstado}")]
        public async Task<ActionResult> CambiarEstadoValidacion(int id, int nuevoEstado)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoValidacionFincaAsync(id, nuevoEstado);
                return Ok(new { mensaje = "Estado de validación actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado de validación.");
            }
        }
    }
}
