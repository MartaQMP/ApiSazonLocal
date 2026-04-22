using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazon_Local.Filters;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using Sazon_Local.Services;
using System.Globalization;
using System.Security.Claims;

namespace MvcSazonLocal.Controllers
{
    public class CuentaController : Controller
    {
        private SazonApiService serviceApi;
        private HelperPath helper;
        private static string[] extensionesValidas = { ".jpg", ".jpeg", ".png" };

        public CuentaController(SazonApiService serviceApi, HelperPath helper)
        {
            this.serviceApi = serviceApi;
            this.helper = helper;
        }

        private IActionResult AjaxOkOrRedirect(string action)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Ok();
            return RedirectToAction(action);
        }

        #region PERFIL
        [HttpGet]
        [AuthorizeUsuarios]
        public async Task<IActionResult> Perfil()
        {
            ViewData["PaginaActiva"] = "Perfil";
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Usuario usuario = await this.serviceApi.GetUsuarioByIdAsync(idUsuario);
            if(usuario.Imagen != null)
            {
                usuario.Imagen = this.helper.MapUrlPath(usuario.Imagen, Folders.Usuarios);
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(usuario);
            }
            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPerfil(string nombre, string apellidos, string telefono, IFormFile imagen)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Usuario usuarioActual = await this.serviceApi.GetUsuarioByIdAsync(idUsuario);
            string nombreImagenFinal = usuarioActual.Imagen;

            if (imagen != null && imagen.Length > 0)
            {
                string extension = Path.GetExtension(imagen.FileName).ToLower();

                if (extensionesValidas.Contains(extension))
                {
                    string pathAntiguo = this.helper.MapPath(nombreImagenFinal, Folders.Usuarios);
                    if (System.IO.File.Exists(pathAntiguo))
                    {
                        System.IO.File.Delete(pathAntiguo);
                    }

                    string nombreLimpio = HelperTextCleaner.LimpiarTexto(nombre);
                    string apellidoLimpio = HelperTextCleaner.LimpiarTexto(apellidos);
                    nombreImagenFinal = $"{idUsuario}_{nombreLimpio}_{apellidoLimpio}_{DateTime.Now.Ticks}{extension}";

                    string path = this.helper.MapPath(nombreImagenFinal, Folders.Usuarios);
                    
                    using (Stream stream = new FileStream(path, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }
                }
                else
                {
                    ViewBag.ErrorMensaje = "El archivo pasado no es una imagen, tiene q tener extension: .jpg, .jpeg o .png.";
                }
            }
            await this.serviceApi.UpdateUsuario(idUsuario, nombre, apellidos, telefono, nombreImagenFinal);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, nombre));
            string nombreRol = usuarioActual.IdRol == 1 ? "ADMINISTRADOR" : (usuarioActual.IdRol == 2 ? "AGRICULTOR" : "CLIENTE");
            identity.AddClaim(new Claim(ClaimTypes.Role, nombreRol));
            identity.AddClaim(new Claim("ID_ROL", usuarioActual.IdRol.ToString()));
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            
            return AjaxOkOrRedirect("Perfil");
        }
        #endregion

        #region DIRECCIONES
        [AuthorizeUsuarios]
        [HttpGet]
        public async Task<IActionResult> Direcciones(int? idEditar)
        {
            ViewData["PaginaActiva"] = "Direcciones";
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var direcciones = await this.serviceApi.GetDireccionesUsuarioAsync(idUsuario);
            if(direcciones == null)
            {
                ViewBag.SinDirecciones = "No tienes ninguna direccion añadida";
                return View();
            }
            ViewBag.IdEditar = idEditar;
            ViewBag.ErrorMensaje = TempData["ErrorMensaje"];
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(direcciones);
            }
            return View(direcciones);
        }

        [HttpPost]
        public async Task<IActionResult> Actualizar(int idDireccion, string? nombreEtiqueta, string calleNumero, string? piso, string? puerta, string municipio, string provincia, string? notasAdicionales, string cp, bool esPrincipal)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var coordenadas = await HelperMap.GetCoordenadasAsync(calleNumero, municipio, provincia);
            if (coordenadas.latitud == 0 && coordenadas.longitud == 0)
            {
                TempData["ErrorMensaje"] = "Ha habido un error actualizando la direccion.";
                return RedirectToAction("Direcciones");
            }
            await this.serviceApi.ActualizarDireccionAsync(idDireccion, nombreEtiqueta, calleNumero, piso, puerta, cp, municipio, provincia, notasAdicionales, coordenadas.latitud, coordenadas.longitud, esPrincipal, idUsuario);
            return AjaxOkOrRedirect("Direcciones");
        }

        public IActionResult CrearDireccion()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearDireccion(string? nombreEtiqueta, string calleNumero, string? piso, string? puerta, string municipio, string provincia, string? notasAdicionales, string cp, bool esPrincipal)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var coordenadas = await HelperMap.GetCoordenadasAsync(calleNumero, municipio, provincia);
            if (coordenadas.latitud == 0 && coordenadas.longitud == 0)
            {
                TempData["ErrorMensaje"] = "Ha habido un error calculando las coordenadas revisa la direccion.";
                return RedirectToAction("Direcciones");
            }
            await this.serviceApi.InsertarDireccionAsync(idUsuario, nombreEtiqueta, calleNumero, piso, puerta, cp, municipio, provincia, notasAdicionales, coordenadas.latitud, coordenadas.longitud, esPrincipal);
            return RedirectToAction("Direcciones");
        }


        [HttpPost]
        public async Task<IActionResult> Eliminar(int idDireccion)
        {
            var direccion = await this.serviceApi.GetDireccionByIdAsync(idDireccion);
            if (direccion != null)
            {
                await this.serviceApi.EliminarDireccionAsync(idDireccion);
            }
            TempData["ErrorMensaje"] = "Ha habido un error eliminando la direccion";
            return AjaxOkOrRedirect("Direcciones");
        }
        #endregion

        #region PEDIDOS
        [AuthorizeUsuarios]
        public async Task<IActionResult> Pedidos()
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var pedidos = await this.serviceApi.GetPedidosUsuarioAsync(idUsuario);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(pedidos);
            }
            return View(pedidos);
        }
        #endregion

        #region SEGURIDAD

        [AuthorizeUsuarios]
        public IActionResult Seguridad()
        {
            ViewBag.ErrorMensaje = TempData["ErrorMensaje"];
            ViewBag.Mensaje = TempData["Mensaje"];
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Seguridad(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["ErrorMensaje"] = "Todos los campos son obligatorios.";
                return AjaxOkOrRedirect("Seguridad");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMensaje"] = "No coinciden las contraseñas.";
                return AjaxOkOrRedirect("Seguridad");
            }

            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await this.serviceApi.GetUsuarioByIdAsync(idUsuario);
            var registroKey = await this.serviceApi.GetKeysUsuarioAsync(idUsuario);
            if (registroKey == null) return NotFound();

            byte[] saltAntiguo = registroKey.Salt; 
            byte[] hashOldInput = HelperAuth.EncryptPassword(oldPassword, saltAntiguo);

            if (!HelperAuth.CompararPasswords(hashOldInput, registroKey.Password))
            {
                TempData["ErrorMensaje"] = "La contraseña actual no es correcta.";
                return AjaxOkOrRedirect("Seguridad");
            }

            byte[] nuevoSalt = HelperAuth.GenerarSalt();
            byte[] nuevoHash = HelperAuth.EncryptPassword(newPassword, nuevoSalt);

            await this.serviceApi.ActualizarPassword(idUsuario, nuevoSalt, nuevoHash, newPassword);

            TempData["Mensaje"] = "Contraseña actualizada con éxito.";
            return AjaxOkOrRedirect("Seguridad");
        }
        #endregion

        #region MIS PRODUCTOS
        [AuthorizeUsuarios(Roles = "AGRICULTOR")]
        [HttpGet]
        public async Task<IActionResult> MisProductos(int? idEditando)
        {
            ViewBag.IdEditando = idEditando;
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var productos = await this.serviceApi.GetProductosUsuarioAsync(idUsuario);
            foreach (var producto in productos)
            {
                if (!string.IsNullOrEmpty(producto.Imagen))
                {
                    producto.Imagen = this.helper.MapUrlPath(producto.Imagen, Folders.Productos); ;
                }
                else
                {
                    producto.Imagen = this.helper.MapUrlPath(producto.Subcategoria.Imagen, Folders.Subcategorias); ;
                }
            }
            var productosAgrupados = productos.GroupBy(p => p.Finca.Nombre);
            ViewBag.Mensaje = TempData["Mensaje"];
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(productosAgrupados);
            }
            return View(productosAgrupados);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarProducto(int idProducto, string nuevoPrecio, string nuevoStock)
        {
            var producto = await this.serviceApi.GetProductoByIdAsync(idProducto);
            if (producto != null)
            {
                var culture = CultureInfo.InvariantCulture;
                decimal precio = Convert.ToDecimal(nuevoPrecio, culture);
                precio *= 1.15m;
                int stock = int.Parse(nuevoStock);
                await this.serviceApi.ActualizarProductoAsync(producto.IdProducto, precio, stock);
                TempData["Mensaje"] = "Producto actualizado correctamente.";
                
            }
            return AjaxOkOrRedirect("MisProductos");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int idProducto)
        {
            var producto = await this.serviceApi.GetProductoByIdAsync(idProducto);
            if (producto != null)
            {
                await this.serviceApi.CambiarEstadoProductoAsync(idProducto);
            }
            return AjaxOkOrRedirect("MisProductos");
        }

        [HttpGet]
        public async Task<JsonResult> GetSubcategorias(int idCategoria)
        {
            var subcategorias = await this.serviceApi.GetSubcategoriasByCategoriaAsync(idCategoria);
            return Json(subcategorias);
        }

        public async Task<IActionResult> CrearProducto()
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Fincas = (await this.serviceApi.GetFincasUsuarioAsync(idUsuario)).Where(f =>f.EstaActiva).ToList();
            ViewBag.Categorias = (await this.serviceApi.GetCategoriasAsync()).Where(c => c.EstaActiva).ToList();
            ViewBag.Subcategorias = (await this.serviceApi.GetSubcategoriasAsync()).Where(s => s.EstaActiva).ToList();
            ViewBag.UnidadesMedida = (await this.serviceApi.GetUnidadesMedidaAsync()).Where(u =>u.EstaActiva).ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CrearProducto(string nombre, string? descripcion, IFormFile? imagen, string precioUnidad, int unidadMedida, string stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            string? urlImagen = null;

            if (imagen != null)
            {
                string extension = Path.GetExtension(imagen.FileName).ToLower();
                if (extensionesValidas.Contains(extension))
                {
                    string nombreLimpio = HelperTextCleaner.LimpiarTexto(nombre);
                    urlImagen = idUsuario + "_" + nombreLimpio + "_" + idFinca + extension;
                    string path = this.helper.MapPath(urlImagen, Folders.Productos);
                    using (Stream stream = new FileStream(path, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }
                }
                else
                {
                    ViewBag.ErrorMensaje = "El archivo pasado no es una imagen, tiene q tener extension: .jpg, .jpeg o .png."; 
                    ViewBag.Fincas = await this.serviceApi.GetFincasUsuarioAsync(idUsuario);
                    ViewBag.Categorias = await this.serviceApi.GetCategoriasAsync();
                    ViewBag.Subcategorias = await this.serviceApi.GetSubcategoriasAsync();
                    return View();
                }
            }
            var culture = CultureInfo.InvariantCulture;
            decimal nuevoPrecio = Convert.ToDecimal(precioUnidad, culture);
            nuevoPrecio *= 1.15m;
            int nuevoStock = int.Parse(stock);
            await this.serviceApi.InsertarProductoAsync(nombre, descripcion, urlImagen, nuevoPrecio, unidadMedida, nuevoStock, estaActivo, idFinca, idCategoria, idSubcategoria);
            
            TempData["Mensaje"] = "Producto creado correctamente";
            return AjaxOkOrRedirect("MisProductos");
        }
        #endregion

        #region PEDIDOS PENDIENTES
        [HttpGet]
        [AuthorizeUsuarios(Roles = "AGRICULTOR")]
        public async Task<IActionResult> PedidosPendientes()
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var pedidos = await this.serviceApi.GetPedidosProductosPendientesAsync(idUsuario);
            ViewBag.IdUsuario = idUsuario;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(pedidos);
            }
            return View(pedidos);
        }
        [HttpPost]
        public async Task<IActionResult> ActualizarEstadoPedido(int idPedido)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); ;
            List<DetallePedido> items = await this.serviceApi.GetDetallePedidosByPedidoAsync(idPedido);
            Console.WriteLine("Total items: " + items.Count);
            Console.WriteLine("IdUsuario: " + idUsuario);
            var misProductosPendientes = items.Where(d => d.Producto.Finca.Agricultor.IdUsuario == idUsuario && !d.Listo);
            foreach (DetallePedido item in misProductosPendientes)
            {
                Console.WriteLine($"Producto: {item.Producto?.Nombre}, Finca: {item.Producto?.Finca?.Nombre}, Agricultor: {item.Producto?.Finca?.Agricultor?.IdUsuario}, Listo: {item.Listo}");
                await this.serviceApi.CambiarEstadoDetalleProductoAsync(item.IdDetalle);
            }
            return AjaxOkOrRedirect("PedidosPendientes");
        }
        #endregion

        #region MIS FINCAS
        [AuthorizeUsuarios(Roles = "AGRICULTOR")]
        public async Task<IActionResult> MisFincas()
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            List<Finca> fincas = await this.serviceApi.GetFincasUsuarioAsync(idUsuario);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(fincas);
            }
            return View(fincas);
        }
        [HttpGet]
        public IActionResult CrearFinca()
        {
            return View("GestionarFinca", new Finca());
        }

        [HttpGet]
        public async Task<IActionResult> EditarFinca(int id)
        {
            var finca = await this.serviceApi.GetFincaByIdAsync(id);

            if (finca == null)
            {
                return RedirectToAction("MisFincas");
            }
            return View("GestionarFinca", finca);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarFinca(int idFinca, string nombre, string direccion ,string municipio, string provincia, string latitudStr, string longitudStr)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            decimal latitud = decimal.Parse(latitudStr.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
            decimal longitud = decimal.Parse(longitudStr.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);

            if (idFinca == 0)
            {
                await this.serviceApi.InsertarFincaAsync(idFinca, nombre, direccion, municipio, provincia, latitud, longitud, idUsuario);
                TempData["Mensaje"] = "¡Finca registrada con éxito!";
            }
            else
            {
                await this.serviceApi.ActualizarFincaAsync(idFinca, nombre, direccion, municipio, provincia, latitud, longitud, idUsuario);
                TempData["Mensaje"] = "Finca actualizada correctamente.";
            }

            return RedirectToAction("MisFincas");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoFinca(int idFinca)
        {
            var finca = await this.serviceApi.GetFincaByIdAsync(idFinca);
            if(finca.EstaValidada != 1)
            {
                await this.serviceApi.CambiarEstadoFincaAsync(idFinca);
            }
            return RedirectToAction("MisFincas");
        }
        #endregion
    }
}
