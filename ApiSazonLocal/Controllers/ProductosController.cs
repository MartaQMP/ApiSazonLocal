using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using Microsoft.AspNetCore.Authorization;
using ApiSazonLocal.Helpers;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public ProductosController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
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
            var result = await this.repo.GetProductosFiltroAsync(
                posicion, buscador, idCategoria, idSubcategoria, idFinca, precio);
            return Ok(result);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Producto>>> GetProductosUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var productos = await this.repo.GetProductosUsuarioAsync(usuario.IdUsuario);
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> FindProducto(int id)
        {
            var producto = await this.repo.GetProductoByIdAsync(id);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpPost]
        public async Task<ActionResult> Post(ProductoDto producto)
        {
            try
            {
                await this.repo.InsertarProductoAsync(
                    producto.Nombre, producto.Descripcion, producto.Imagen,
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
