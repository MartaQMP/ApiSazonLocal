using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MensajesController : ControllerBase
    {
        private IRepository repo;

        public MensajesController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Mensaje>>> GetMensajes()
        {
            var mensajes = await this.repo.GetMensajesAsync();
            return Ok(mensajes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Mensaje>> GetMensaje(int id)
        {
            var mensaje = await this.repo.GetMensajeByIdAsync(id);
            if (mensaje == null)
            {
                return NotFound(new { mensaje = $"El mensaje con ID {id} no existe." });
            }
            return Ok(mensaje);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] MensajeDto mensaje)
        {
            try
            {
                await this.repo.InsertarMensajeAsync(mensaje.IdUsuario, mensaje.Nombre, mensaje.Email, mensaje.TipoConsulta, mensaje.Asunto, mensaje.Contenido);
                return Ok(new { mensaje = "Mensaje enviado correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al enviar el mensaje.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> MarcarLeido(int id)
        {
            var mensaje = await this.repo.GetMensajeByIdAsync(id);
            if (mensaje == null)
            {
                return NotFound(new { mensaje = "Mensaje no encontrado." });
            }

            try
            {
                await this.repo.MarcarMensajeLeidoAsync(id);
                return Ok(new { mensaje = "Mensaje marcado como leído." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al marcar el mensaje como leído.");
            }
        }

        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> MarcarRespondido(int id)
        {
            var mensaje = await this.repo.GetMensajeByIdAsync(id);
            if (mensaje == null)
            {
                return NotFound(new { mensaje = "Mensaje no encontrado." });
            }

            try
            {
                await this.repo.MarcarComoRespondidoAsync(id);
                return Ok(new { mensaje = "Mensaje marcado como respondido." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al marcar el mensaje como respondido.");
            }
        }
    }
}
