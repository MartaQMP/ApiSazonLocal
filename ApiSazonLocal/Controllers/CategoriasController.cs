using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using Microsoft.AspNetCore.Authorization;

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
            List<Categoria> categorias = await this.repo.GetCategoriasAsync();
            return Ok(categorias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria>> GetCategoria(int id)
        {
            Categoria categoria = await this.repo.GetCategoriaByIdAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = $"La categoría con ID {id} no existe." });
            }
            return Ok(categoria);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CategoriaDto categoria)
        {
            await this.repo.InsertarCategoriaAsync(categoria.Nombre, categoria.Descripcion);
            return Ok(new { mensaje = "Categoría creada correctamente." });
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstadoCategoria(int id)
        {
            await this.repo.CambiarEstadoCategoriaAsync(id);
            return Ok(new { mensaje = "Estado de la categoría actualizado." });
        }
    }
}
