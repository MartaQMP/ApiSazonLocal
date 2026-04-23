using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcSazonLocal.Filters;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;
using System.Security.Claims;
using SazonLocalInterfaces.Services;

namespace MvcSazonLocal.Controllers
{
    public class AtencionUsuarioController : Controller
    {
        private readonly SazonApiService serviceApi;
        private readonly IEmailService service;

        public AtencionUsuarioController (SazonApiService serviceApi, IEmailService service)
        {
            this.serviceApi = serviceApi;
            this.service = service;
        }

        #region CONTACTO USUARIO
        public IActionResult Contacto()
        {
            ViewBag.Mensaje = TempData["Mensaje"];
            ViewBag.MensajeError = TempData["MensajeError"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            try
            {
                string claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? idUsuario = null;
                if (claimId != null)
                {
                    idUsuario = int.Parse(claimId);
                }
                if (idUsuario != null)
                {
                    await this.serviceApi.InsertarMensajeAsync(idUsuario, nombre, email, tipoConsulta, asunto, mensaje);
                }
                else
                {
                    await this.serviceApi.InsertarMensajeAsync(null, nombre, email, tipoConsulta, asunto, mensaje);
                }
                TempData["Mensaje"] = "¡Gracias! Tu mensaje ha llegado a la huerta correctamente.";
                return RedirectToAction("Contacto");

            }catch(Exception e)
            {
                TempData["MensajeError"] = "Vaya, algo ha fallado al enviar el mensaje. Inténtalo de nuevo más tarde. "+e;
                return RedirectToAction("Contacto");
            }
        }
        #endregion

        #region BUZÓN DE SUGERENCIAS Y CONTACTO
        [AuthorizeUsuarios(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> BuzonMensajes()
        {
            List<Mensaje> mensajes = await this.serviceApi.GetMensajesAsync();
            ViewBag.Mensaje = TempData["Mensaje"];
            ViewBag.MensajeError = TempData["MensajeError"];
            return View(mensajes);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoLeido(int idMensaje)
        {
            await this.serviceApi.MarcarMensajeLeidoAsync(idMensaje);
            return RedirectToAction("BuzonMensajes");
        }
        #endregion

        #region GESTIÓN DE MENSAJES Y RESPUESTAS
        [AuthorizeUsuarios(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> ResponderMensaje(int idMensaje)
        {
            Mensaje mensaje = await this.serviceApi.GetMensajeByIdAsync(idMensaje);
            if (mensaje != null && !mensaje.EsLeido)
            {
                await this.serviceApi.MarcarMensajeLeidoAsync(idMensaje);
            }
            return View(mensaje);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarRespuestaEmail(string emailDestino, string asuntoRespuesta, string cuerpoRespuesta, int idMensaje)
        {
            try
            {
                string cuerpoHtml = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: #556B2F; padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>Sazón Local</h1>
                            <p style='color: #F5F5DC; margin: 5px 0 0;'>El sabor que el campo nunca perdió</p>
                        </div>
                        <div style='border: 1px solid #556B2F; padding: 30px;'>
                            <h2 style='color: #556B2F;'>Respuesta a tu mensaje</h2>
                            <p style='color: #555; line-height: 1.6;'>Hemos recibido tu consulta y queremos responderte:</p>
                            <div style='background: #F5F5DC; border-left: 4px solid #556B2F; padding: 15px; margin: 20px 0; font-size: 15px; color: #333; line-height: 1.6;'>
                                {cuerpoRespuesta}
                            </div>
                            <p style='color: #888; font-size: 13px;'>Si tienes más preguntas no dudes en contactarnos de nuevo.</p>
                        </div>
                        <div style='background: #f9f9f9; padding: 15px; text-align: center; border-top: 1px solid #ddd;'>
                            <p style='color: #aaa; font-size: 12px; margin: 0;'>© 2026 Sazón Local · sazonlocal.sl@gmail.com</p>
                        </div>
                    </div>";

                await this.service.SendEmailAsync(emailDestino, asuntoRespuesta, cuerpoHtml);
                await this.serviceApi.MarcarComoRespondidoAsync(idMensaje);
                TempData["Mensaje"] = "La respuesta ha sido enviada correctamente a " + emailDestino;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al enviar el email: " + ex.Message;
            }
            return RedirectToAction("BuzonMensajes");
        }

        #endregion
    }
}
