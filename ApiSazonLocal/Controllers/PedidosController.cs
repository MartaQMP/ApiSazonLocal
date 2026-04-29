using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using ApiSazonLocal.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public PedidosController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
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

        [Authorize]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Pedido>>> GetPedidosUsuario()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var pedidos = await this.repo.GetPedidosUsuarioAsync(usuario.IdUsuario);
            return Ok(pedidos);
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Pedido>>> GetPedidosProductosPendientes()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            var pedidos = await this.repo.GetPedidosProductosPendientesAsync(usuario.IdUsuario);
            return Ok(pedidos);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] PedidoDto pedido)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            try
            {
                int idPedido = await this.repo.CrearPedidoAsync(usuario.IdUsuario, pedido.IdDireccion);
                return Ok(new { mensaje = "Pedido creado correctamente.", idPedido = idPedido });
            }
            catch (Exception)
            {
                return BadRequest("Error al crear el pedido.");
            }
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPut]
        [Route("[action]/{id}/{nuevoEstado}")]
        public async Task<ActionResult> CambiarEstadoPedido(int id, string nuevoEstado)
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

        [Authorize]
        [HttpGet]
        [Route("[action]/{idPedido}")]
        public async Task<ActionResult<List<DetallePedido>>> GetDetallesPedido(int idPedido)
        {
            var detalles = await this.repo.GetDetallePedidosByPedidoAsync(idPedido);
            return Ok(detalles);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]/{idDetalle}")]
        public async Task<ActionResult<DetallePedido>> GetDetallePedido(int idDetalle)
        {
            var detalle = await this.repo.GetDetallePedidoByIdAsync(idDetalle);
            if (detalle == null)
            {
                return NotFound(new { mensaje = "Detalle de pedido no encontrado." });
            }
            return Ok(detalle);
        }

        [Authorize(Roles = "AGRICULTOR")]
        [HttpPut]
        [Route("[action]/{idDetalle}")]
        public async Task<ActionResult> CambiarEstadoDetallePedido(int idDetalle)
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
