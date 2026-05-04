using ApiSazonLocal.Helpers;
using ApiSazonLocal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;
        private BlobService service;
        private static string[] extensionesValidas = { ".jpg", ".jpeg", ".png" };
        private string containerName = "productos-sl";

        public ProductosController(IRepository repo, HelperToken helper, BlobService service)
        {
            this.repo = repo;
            this.helper = helper;
            this.service = service;
        }

        [HttpGet]
        [Route("GetProductosFiltro")]
        public async Task<ActionResult<ProductosPaginacion>> GetProductosFiltro(
            int posicion,
            [FromQuery] string? buscador,
            [FromQuery] int? idCategoria,
            [FromQuery] int? idSubcategoria,
            [FromQuery] int? idFinca,
            [FromQuery] decimal? precio)
        {
            ProductosPaginacion result = await this.repo.GetProductosFiltroAsync(posicion, buscador, idCategoria, idSubcategoria, idFinca, precio);
            foreach (Producto producto in result.Productos)
            {
                if (!string.IsNullOrEmpty(producto.Imagen))
                {
                    producto.Imagen = this.service.GetBlobSasUrl(containerName, producto.Imagen);
                }
            }
            return Ok(result);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Producto>>> GetProductosUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            List<Producto> productos = await this.repo.GetProductosUsuarioAsync(usuario.IdUsuario);
            foreach (Producto producto in productos)
            {
                if (!string.IsNullOrEmpty(producto.Imagen))
                {
                    producto.Imagen = this.service.GetBlobSasUrl(containerName, producto.Imagen);
                }
            }
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> FindProducto(int id)
        {
            Producto producto = await this.repo.GetProductoByIdAsync(id);
            if (producto == null) return NotFound();
            if (!string.IsNullOrEmpty(producto.Imagen))
            {
                producto.Imagen = this.service.GetBlobSasUrl(containerName, producto.Imagen);
            }
            return Ok(producto);
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpPost]
        public async Task<ActionResult> Post([FromForm] ProductoDto producto, IFormFile imagen)
        {
            UsuarioLogin usuarioLogin = this.helper.GetUsuario();
            try
            {
                string? urlImagen = null;
                if (imagen != null)
                {
                    string extension = Path.GetExtension(imagen.FileName).ToLower();
                    if (extensionesValidas.Contains(extension))
                    {
                        string nombreLimpio = HelperTextCleaner.LimpiarTexto(producto.Nombre);
                        urlImagen = usuarioLogin.IdUsuario + "_" + nombreLimpio + "_" + producto.IdFinca + extension;
                        using (var stream = imagen.OpenReadStream())
                        {
                            await service.UploadBlobAsync(containerName, urlImagen, stream);
                        }
                    }
                }
                await this.repo.InsertarProductoAsync(
                    producto.Nombre, producto.Descripcion, urlImagen,
                    producto.PrecioUnidad, producto.IdUnidadMedida, producto.Stock,
                    producto.EstaActivo, producto.IdFinca, producto.IdCategoria,
                    producto.IdSubcategoria);
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar el producto.");
            }
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpPut]
        [Route("[action]/{id}/{precio}/{stock}")]
        public async Task<ActionResult> ActualizarDatosProducto(int id, decimal precio, int stock)
        {
            await this.repo.ActualizarProductoAsync(id, precio, stock);
            return Ok(new { mensaje = "Stock y precio actualizados" });
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstadoProducto(int id)
        {
            await this.repo.CambiarEstadoProductoAsync(id);
            return Ok();
        }

        [HttpPut]
        [Route("[action]/{id}/{cantidad}")]
        public async Task<ActionResult> ActualizarStockProducto(int id, int cantidad)
        {
            await this.repo.ActualizarStockCompraAsync(id, cantidad);
            return Ok();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<ActionResult<int>> GetStockProducto(int id)
        {
            int stock = await this.repo.GetStockProductoAsync(id);
            return Ok(stock);
        }
    }
}
