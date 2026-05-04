using ApiSazonLocal.Helpers;
using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public CarritoController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<CarritoItem>>> GetCarritoUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var carrito = await this.repo.GetCarritoUsuarioAsync(usuario.IdUsuario);
            return Ok(carrito);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]/{idProducto}")]
        public async Task<ActionResult<CarritoItem>> GetProductoCarritoUsuario(int idProducto)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            CarritoItem item = await this.repo.GetProductoCarritoAsync(usuario.IdUsuario, idProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }
            return Ok(item);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<decimal>> GetSubtotalCarritoUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            decimal subtotal = await this.repo.GetSubtotalCarrito(usuario.IdUsuario);
            return Ok(new { subtotal = subtotal });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CarritoItemDto carrito)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            await this.repo.InsertarProductoCarritoAsync(carrito.Cantidad, usuario.IdUsuario, carrito.IdProducto);
            return Ok(new { mensaje = "Producto añadido al carrito." });
        }

        [Authorize]
        [HttpPut]
        [Route("[action]")]
        public async Task<ActionResult> ActualizarCantidadProductoCarrito([FromBody] CarritoItemDto carrito)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            CarritoItem item = await this.repo.GetProductoCarritoAsync(usuario.IdUsuario, carrito.IdProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }

            try
            {
                await this.repo.ActualizarCantidadCarritoAsync(usuario.IdUsuario, carrito.IdProducto, carrito.Cantidad);
                return Ok(new { mensaje = "Cantidad actualizada." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la cantidad.");
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("[action]/{idProducto}")]
        public async Task<ActionResult> EliminarProductoCarrito(int idProducto)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            CarritoItem item = await this.repo.GetProductoCarritoAsync(usuario.IdUsuario, idProducto);
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado en el carrito." });
            }

            try
            {
                await this.repo.EliminarProductoCarritoAsync(usuario.IdUsuario, idProducto);
                return Ok(new { mensaje = "Producto eliminado del carrito." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al eliminar el producto.");
            }
        }

        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult> EliminarCarritoUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            try
            {
                await this.repo.EliminarCarritoUsuarioAsync(usuario.IdUsuario);
                return Ok(new { mensaje = "Carrito vaciado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al vaciar el carrito.");
            }
        }
    }
}
