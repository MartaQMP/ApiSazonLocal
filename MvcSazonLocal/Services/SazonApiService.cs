using SazonLocalModels.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace MvcSazonLocal.Services
{
    public class SazonApiService
    {
        #region Configuracion y Helpers
        private string ApiUrl;
        private MediaTypeWithQualityHeaderValue header;

        public SazonApiService(IConfiguration configuration)
        {
            this.ApiUrl = configuration.GetValue<string>("ApiUrls:ApiSazon");
            this.header = new MediaTypeWithQualityHeaderValue("application/json");
        }

        private HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.ApiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            return client;
        }

        private HttpClient httpClient => this.CreateClient();

        private async Task<T?> GetAsync<T>(string request)
        {
            return await this.CallApiAsync<T>(request);
        }

        private static string BuildUrl(string path, Dictionary<string, string?> query)
        {
            if (query.Count == 0)
            {
                return path;
            }

            List<string> parametros = new List<string>();

            foreach (var item in query)
            {
                if (item.Value == null)
                {
                    continue;
                }

                string clave = Uri.EscapeDataString(item.Key);
                string valor = Uri.EscapeDataString(item.Value);
                parametros.Add($"{clave}={valor}");
            }

            if (parametros.Count == 0)
            {
                return path;
            }

            return $"{path}?{string.Join("&", parametros)}";
        }

        private async Task<T> CallApiAsync<T>(string request)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.GetAsync(request);
                if (response.IsSuccessStatusCode == true)
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                else
                {
                    return default(T);
                }
            }
        }
        #endregion

        #region Usuarios
        public async Task<int> UsuariosSinHashAsync()
        {
            var usuarios = await this.GetUsuariosSinHashAsync();
            return usuarios.Count;
        }

        public async Task RegisterUserAsync(string nombre, string apellidos, string email, string password, string imagen, string telefono, int idRol)
        {
            using (HttpClient client = this.CreateClient())
            {
                string request = "api/Auth/Register";
                var user = new
                {
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Email = email,
                    Password = password,
                    Telefono = telefono,
                    IdRol = idRol
                };

                string json = JsonConvert.SerializeObject(user);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<Usuario> LogInAsync(string email, string password)
        {
            using (HttpClient client = this.CreateClient())
            {
                string request = "api/Auth/Login";
                var loginData = new
                {
                    Email = email,
                    Password = password
                };

                string json = JsonConvert.SerializeObject(loginData);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<Usuario>();
            }
        }

        public async Task<List<Usuario>> GetUsuariosSinHashAsync()
        {
            return await this.GetAsync<List<Usuario>>("api/Usuarios/UsuariosSinHash") ?? new List<Usuario>();
        }

        public async Task<Usuario> GetUsuarioByIdAsync(int idUsuario)
            => await this.GetAsync<Usuario>($"api/Usuarios/{idUsuario}");

        public async Task UpdateUsuario(int idUsuario, string nombre, string apellidos, string telefono, string imagen)
        {
            using (HttpClient client = this.CreateClient())
            {
                string request = "api/Usuarios/UpdatePerfil";
                Usuario user = new Usuario
                {
                    IdUsuario = idUsuario,
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Telefono = telefono,
                    Imagen = imagen
                };

                string json = JsonConvert.SerializeObject(user);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<Usuario> GetUsuarioByEmailAsync(string email)
        {
            return await this.GetAsync<Usuario>($"api/Usuarios/GetUsuarioByEmail/{Uri.EscapeDataString(email)}");
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
            => await this.GetAsync<List<Usuario>>("api/Usuarios") ?? new List<Usuario>();

        public async Task UpdateEstadoUsuarioAsync(int idUsuario)
        {
            var response = await this.httpClient.PutAsync($"api/Usuarios/UpdateEstado/{idUsuario}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<KeysUsuario> GetKeysUsuarioAsync(int idUsuario)
            => await this.GetAsync<KeysUsuario>($"api/Usuarios/GetKeysUsuario/{idUsuario}");

        public async Task ActualizarPassword(int idUsuario, byte[] salt, byte[] password, string contrasena)
        {
            string request = "api/Usuarios/ActualizarPassword";
            var payload = new
            {
                IdUsuario = idUsuario,
                Salt = salt,
                Password = password,
                Contrasena = contrasena
            };

            string json = JsonConvert.SerializeObject(payload);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await this.httpClient.PutAsync(request, content);
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Productos y Unidades
        public async Task<ProductosPaginacion> GetProductosFiltroAsync(int posicion, string? buscador, int? idCategoria, int? idSubcategoria, int? idFinca, decimal? precio)
        {
            var url = BuildUrl("api/Productos/GetProductosFiltro", new Dictionary<string, string?>
            {
                ["posicion"] = posicion.ToString(),
                ["buscador"] = buscador,
                ["idCategoria"] = idCategoria?.ToString(),
                ["idSubcategoria"] = idSubcategoria?.ToString(),
                ["idFinca"] = idFinca?.ToString(),
                ["precio"] = precio?.ToString(CultureInfo.InvariantCulture)
            });
            return await this.GetAsync<ProductosPaginacion>(url)
                ?? new ProductosPaginacion
                {
                    Productos = new List<Producto>(),
                    TotalProductos = 0,
                    NumeroRegistros = 0
                };
        }

        public async Task<List<Producto>> GetProductosUsuarioAsync(int idUsuario)
            => await this.GetAsync<List<Producto>>($"api/Productos/Usuario/{idUsuario}") ?? new List<Producto>();

        public async Task<Producto> GetProductoByIdAsync(int idProducto)
            => await this.GetAsync<Producto>($"api/Productos/{idProducto}");

        public async Task InsertarProductoAsync(string nombre, string? descripcion, string? imagen, decimal precioUnidad, int unidadMedida, int stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria)
        {
            var response = await this.httpClient.PostAsJsonAsync("api/Productos", new Producto
            {
                Nombre = nombre,
                Descripcion = descripcion,
                Imagen = imagen,
                PrecioUnidad = precioUnidad,
                IdUnidadMedida = unidadMedida,
                Stock = stock,
                EstaActivo = estaActivo,
                IdFinca = idFinca,
                IdCategoria = idCategoria,
                IdSubcategoria = idSubcategoria
            });
            response.EnsureSuccessStatusCode();
        }

        public async Task ActualizarProductoAsync(int idProducto, decimal nuevoPrecio, int nuevoStock)
        {
            var response = await this.httpClient.PutAsync($"api/Productos/ActualizarDatos/{idProducto}/{nuevoPrecio.ToString(CultureInfo.InvariantCulture)}/{nuevoStock}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoProductoAsync(int idProducto)
        {
            var response = await this.httpClient.PutAsync($"api/Productos/CambiarEstado/{idProducto}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<int> GetStockProductoAsync(int idProducto)
            => await this.GetAsync<int>($"api/Productos/GetStock/{idProducto}");

        public async Task<List<UnidadMedida>> GetUnidadesMedidaAsync()
            => await this.GetAsync<List<UnidadMedida>>("api/UnidadMedida") ?? new List<UnidadMedida>();

        public async Task InsertarUnidadMedidaAsync(string nombre)
        {
            var response = await this.httpClient.PostAsync($"api/UnidadMedida/Insertar/{Uri.EscapeDataString(nombre)}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoUnidadMedidaAsync(int idUnidad)
        {
            var response = await this.httpClient.PutAsync($"api/UnidadMedida/CambiarEstado/{idUnidad}", null);
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Fincas
        public async Task<List<Finca>> GetFincasActivasAsync()
            => await this.GetAsync<List<Finca>>("api/Fincas/GetFincasActivas") ?? new List<Finca>();

        public async Task<List<Finca>> GetFincasPendientesAsync()
            => await this.GetAsync<List<Finca>>("api/Fincas/GetFincasPendientes") ?? new List<Finca>();

        public async Task<List<Finca>> GetFincasRechazadasAsync()
            => await this.GetAsync<List<Finca>>("api/Fincas/GetFincasRechazadas") ?? new List<Finca>();

        public async Task<List<Finca>> GetFincasUsuarioAsync(int idUsuario)
            => await this.GetAsync<List<Finca>>($"api/Fincas/GetFincasUsuario/{idUsuario}") ?? new List<Finca>();

        public async Task<Finca> GetFincaByIdAsync(int idFinca)
            => await this.GetAsync<Finca>($"api/Fincas/{idFinca}");

        public async Task InsertarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            var url = BuildUrl("api/Fincas", new Dictionary<string, string?>
            {
                ["nombre"] = nombre,
                ["direccion"] = direccion,
                ["municipio"] = municipio,
                ["provincia"] = provincia,
                ["latitud"] = latitud.ToString(CultureInfo.InvariantCulture),
                ["longitud"] = longitud.ToString(CultureInfo.InvariantCulture),
                ["idUsuario"] = idUsuario.ToString()
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task ActualizarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            var url = BuildUrl($"api/Fincas/Actualizar/{idFinca}", new Dictionary<string, string?>
            {
                ["nombre"] = nombre,
                ["direccion"] = direccion,
                ["municipio"] = municipio,
                ["provincia"] = provincia,
                ["latitud"] = latitud.ToString(CultureInfo.InvariantCulture),
                ["longitud"] = longitud.ToString(CultureInfo.InvariantCulture),
                ["idUsuario"] = idUsuario.ToString()
            });
            var response = await this.httpClient.PutAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoFincaAsync(int idFinca)
        {
            var response = await this.httpClient.PutAsync($"api/Fincas/CambiarEstado/{idFinca}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoValidacionFincaAsync(int idFinca, int nuevoEstado)
        {
            var response = await this.httpClient.PutAsync($"api/Fincas/CambiarEstadoValidacion/{idFinca}/{nuevoEstado}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Finca>> GetFincasAdminAsync(int? estado)
        {
            if (estado.HasValue)
            {
                return await this.GetAsync<List<Finca>>($"api/Fincas/GetFincasAdmin/{estado.Value}") ?? new List<Finca>();
            }

            var pendientes = await this.GetAsync<List<Finca>>("api/Fincas/GetFincasAdmin/1") ?? new List<Finca>();
            var aprobadas = await this.GetAsync<List<Finca>>("api/Fincas/GetFincasAdmin/2") ?? new List<Finca>();
            var rechazadas = await this.GetAsync<List<Finca>>("api/Fincas/GetFincasAdmin/3") ?? new List<Finca>();
            return pendientes.Concat(aprobadas).Concat(rechazadas).DistinctBy(f => f.IdFinca).ToList();
        }
        #endregion

        #region Direcciones
        public async Task<Direccion> GetDireccionByIdAsync(int idDireccion)
            => await this.GetAsync<Direccion>($"api/Direcciones/{idDireccion}");

        public async Task<List<Direccion>> GetDireccionesUsuarioAsync(int idUsuario)
            => await this.GetAsync<List<Direccion>>($"api/Direcciones/GetDireccionesUsuario/{idUsuario}") ?? new List<Direccion>();

        public async Task InsertarDireccionAsync(int idUsuario, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal)
        {
            var url = BuildUrl("api/Direcciones", new Dictionary<string, string?>
            {
                ["idUsuario"] = idUsuario.ToString(),
                ["etiqueta"] = etiqueta,
                ["calleNumero"] = calleNumero,
                ["piso"] = piso,
                ["puerta"] = puerta,
                ["cp"] = cp,
                ["municipio"] = municipio,
                ["provincia"] = provincia,
                ["notasAdicionales"] = notasAdicionales,
                ["latitud"] = latitud.ToString(CultureInfo.InvariantCulture),
                ["longitud"] = longitud.ToString(CultureInfo.InvariantCulture),
                ["esPrincipal"] = esPrincipal.ToString()
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task ActualizarDireccionAsync(int idDireccion, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal, int idUsuario)
        {
            var url = BuildUrl($"api/Direcciones/{idDireccion}", new Dictionary<string, string?>
            {
                ["etiqueta"] = etiqueta,
                ["calleNumero"] = calleNumero,
                ["piso"] = piso,
                ["puerta"] = puerta,
                ["cp"] = cp,
                ["municipio"] = municipio,
                ["provincia"] = provincia,
                ["notasAdicionales"] = notasAdicionales,
                ["latitud"] = latitud.ToString(CultureInfo.InvariantCulture),
                ["longitud"] = longitud.ToString(CultureInfo.InvariantCulture),
                ["esPrincipal"] = esPrincipal.ToString(),
                ["idUsuario"] = idUsuario.ToString()
            });
            var response = await this.httpClient.PutAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task EliminarDireccionAsync(int idDireccion)
        {
            var response = await this.httpClient.DeleteAsync($"api/Direcciones/{idDireccion}");
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Categorias y Subcategorias
        public async Task<List<Categoria>> GetCategoriasAsync()
            => await this.GetAsync<List<Categoria>>("api/Categorias") ?? new List<Categoria>();

        public async Task InsertarCategoriaAsync(string nombre, string descripcion)
        {
            var url = BuildUrl("api/Categorias", new Dictionary<string, string?>
            {
                ["nombre"] = nombre,
                ["descripcion"] = descripcion
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoCategoriaAsync(int idCategoria)
        {
            var response = await this.httpClient.PutAsync($"api/Categorias/CambiarEstado/{idCategoria}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Subcategoria>> GetSubcategoriasAsync()
            => await this.GetAsync<List<Subcategoria>>("api/Subcategorias") ?? new List<Subcategoria>();

        public async Task<List<Subcategoria>> GetSubcategoriasByCategoriaAsync(int idCategoria)
            => await this.GetAsync<List<Subcategoria>>($"api/Subcategorias/GetPorCategoria/{idCategoria}") ?? new List<Subcategoria>();

        public async Task<List<Subcategoria>> GetSubcategoriasConCategoriaAsync()
            => await this.GetAsync<List<Subcategoria>>("api/Subcategorias/GetConCategoria") ?? new List<Subcategoria>();

        public async Task InsertarSubcategoriaAsync(string nombre, string descripcion, string imagen, int idCategoria)
        {
            var url = BuildUrl("api/Subcategorias", new Dictionary<string, string?>
            {
                ["nombre"] = nombre,
                ["descripcion"] = descripcion,
                ["imagen"] = imagen,
                ["idCategoria"] = idCategoria.ToString()
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task CambiarEstadoSubcategoriaAsync(int idSubcategoria)
        {
            var response = await this.httpClient.PutAsync($"api/Subcategorias/CambiarEstado/{idSubcategoria}", null);
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Pedidos
        public async Task<List<Pedido>> GetPedidosUsuarioAsync(int idUsuario)
            => await this.GetAsync<List<Pedido>>($"api/Pedidos/GetPedidosUsuario/{idUsuario}") ?? new List<Pedido>();

        public async Task<List<Pedido>> GetPedidosProductosPendientesAsync(int idUsuario)
            => await this.GetAsync<List<Pedido>>($"api/Pedidos/GetPedidosProductosPendientes/{idUsuario}") ?? new List<Pedido>();

        public async Task CambiarEstadoPedido(int idPedido, string nuevoEstado)
        {
            var response = await this.httpClient.PutAsync($"api/Pedidos/CambiarEstado/{idPedido}/{Uri.EscapeDataString(nuevoEstado)}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<int> CrearPedidoAsync(int idUsuario, int idDireccion)
        {
            var url = BuildUrl("api/Pedidos", new Dictionary<string, string?>
            {
                ["idUsuario"] = idUsuario.ToString(),
                ["idDireccion"] = idDireccion.ToString()
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(contentStream);
            return document.RootElement.GetProperty("idPedido").GetInt32();
        }

        public async Task<Pedido> GetPedidoByIdAsync(int idPedido)
            => await this.GetAsync<Pedido>($"api/Pedidos/{idPedido}");

        public async Task<List<Pedido>> GetPedidosAsync()
            => await this.GetAsync<List<Pedido>>("api/Pedidos") ?? new List<Pedido>();

        public async Task<DetallePedido> GetDetallePedidoByIdAsync(int idDetalle)
            => await this.GetAsync<DetallePedido>($"api/Pedidos/GetDetalle/{idDetalle}");

        public async Task CambiarEstadoDetalleProductoAsync(int idDetalle)
        {
            var response = await this.httpClient.PutAsync($"api/Pedidos/CambiarEstadoDetalle/{idDetalle}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<DetallePedido>> GetDetallePedidosByPedidoAsync(int idPedido)
            => await this.GetAsync<List<DetallePedido>>($"api/Pedidos/GetDetalles/{idPedido}") ?? new List<DetallePedido>();
        #endregion

        #region Carrito
        public async Task InsertarProductoCarritoAsync(int cantidad, int idUsuario, int idProducto)
        {
            var url = BuildUrl("api/Carrito", new Dictionary<string, string?>
            {
                ["cantidad"] = cantidad.ToString(),
                ["idUsuario"] = idUsuario.ToString(),
                ["idProducto"] = idProducto.ToString()
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<CarritoItem> GetProductoCarritoAsync(int idUsuario, int idProducto)
        {
            var response = await this.httpClient.GetAsync($"api/Carrito/GetProductoCarrito/{idUsuario}/{idProducto}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return await response.Content.ReadFromJsonAsync<CarritoItem>();
        }

        public async Task<List<CarritoItem>> GetCarritoUsuarioAsync(int idUsuario)
            => await this.GetAsync<List<CarritoItem>>($"api/Carrito/GetCarritoUsuario/{idUsuario}") ?? new List<CarritoItem>();

        public async Task ActualizarCantidadCarritoAsync(int idUsuario, int idProducto, int nuevaCantidad)
        {
            var response = await this.httpClient.PutAsync($"api/Carrito/ActualizarCantidad/{idUsuario}/{idProducto}/{nuevaCantidad}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task EliminarProductoCarritoAsync(int idUsuario, int idProducto)
        {
            var response = await this.httpClient.DeleteAsync($"api/Carrito/EliminarProducto/{idUsuario}/{idProducto}");
            response.EnsureSuccessStatusCode();
        }

        public async Task EliminarCarritoUsuarioAsync(int idUsuario)
        {
            var response = await this.httpClient.DeleteAsync($"api/Carrito/EliminarCarrito/{idUsuario}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<decimal> GetSubtotalCarrito(int idUsuario)
        {
            var response = await this.httpClient.GetAsync($"api/Carrito/GetSubtotal/{idUsuario}");
            response.EnsureSuccessStatusCode();
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(contentStream);
            return document.RootElement.GetProperty("subtotal").GetDecimal();
        }
        #endregion

        #region Pagos
        public async Task InsertarPagoUsuarioAsync(int idPedido, string pasarela, string metodo, string ultimosDigitos, string estado, string transactionId)
        {
            var url = BuildUrl("api/Pagos", new Dictionary<string, string?>
            {
                ["idPedido"] = idPedido.ToString(),
                ["pasarela"] = pasarela,
                ["metodo"] = metodo,
                ["ultimosDigitos"] = ultimosDigitos,
                ["estado"] = estado,
                ["transactionId"] = transactionId
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Mensajes
        public async Task InsertarMensajeAsync(int? idUsuario, string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            var url = BuildUrl("api/Mensajes", new Dictionary<string, string?>
            {
                ["idUsuario"] = idUsuario?.ToString(),
                ["nombre"] = nombre,
                ["email"] = email,
                ["tipoConsulta"] = tipoConsulta,
                ["asunto"] = asunto,
                ["mensaje"] = mensaje
            });
            var response = await this.httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Mensaje>> GetMensajesAsync()
            => await this.GetAsync<List<Mensaje>>("api/Mensajes") ?? new List<Mensaje>();

        public async Task<Mensaje> GetMensajeByIdAsync(int idMensaje)
            => await this.GetAsync<Mensaje>($"api/Mensajes/{idMensaje}");

        public async Task MarcarMensajeLeidoAsync(int idMensaje)
        {
            var response = await this.httpClient.PutAsync($"api/Mensajes/MarcarLeido/{idMensaje}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task MarcarComoRespondidoAsync(int idMensaje)
        {
            var response = await this.httpClient.PutAsync($"api/Mensajes/MarcarRespondido/{idMensaje}", null);
            response.EnsureSuccessStatusCode();
        }
        #endregion
    }
}
