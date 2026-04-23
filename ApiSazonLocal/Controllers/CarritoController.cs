using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoController : ControllerBase
    {
        private IRepository repo;

        public CarritoController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<CarritoItem>>> GetCarritoUsuario(int idUsuario)
        {
            var carrito = await this.repo.GetCarritoUsuarioAsync(idUsuario);
            return Ok(carrito);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}/{idProducto}")]
        public async Task<ActionResult<CarritoItem>> GetProductoCarrito(int idUsuario, int idProducto)
        {
            var item = await this.repo.GetProductoCarritoAsync(idUsuario, idProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }
            return Ok(item);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<decimal>> GetSubtotal(int idUsuario)
        {
            decimal subtotal = await this.repo.GetSubtotalCarrito(idUsuario);
            return Ok(new { subtotal = subtotal });
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] int cantidad,
            [FromQuery] int idUsuario,
            [FromQuery] int idProducto)
        {
            try
            {
                await this.repo.InsertarProductoCarritoAsync(cantidad, idUsuario, idProducto);
                return Ok(new { mensaje = "Producto añadido al carrito." });
            }
            catch (Exception)
            {
                return BadRequest("Error al añadir el producto al carrito.");
            }
        }

        [HttpPut]
        [Route("[action]/{idUsuario}/{idProducto}/{nuevaCantidad}")]
        public async Task<ActionResult> ActualizarCantidad(int idUsuario, int idProducto, int nuevaCantidad)
        {
            var item = await this.repo.GetProductoCarritoAsync(idUsuario, idProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }

            try
            {
                await this.repo.ActualizarCantidadCarritoAsync(idUsuario, idProducto, nuevaCantidad);
                return Ok(new { mensaje = "Cantidad actualizada." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la cantidad.");
            }
        }

        [HttpDelete]
        [Route("[action]/{idUsuario}/{idProducto}")]
        public async Task<ActionResult> EliminarProducto(int idUsuario, int idProducto)
        {
            var item = await this.repo.GetProductoCarritoAsync(idUsuario, idProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }

            try
            {
                await this.repo.EliminarProductoCarritoAsync(idUsuario, idProducto);
                return Ok(new { mensaje = "Producto eliminado del carrito." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al eliminar el producto.");
            }
        }

        [HttpDelete]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult> EliminarCarrito(int idUsuario)
        {
            try
            {
                await this.repo.EliminarCarritoUsuarioAsync(idUsuario);
                return Ok(new { mensaje = "Carrito vaciado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al vaciar el carrito.");
            }
        }
    }
}
