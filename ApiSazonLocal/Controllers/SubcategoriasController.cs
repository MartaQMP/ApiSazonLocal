using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubcategoriasController : ControllerBase
    {
        private IRepository repo;

        public SubcategoriasController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Subcategoria>>> GetSubcategorias()
        {
            var subcategorias = await this.repo.GetSubcategoriasAsync();
            return Ok(subcategorias);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Subcategoria>>> GetConCategoria()
        {
            var subcategorias = await this.repo.GetSubcategoriasConCategoriaAsync();
            return Ok(subcategorias);
        }

        [HttpGet]
        [Route("[action]/{idCategoria}")]
        public async Task<ActionResult<List<Subcategoria>>> GetPorCategoria(int idCategoria)
        {
            var subcategorias = await this.repo.GetSubcategoriasByCategoriaAsync(idCategoria);
            return Ok(subcategorias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Subcategoria>> GetSubcategoria(int id)
        {
            var subcategoria = await this.repo.GetSubcategoriaByIdAsync(id);
            if (subcategoria == null)
            {
                return NotFound(new { mensaje = $"La subcategoría con ID {id} no existe." });
            }
            return Ok(subcategoria);
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] string descripcion,
            [FromQuery] string imagen,
            [FromQuery] int idCategoria)
        {
            try
            {
                await this.repo.InsertarSubcategoriaAsync(nombre, descripcion, imagen, idCategoria);
                return Ok(new { mensaje = "Subcategoría creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la subcategoría.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            var subcategoria = await this.repo.GetSubcategoriaByIdAsync(id);
            if (subcategoria == null)
            {
                return NotFound(new { mensaje = "Subcategoría no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoSubcategoriaAsync(id);
                return Ok(new { mensaje = "Estado de la subcategoría actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }
    }
}
