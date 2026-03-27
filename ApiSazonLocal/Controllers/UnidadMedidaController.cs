using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnidadMedidaController : ControllerBase
    {
        private IRepository repo;

        public UnidadMedidaController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<UnidadMedida>>> GetUnidadesMedida()
        {
            var unidades = await this.repo.GetUnidadesMedidaAsync();
            return Ok(unidades);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UnidadMedida>> GetUnidadMedida(int id)
        {
            var unidad = await this.repo.GetUnidadMedidaByIdAsync(id);
            if (unidad == null)
            {
                return NotFound(new { mensaje = $"La unidad de medida con ID {id} no existe." });
            }
            return Ok(unidad);
        }

        [HttpPost]
        [Route("[action]/{nombre}")]
        public async Task<ActionResult> Insertar(string nombre)
        {
            try
            {
                await this.repo.InsertarUnidadMedidaAsync(nombre);
                return Ok(new { mensaje = "Unidad de medida creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la unidad de medida.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            var unidad = await this.repo.GetUnidadMedidaByIdAsync(id);
            if (unidad == null)
            {
                return NotFound(new { mensaje = "Unidad de medida no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoUnidadMedidaAsync(id);
                return Ok(new { mensaje = "Estado de la unidad de medida actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }
    }
}
