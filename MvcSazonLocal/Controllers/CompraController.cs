using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcSazonLocal.Extensions;
using MvcSazonLocal.Filters;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;
using SazonLocalInterfaces.Services;
using Stripe;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcSazonLocal.Controllers
{
    public class CompraController : Controller
    {
        private readonly SazonApiService serviceApi;
        private readonly HelperPath helper;
        private readonly IEmailService emailService;
        private readonly IPdfService pdfService;

        public CompraController(SazonApiService serviceApi, HelperPath helper, IEmailService emailService, IPdfService pdfService)
        {
            this.serviceApi = serviceApi;
            this.helper = helper;
            this.emailService = emailService;
            this.pdfService = pdfService;
        }

        #region CARRITO
        private async Task<List<CarritoItem>> CargarCarritoAsync()
        {
            string claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }
            List<CarritoItem> listaCarrito = new List<CarritoItem>();

            if (idUsuario == null)
            {
                Dictionary<int, int> carritoSession = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO") ?? new Dictionary<int, int>();
                foreach (var producto in carritoSession)
                {
                    Producto pro = await this.serviceApi.GetProductoByIdAsync(producto.Key);
                    listaCarrito.Add(new CarritoItem
                    {
                        IdProducto = producto.Key,
                        Cantidad = producto.Value,
                        IdUsuario = null,
                        Producto = pro
                    });
                }
            }
            else
            {
                listaCarrito = await this.serviceApi.GetCarritoUsuarioAsync(idUsuario.Value);
            }

            foreach (CarritoItem item in listaCarrito)
            {
                if (!string.IsNullOrEmpty(item.Producto.Imagen))
                    item.Producto.Imagen = this.helper.MapUrlPath(item.Producto.Imagen, Folders.Productos);
                else
                    item.Producto.Imagen = this.helper.MapUrlPath(item.Producto.Subcategoria.Imagen, Folders.Subcategorias);
            }
            List<CarritoItem> insuficientes = listaCarrito.Where(c => c.Producto.Stock > 0 && c.Producto.Stock < c.Cantidad).ToList();
            List<CarritoItem> agotados = listaCarrito.Where(c => c.Producto.Stock <= 0).ToList();

            ViewBag.Insuficientes = insuficientes;
            ViewBag.Agotados = agotados;
            List<CarritoItem> disponibles = listaCarrito.Where(c => c.Producto.Stock > 0 && c.Producto.Stock >= c.Cantidad).ToList();
            return disponibles;
        }

        public async Task<IActionResult> Carrito()
        {
            List<CarritoItem> disponibles = await this.CargarCarritoAsync();
            return View(disponibles);
        }

        public async Task<IActionResult> _EliminarProductoCarrito(int idProducto)
        {
            string claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }

            if (idUsuario == null)
            {
                var carrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
                if (carrito != null && carrito.ContainsKey(idProducto))
                {
                    carrito.Remove(idProducto);
                    HttpContext.Session.SetObject("CARRITO", carrito);
                }
            }
            else
            {
                await this.serviceApi.EliminarProductoCarritoAsync(idUsuario.Value, idProducto);
            }

            List<CarritoItem> disponibles = await CargarCarritoAsync();
            return PartialView("_PartialCarrito", disponibles);
        }

        public async Task<IActionResult> _ActualizarCantidadCarrito(int idProducto, int cantidad)
        {
            string claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }

            if (idUsuario == null)
            {
                var carrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
                if (carrito != null && carrito.ContainsKey(idProducto))
                {
                    carrito[idProducto] = cantidad;
                    HttpContext.Session.SetObject("CARRITO", carrito);
                }
            }
            else
            {
                await this.serviceApi.ActualizarCantidadCarritoAsync(idUsuario.Value, idProducto, cantidad);
            }
            List<CarritoItem> disponibles = await CargarCarritoAsync();
            return PartialView("_PartialCarrito", disponibles);
        }
        #endregion

        #region CHECKOUT
        [AuthorizeUsuarios]
        public async Task<IActionResult> Checkout()
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            List<CarritoItem> items = await this.CargarCarritoAsync();
            List<Direccion> direcciones = await this.serviceApi.GetDireccionesUsuarioAsync(idUsuario);
            decimal subtotal = await this.serviceApi.GetSubtotalCarrito(idUsuario);
            decimal envio = subtotal >= 50 ? 0 : 3.99m;
            decimal tasaGestion = subtotal * 0.05m;
            decimal total = subtotal + tasaGestion + envio;

            ViewBag.Subtotal = subtotal;
            ViewBag.Tasa = tasaGestion;
            ViewBag.Envio = envio;
            ViewBag.Total = total;
            ViewBag.CantidadProductos = items.Sum(i => i.Cantidad);
            ViewBag.ErrorMensaje = TempData["ErrorMensaje"];
            return View(direcciones);
        }

        private async Task<(decimal Total, List<string> Errores)> ValidarStockYCalcularTotal(List<CarritoItem> items)
        {
            decimal subtotal = 0;
            List<string> errores = new List<string>();

            foreach (var item in items)
            {
                subtotal += item.Cantidad * item.Producto.PrecioUnidad;
                int stockReal = await this.serviceApi.GetStockProductoAsync(item.IdProducto);

                if (stockReal < item.Cantidad)
                {
                    errores.Add($"• {item.Producto.Nombre}: solo quedan {stockReal} unidades (pediste {item.Cantidad}).");
                }
            }
            decimal tasaGestion = subtotal * 0.05m; 
            decimal gastosEnvio = (subtotal < 50) ? 3.99m : 0;
            decimal total = subtotal + tasaGestion + gastosEnvio;

            return (total, errores);
        }

        private async Task<(string Status, string TransactionId)> ProcesarPagoStripe(decimal total, string methodId)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(total * 100),
                Currency = "eur",
                PaymentMethod = methodId,
                Confirm = true,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true, AllowRedirects = "never" }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(options);
            return (intent.Status, intent.Id);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPedido(int idDireccion, string metodoPago, string transactionId, string ultimosDigitos, string estadoPago)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            List<CarritoItem> items = await this.serviceApi.GetCarritoUsuarioAsync(idUsuario);
            var (totalPedido, erroresStock) = await ValidarStockYCalcularTotal(items);

            if (erroresStock.Count > 0)
            {
                TempData["ErrorMensaje"] = "Hay problemas con el stock de algunos productos:<br/>" +
                                           string.Join("<br/>", erroresStock);

                return RedirectToAction("Checkout");
            }

            if (idDireccion <= 0)
            {
                TempData["ErrorMensaje"] = "No has seleccionado ninguna dirección de envío.";
                return View();
            }

            if (metodoPago.ToLower() == "tarjeta")
            {
                try
                {
                    var resultadoStripe = await ProcesarPagoStripe(totalPedido, transactionId);

                    if (resultadoStripe.Status != "succeeded")
                    {
                        TempData["ErrorMensaje"] = "Stripe no pudo procesar el pago.";
                        return RedirectToAction("Checkout");
                    }
                    estadoPago = "COMPLETADO";
                    transactionId = resultadoStripe.TransactionId;
                }
                catch (StripeException ex)
                {
                    TempData["ErrorMensaje"] = "Error en la pasarela: " + ex.Message;
                    return RedirectToAction("Checkout");
                }
            }

            int idPedido = await this.serviceApi.CrearPedidoAsync(idUsuario, idDireccion);
            if (idPedido <= 0)
            {
                TempData["ErrorMensaje"] = "No se ha podido registrar el pedido en el sistema. Inténtalo de nuevo.";
                return View();
            }
            if (metodoPago.ToLower() == "tarjeta")
            {
                await this.serviceApi.InsertarPagoUsuarioAsync(idPedido, "STRIPE", "TARJETA", ultimosDigitos, estadoPago, transactionId);
            }
            await this.serviceApi.EliminarCarritoUsuarioAsync(idUsuario);
            return RedirectToAction("Confirmacion", new { idPedido = idPedido });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDireccion(string? nombreEtiqueta, string calleNumero, string? piso, string? puerta, string codigoPostal, string municipio, string provincia, string? notasAdicionales, bool esPrincipal)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var coordenadas = await HelperMap.GetCoordenadasAsync(calleNumero, municipio, provincia);
            if (coordenadas.latitud == 0 && coordenadas.longitud == 0)
            {
                TempData["ErrorMensaje"] = "Ha habido un error calculando las coordenadas revisa la direccion.";
            }
            await this.serviceApi.InsertarDireccionAsync(idUsuario, nombreEtiqueta, calleNumero, piso, puerta, codigoPostal, municipio, provincia, notasAdicionales, coordenadas.latitud, coordenadas.longitud, esPrincipal);
            return RedirectToAction("Checkout");
        }
        #endregion

        #region CONFIRMACION
        [AuthorizeUsuarios]
        public async Task<IActionResult> Confirmacion(int idPedido)
        {
            Pedido pedido = await this.serviceApi.GetPedidoByIdAsync(idPedido);
            byte[] pdfBytes = pdfService.GenerarFacturaPedido(pedido);
            bool emailEnviado = HttpContext.Session.GetObject<bool>("EMAIL");

            if (!emailEnviado)
            {
                string asunto = $"Confirmación Pedido #{pedido.IdPedido}";
                string cuerpoHtml = $@"
                    <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 20px auto; border: 1px solid #E0E0E0; border-radius: 12px; overflow: hidden; background-color: #FAF9F6; box-shadow: 0 4px 15px rgba(0,0,0,0.08);'>
    
                        <div style='background-color: #556B2f; padding: 30px 20px; text-align: center;'>
                            <h1 style='color: #FAF9F6; margin: 0; font-size: 28px; letter-spacing: 2px; text-transform: uppercase;'>SAZÓN LOCAL</h1>
                            <p style='color: #8FBC8F; margin: 5px 0 0 0; font-size: 14px;'>El sabor que el campo nunca perdió</p>
                        </div>

                        <div style='padding: 40px; background-color: #ffffff;'>
                            <h2 style='color: #2F3E1B; margin-top: 0; font-size: 22px;'>¡Hola, {pedido.Cliente.Nombre}!</h2>
        
                            <p style='color: #2F2F2F; font-size: 16px; line-height: 1.6;'>
                                Tu pedido ha sido confirmado con éxito. Ya estamos seleccionando los mejores productos para que los recibas muy pronto.
                            </p>

                            <div style='background-color: #F5F5DC; border-left: 5px solid #A0522D; padding: 20px; margin: 25px 0;'>
                                <h4 style='color: #A0522D; margin: 0 0 10px 0; text-transform: uppercase; font-size: 13px;'>Resumen de confirmación</h4>
                                <table style='width: 100%; color: #555555; font-size: 15px;'>
                                    <tr>
                                        <td style='padding: 4px 0;'><strong>Nº Pedido:</strong></td>
                                        <td style='padding: 4px 0; text-align: right;'>#{pedido.IdPedido}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 4px 0;'><strong>Fecha:</strong></td>
                                        <td style='padding: 4px 0; text-align: right;'>{pedido.FechaPedido:dd/MM/yyyy HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 4px 0;'><strong>Total pagado:</strong></td>
                                        <td style='padding: 4px 0; text-align: right; color: #556B2f; font-weight: bold;'>{pedido.Total:N2}€</td>
                                    </tr>
                                </table>
                            </div>

                            <p style='color: #555555; font-size: 14px; font-style: italic;'>
                                * Te hemos adjuntado la <strong>factura oficial</strong> en formato PDF a este correo electrónico.
                            </p>

                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #E0E0E0;'>
                                <p style='color: #2F2F2F; font-size: 15px; margin-bottom: 5px;'><strong>Dirección de entrega:</strong></p>
                                <p style='color: #666666; font-size: 14px; margin: 0;'>
                                    {pedido.Direccion.CalleNumero}, {pedido.Direccion.Piso} {pedido.Direccion.Puerta}<br>
                                    {pedido.Direccion.CodigoPostal} - {pedido.Direccion.Municipio} ({pedido.Direccion.Provincia})
                                </p>
                            </div>
                        </div>

                        <div style='background-color: #FAF9F6; padding: 20px; text-align: center; border-top: 1px solid #E0E0E0;'>
                            <p style='color: #A0522D; font-size: 12px; margin: 0; font-weight: bold;'>
                                Sazón Local &copy; {DateTime.Now.Year}
                            </p>
                            <p style='color: #999999; font-size: 11px; margin-top: 5px;'>
                                Has recibido este correo porque realizaste una compra en nuestra tienda online.
                            </p>
                        </div>
                    </div>";
                string fileName = $"Factura_SazonLocal_{pedido.IdPedido}.pdf";
                try
                {
                    await emailService.SendEmailWithAttachmentBytesAsync(pedido.Cliente.Email, asunto, cuerpoHtml, pdfBytes,fileName);
                    HttpContext.Session.SetObject("EMAIL", true);
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMensaje = "Error al enviar el email: " +ex.Message;
                    Console.WriteLine($"Error al enviar email: {ex.Message}");
                }
            }
            return View(pedido);
        }
        #endregion
    }
}
