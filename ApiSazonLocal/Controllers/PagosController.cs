using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagosController : ControllerBase
    {
        private IRepository repo;

        public PagosController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] PagoDto pago)
        {
            try
            {
                await this.repo.InsertarPagoUsuarioAsync(pago.IdPedido, pago.Pasarela, pago.MetodoPago, pago.UltimosDigitosTarjeta, pago.EstadoPago, pago.TransactionId);
                return Ok(new { mensaje = "Pago registrado correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al registrar el pago.");
            }
        }
    }
}
