using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private IRepository repo;

        public CategoriasController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Categoria>>> GetCategorias()
        {
            var categorias = await this.repo.GetCategoriasAsync();
            return Ok(categorias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria>> GetCategoria(int id)
        {
            var categoria = await this.repo.GetCategoriaByIdAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = $"La categoría con ID {id} no existe." });
            }
            return Ok(categoria);
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] string descripcion)
        {
            try
            {
                await this.repo.InsertarCategoriaAsync(nombre, descripcion);
                return Ok(new { mensaje = "Categoría creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la categoría.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            var categoria = await this.repo.GetCategoriaByIdAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoCategoriaAsync(id);
                return Ok(new { mensaje = "Estado de la categoría actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }
    }
}
