using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private IRepository repo;

        public ProductosController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<ProductosPaginacion>> GetProductosFiltro(
            [FromQuery] int posicion,
            [FromQuery] string? buscador,
            [FromQuery] int? idCategoria,
            [FromQuery] int? idSubcategoria,
            [FromQuery] int? idFinca,
            [FromQuery] decimal? precio)
        {
            var result = await this.repo.GetProductosFiltroAsync(posicion, buscador, idCategoria, idSubcategoria, idFinca, precio);
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<Producto>>> Usuario(int idUsuario)
        {
            var productos = await this.repo.GetProductosUsuarioAsync(idUsuario);
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> FindProducto(int id)
        {
            var producto = await this.repo.GetProductoByIdAsync(id);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        public async Task<ActionResult> Post(Producto producto)
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

        [HttpPut]
        [Route("[action]/{id}/{precio}/{stock}")]
        public async Task<ActionResult> ActualizarDatos(int id, decimal precio, int stock)
        {
            await this.repo.ActualizarProductoAsync(id, precio, stock);
            return Ok(new { mensaje = "Stock y precio actualizados" });
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            await this.repo.CambiarEstadoProductoAsync(id);
            return Ok();
        }

        [HttpPut]
        [Route("[action]/{id}/{cantidad}")]
        public async Task<ActionResult> ActualizarStockCompra(int id, int cantidad)
        {
            await this.repo.ActualizarStockCompraAsync(id, cantidad);
            return Ok();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<ActionResult<int>> GetStock(int id)
        {
            int stock = await this.repo.GetStockProductoAsync(id);
            return Ok(stock);
        }
    }
}
