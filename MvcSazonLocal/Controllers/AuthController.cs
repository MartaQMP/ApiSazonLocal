using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MvcSazonLocal.Data;
using MvcSazonLocal.Extensions;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;
using System.Security.Claims;
using SazonLocalInterfaces.Services;

namespace MvcSazonLocal.Controllers
{
    public class AuthController : Controller
    {
        private SazonApiService serviceApi;
        private SazonContext context;
        private IEmailService emailService;

        public AuthController(SazonApiService serviceApi, SazonContext context, IEmailService emailService)
        {
            this.serviceApi = serviceApi;
            this.context = context;
            this.emailService = emailService;
        }

        #region REGISTER
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string nombre, string apellidos, string email, string password, string confirmPassword, string telefono, int idRol)
        {
            if (password != confirmPassword)
            {
                ViewBag.MensajeError = "Las contraseñas no coinciden";
                return View();
            }
            Usuario user = await this.serviceApi.GetUsuarioByEmailAsync(email);
            if(user  != null)
            {
                ViewBag.MensajeError = "Ya hay un usuario con este email";
                return View();
            }
            await this.serviceApi.RegisterUserAsync(nombre, apellidos, email, password, "usuario-generico.png", telefono, idRol);
            ViewBag.Mensaje = "Usuario registrado correctamente";
            return RedirectToAction("LogIn");
        }
        #endregion

        #region LOGIN
        public async Task<IActionResult> LogIn(bool estaComprando = false)
        {
            ViewBag.Comprando = estaComprando;
            int usuariosSinHash = await this.serviceApi.UsuariosSinHashAsync();
            if (usuariosSinHash > 0)
            {
                ViewBag.PendientesHash = usuariosSinHash;
            }
            ViewBag.Mensaje = TempData["Mensaje"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(string email, string password, bool estaComprando)
        {
            Usuario user = await this.serviceApi.LogInAsync(email, password);
            if (user == null)
            {
                ViewBag.MensajeError = "Usuario o contraseña incorrectos";
                ViewBag.PendientesHash = 0;
                ViewBag.Comprando = estaComprando;
                return View();
            }
            if (user.EstaActivo == false)
            {
                return RedirectToAction("UsuarioBloqueado");
            }
            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            identity.AddClaim(new Claim(ClaimTypes.Name, user.Nombre));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()));
            string nombreRol = user.IdRol == 1 ? "ADMINISTRADOR" : (user.IdRol == 2 ? "AGRICULTOR" : "CLIENTE");
            identity.AddClaim(new Claim(ClaimTypes.Role, nombreRol));
            identity.AddClaim(new Claim("ID_ROL", user.IdRol.ToString()));

            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            string action = TempData["action"]?.ToString() ?? "Productos";
            string controller = TempData["controller"]?.ToString() ?? "Productos";

            await MigrarCarritoSessionABBDD(user.IdUsuario);
            
            return RedirectToAction(action, controller);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPassword()
        {
            var usuarios = await this.serviceApi.GetUsuariosSinHashAsync();
            foreach (var u in usuarios)
            {
                byte[] salt = HelperAuth.GenerarSalt();
                byte[] pass = HelperAuth.EncryptPassword("12345", salt);

                this.context.KeysUsuarios.Add(new KeysUsuario
                {
                    IdUsuario = u.IdUsuario,
                    Salt = salt,
                    Password = pass
                });
            }
            await this.context.SaveChangesAsync();
            return RedirectToAction("LogIn");
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Productos", "Productos");
        }
        #endregion

        #region ERROR
        public IActionResult ErrorAcceso()
        {
            return View();
        }
        #endregion

        #region USUARIO BLOQUEADO
        public IActionResult UsuarioBloqueado()
        {
            return View();
        }
        #endregion

        #region RECUPERAR PASSWORD
        public IActionResult OlvidePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OlvidePassword(string email)
        {
            Usuario user = await this.serviceApi.GetUsuarioByEmailAsync(email);

            if (user != null)
            {
                string codigo = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                HttpContext.Session.SetObject("CODIGO_RECUPERACION", codigo);
                HttpContext.Session.SetObject("EMAIL", email);

                string asunto = "Código de recuperación - Sazón Local";
                string cuerpoHtml = $@"
                    <div style='border: 1px solid #556B2f; padding: 20px; font-family: sans-serif;'>
                        <h2 style='color: #556B2f;'>Recuperación de Contraseña</h2>
                        <p>Hola {user.Nombre}, usa el siguiente código para cambiar tu clave:</p>
                        <div style='background: #F5F5DC; padding: 10px; font-size: 24px; text-align: center; font-weight: bold;'>
                            {codigo}
                        </div>
                    </div>";

                await this.emailService.SendEmailAsync(email, asunto, cuerpoHtml);
                return RedirectToAction("ValidarCodigo");
            }
            ViewBag.MensajeError = "El email no está registrado";
            return View();
        }

        public IActionResult ValidarCodigo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ValidarCodigo(string codigo, string password, string confirmPassword)
        {
            string emailUsuario = HttpContext.Session.GetObject<string>("EMAIL");
            Usuario usuario = await this.serviceApi.GetUsuarioByEmailAsync(emailUsuario);
            string codigoSesion = HttpContext.Session.GetObject<string>("CODIGO_RECUPERACION");

            if (string.IsNullOrEmpty(codigo) || codigo != codigoSesion)
            {
                ViewBag.MensajeError = "El código no es correcto";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.MensajeError = "Las contraseñas no coinciden";
                return View();
            }

            if (usuario != null)
            {
                var keysAntiguas = await this.serviceApi.GetKeysUsuarioAsync(usuario.IdUsuario);
                byte[] hashParaComparar = HelperAuth.EncryptPassword(password, keysAntiguas.Salt);

                if (HelperAuth.CompararPasswords(hashParaComparar, keysAntiguas.Password))
                {
                    ViewBag.MensajeError = "La contraseña no puede ser igual que la anterior";
                    return View();
                }

                byte[] salt = HelperAuth.GenerarSalt();
                byte[] hashPassword = HelperAuth.EncryptPassword(password, salt);

                await this.serviceApi.ActualizarPassword(usuario.IdUsuario, salt, hashPassword, password);

                HttpContext.Session.Remove("CODIGO_RECUPERACION");
                HttpContext.Session.Remove("EMAIL");

                TempData["Mensaje"] = "Contraseña cambiada con éxito";
                return RedirectToAction("LogIn", "Auth");
            }
            ViewBag.MensajeError = "Error al identificar el usuario";
            return View();
        }
        #endregion

        #region MIGRAR CARRITO SESSION A BBDD
        private async Task MigrarCarritoSessionABBDD(int idUsuario)
        {
            var carritoSession = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");

            if (carritoSession != null && carritoSession.Any())
            {
                foreach (var item in carritoSession)
                {
                    await this.serviceApi.InsertarProductoCarritoAsync(item.Value, idUsuario, item.Key);
                }
                HttpContext.Session.Remove("CARRITO");
            }
        }
        #endregion
    }
}
