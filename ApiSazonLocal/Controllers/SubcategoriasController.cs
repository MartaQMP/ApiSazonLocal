using ApiSazonLocal.Repositories;
using ApiSazonLocal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class SubcategoriasController : ControllerBase
    {
        private IRepository repo;
        private BlobService service;
        private static string[] extensionesValidas = { ".jpg", ".jpeg", ".png" };
        private string containerName = "subcategorias-sl";

        public SubcategoriasController(IRepository repo, BlobService service)
        {
            this.repo = repo;
            this.service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<Subcategoria>>> GetSubcategorias()
        {
            List<Subcategoria> subcategorias = await this.repo.GetSubcategoriasAsync();
            foreach (Subcategoria subcategoria in subcategorias)
            {
                if (!string.IsNullOrEmpty(subcategoria.Imagen))
                {
                    subcategoria.Imagen = this.service.GetBlobSasUrl(containerName, subcategoria.Imagen);
                }
            }
            return Ok(subcategorias);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Subcategoria>>> GetSubcategoriaConCategoria()
        {
            List<Subcategoria> subcategorias = await this.repo.GetSubcategoriasConCategoriaAsync();
            foreach (Subcategoria subcategoria in subcategorias)
            {
                if (!string.IsNullOrEmpty(subcategoria.Imagen))
                {
                    subcategoria.Imagen = this.service.GetBlobSasUrl(containerName, subcategoria.Imagen);
                }
            }
            return Ok(subcategorias);
        }

        [HttpGet]
        [Route("[action]/{idCategoria}")]
        public async Task<ActionResult<List<Subcategoria>>> GetSubcategoriaPorCategoria(int idCategoria)
        {
            List<Subcategoria> subcategorias = await this.repo.GetSubcategoriasByCategoriaAsync(idCategoria);
            foreach (Subcategoria subcategoria in subcategorias)
            {
                if (!string.IsNullOrEmpty(subcategoria.Imagen))
                {
                    subcategoria.Imagen = this.service.GetBlobSasUrl(containerName, subcategoria.Imagen);
                }
            }
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
            if (!string.IsNullOrEmpty(subcategoria.Imagen))
            {
                subcategoria.Imagen = this.service.GetBlobSasUrl(containerName, subcategoria.Imagen);
            }
            return Ok(subcategoria);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        public async Task<ActionResult> Post([FromForm] SubcategoriaDto subcategoria, IFormFile imagen)
        {
            try
            {
                string? urlImagen = null;
                if (imagen != null)
                {
                    string extension = Path.GetExtension(imagen.FileName).ToLower();
                    if (extensionesValidas.Contains(extension))
                    {
                        string nombreLimpio = HelperTextCleaner.LimpiarTexto(subcategoria.Nombre);
                        urlImagen = nombreLimpio + extension;
                        using (var stream = imagen.OpenReadStream())
                        {
                            await service.UploadBlobAsync(containerName, urlImagen, stream);
                        }
                    }
                }
                await this.repo.InsertarSubcategoriaAsync(subcategoria.Nombre, subcategoria.Descripcion, urlImagen, subcategoria.IdCategoria);
                return Ok(new { mensaje = "Subcategoría creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la subcategoría.");
            }
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstadoSubcategoria(int id)
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
