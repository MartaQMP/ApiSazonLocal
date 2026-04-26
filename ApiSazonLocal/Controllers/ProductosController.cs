using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;

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
        [Route("[action]/{posicion}")]
        public async Task<ActionResult<ProductosPaginacion>> GetProductosBuscador(int posicion, [FromBody] ProductoBuscadorDto producto)
        {
            var result = await this.repo.GetProductosFiltroAsync(posicion, producto.Buscador, producto.IdCategoria, producto.IdSubcategoria, producto.IdFinca, producto.Precio);
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/Usuario/{idUsuario}")]
        public async Task<ActionResult<List<Producto>>> GetProductos(int idUsuario)
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

        [HttpPut]
        [Route("[action]/{id}/{precio}/{stock}")]
        public async Task<ActionResult> ActualizarDatosProducto(int id, decimal precio, int stock)
        {
            await this.repo.ActualizarProductoAsync(id, precio, stock);
            return Ok(new { mensaje = "Stock y precio actualizados" });
        }

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
