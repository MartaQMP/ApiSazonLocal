using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private IRepository repo;

        public PedidosController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Pedido>>> GetPedidos()
        {
            var pedidos = await this.repo.GetPedidosAsync();
            return Ok(pedidos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int id)
        {
            var pedido = await this.repo.GetPedidoByIdAsync(id);
            if (pedido == null)
            {
                return NotFound(new { mensaje = $"El pedido con ID {id} no existe." });
            }
            return Ok(pedido);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<Pedido>>> GetPedidosUsuario(int idUsuario)
        {
            var pedidos = await this.repo.GetPedidosUsuarioAsync(idUsuario);
            return Ok(pedidos);
        }

        [HttpGet]
        [Route("[action]/{idUsuario}")]
        public async Task<ActionResult<List<Pedido>>> GetPedidosProductosPendientes(int idUsuario)
        {
            var pedidos = await this.repo.GetPedidosProductosPendientesAsync(idUsuario);
            return Ok(pedidos);
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromQuery] int idUsuario,
            [FromQuery] int idDireccion)
        {
            try
            {
                int idPedido = await this.repo.CrearPedidoAsync(idUsuario, idDireccion);
                return Ok(new { mensaje = "Pedido creado correctamente.", idPedido = idPedido });
            }
            catch (Exception)
            {
                return BadRequest("Error al crear el pedido.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}/{nuevoEstado}")]
        public async Task<ActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var pedido = await this.repo.GetPedidoByIdAsync(id);
            if (pedido == null)
            {
                return NotFound(new { mensaje = "Pedido no encontrado." });
            }

            try
            {
                await this.repo.CambiarEstadoPedido(id, nuevoEstado);
                return Ok(new { mensaje = "Estado del pedido actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }

        [HttpGet]
        [Route("[action]/{idPedido}")]
        public async Task<ActionResult<List<DetallePedido>>> GetDetalles(int idPedido)
        {
            var detalles = await this.repo.GetDetallePedidosByPedidoAsync(idPedido);
            return Ok(detalles);
        }

        [HttpGet]
        [Route("[action]/{idDetalle}")]
        public async Task<ActionResult<DetallePedido>> GetDetalle(int idDetalle)
        {
            var detalle = await this.repo.GetDetallePedidoByIdAsync(idDetalle);
            if (detalle == null)
            {
                return NotFound(new { mensaje = "Detalle de pedido no encontrado." });
            }
            return Ok(detalle);
        }

        [HttpPut]
        [Route("[action]/{idDetalle}")]
        public async Task<ActionResult> CambiarEstadoDetalle(int idDetalle)
        {
            var detalle = await this.repo.GetDetallePedidoByIdAsync(idDetalle);
            if (detalle == null)
            {
                return NotFound(new { mensaje = "Detalle de pedido no encontrado." });
            }

            try
            {
                await this.repo.CambiarEstadoDetalleProductoAsync(idDetalle);
                return Ok(new { mensaje = "Estado del detalle actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado del detalle.");
            }
        }
    }
}
