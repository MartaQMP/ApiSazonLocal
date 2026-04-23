using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Repositories;

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
        public async Task<ActionResult> Post(
            [FromQuery] int idPedido,
            [FromQuery] string pasarela,
            [FromQuery] string metodo,
            [FromQuery] string ultimosDigitos,
            [FromQuery] string estado,
            [FromQuery] string transactionId)
        {
            try
            {
                await this.repo.InsertarPagoUsuarioAsync(idPedido, pasarela, metodo, ultimosDigitos, estado, transactionId);
                return Ok(new { mensaje = "Pago registrado correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al registrar el pago.");
            }
        }
    }
}
