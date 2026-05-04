using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace MvcSazonLocal.Services
{
    public class SazonApiService
    {
        private string ApiUrl;
        private MediaTypeWithQualityHeaderValue header;
        private IHttpContextAccessor accessor;

        public SazonApiService(IConfiguration configuration, IHttpContextAccessor accessor)
        {
            this.ApiUrl = configuration.GetValue<string>("ApiUrls:ApiSazon");
            this.header = new MediaTypeWithQualityHeaderValue("application/json");
            this.accessor = accessor;
        }

        private string GetToken()
        {
            return this.accessor.HttpContext?.User?.FindFirst(x => x.Type == "TOKEN")?.Value;
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
                    return await response.Content.ReadAsAsync<T>();
                }
                else
                {
                    return default(T);
                }
            }
        }

        private async Task<T> CallApiAsync<T>(string request, string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Add("Authorization", "bearer " + token);
                HttpResponseMessage response = await client.GetAsync(request);
                if (response.IsSuccessStatusCode == true)
                {
                    return await response.Content.ReadAsAsync<T>();
                }
                else
                {
                    return default(T);
                }
            }
        }

        #region Auth
        public async Task RegisterUserAsync(string nombre, string apellidos, string email, string password, string imagen, string telefono, int idRol)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Auth/Register";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                Register user = new Register
                {
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Email = email,
                    Telefono = telefono,
                    IdRol = idRol
                };
                string json = JsonConvert.SerializeObject(user);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task<string> LogInAsync(string email, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Auth/Login";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                Login loginData = new Login
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
                string data = await response.Content.ReadAsStringAsync();
                JObject objeto = JObject.Parse(data);
                return objeto.GetValue("response").ToString();
            }
        }
        #endregion

        #region Usuarios
        public async Task<Usuario> GetUsuarioByEmailAsync(string email)
        {
            string request = "api/Usuarios/GetUsuarioByEmail/" + Uri.EscapeDataString(email);
            return await this.CallApiAsync<Usuario>(request);
        }

        // TOKEN
        public async Task<Usuario> GetUsuarioByIdAsync(string token = null)
        {
            string request = "api/Usuarios/GetUsuarioById";
            if (token == null) {
                token = this.GetToken();
            }
            return await this.CallApiAsync<Usuario>(request, token);


        }

        public async Task<KeysUsuario> GetKeysUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Usuarios/GetKeysUsuario";
            return await this.CallApiAsync<KeysUsuario>(request, token);
        }

        public async Task ActualizarPassword(int idUsuario, byte[] salt, byte[] password, string contrasena)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarPasswordUsuario";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var usuarioKeys = new KeysUsuario
                {
                    IdUsuario = idUsuario,
                    Salt = salt,
                    Password = password
                };
                string json = JsonConvert.SerializeObject(usuarioKeys);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PutAsync(request, content);
            }
        }

        public async Task UpdateUsuario(string nombre, string apellidos, string telefono, string imagen)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarPerfil";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Usuario user = new Usuario
                {
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Telefono = telefono,
                    Imagen = imagen
                };
                string json = JsonConvert.SerializeObject(user);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PutAsync(request, content);
            }
        }

        // ADMINISTRADOR
        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            string token = this.GetToken();
            string request = "api/Usuarios/GetUsuarios";
            return await this.CallApiAsync<List<Usuario>>(request, token) ?? new List<Usuario>();
        }

        public async Task UpdateEstadoUsuarioAsync(int idUsuario)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarEstadoUsuario/" + idUsuario;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }
        #endregion

        #region Productos y Unidades
        public async Task<ProductosPaginacion> GetProductosFiltroAsync(int posicion, string? buscador, int? idCategoria, int? idSubcategoria, int? idFinca, decimal? precio)
        {
            string request = "api/Productos/GetProductosFiltro?posicion=" + posicion;
            if (!string.IsNullOrEmpty(buscador)) request += "&buscador=" + Uri.EscapeDataString(buscador);
            if (idCategoria.HasValue) request += "&idCategoria=" + idCategoria;
            if (idSubcategoria.HasValue) request += "&idSubcategoria=" + idSubcategoria;
            if (idFinca.HasValue) request += "&idFinca=" + idFinca;
            if (precio.HasValue) request += "&precio=" + precio.Value.ToString(CultureInfo.InvariantCulture);
            
            return await this.CallApiAsync<ProductosPaginacion>(request)
                ?? new ProductosPaginacion
                {
                    Productos = new List<Producto>(),
                    TotalProductos = 0,
                    NumeroRegistros = 0
                };
        }
        public async Task<Producto> GetProductoByIdAsync(int idProducto)
        {
            string request = "api/Productos/" + idProducto;
            return await this.CallApiAsync<Producto>(request);
        }

        public async Task<int> GetStockProductoAsync(int idProducto)
        {
            string request = "api/Productos/GetStockProducto/" + idProducto;
            return await this.CallApiAsync<int>(request);
        }
        
        // TOKEN
        public async Task<List<Producto>> GetProductosUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Productos/GetProductosUsuario";
            return await this.CallApiAsync<List<Producto>>(request, token) ?? new List<Producto>();
        }

        // AGRICULTOR
        public async Task InsertarProductoAsync(string nombre, string? descripcion, string? imagen, decimal precioUnidad, int unidadMedida, int stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                ProductoDto producto = new ProductoDto
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
                };
                string json = JsonConvert.SerializeObject(producto);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarProductoAsync(int idProducto, decimal nuevoPrecio, int nuevoStock)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos/ActualizarDatosProducto/" + idProducto + "/" + nuevoPrecio.ToString(CultureInfo.InvariantCulture) + "/" + nuevoStock;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task CambiarEstadoProductoAsync(int idProducto)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos/CambiarEstadoProducto/" + idProducto;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }


        public async Task<List<UnidadMedida>> GetUnidadesMedidaAsync()
        {
            string request = "api/UnidadMedida";
            return await this.CallApiAsync<List<UnidadMedida>>(request) ?? new List<UnidadMedida>();
        }

        public async Task InsertarUnidadMedidaAsync(string nombre)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/UnidadMedida/" + Uri.EscapeDataString(nombre);
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PostAsync(request, null);
            }
        }

        public async Task CambiarEstadoUnidadMedidaAsync(int idUnidad)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/UnidadMedida/CambiarEstadoUnidadMedida/" + idUnidad;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }
        #endregion

        #region Fincas
        public async Task<List<Finca>> GetFincasActivasAsync()
        {
            string request = "api/Fincas/GetFincasActivas";
            return await this.CallApiAsync<List<Finca>>(request) ?? new List<Finca>();
        }

        public async Task<List<Finca>> GetFincasPendientesAsync()
        {
            string token = this.GetToken();
            string request = "api/Fincas/GetFincasPendientes";
            return await this.CallApiAsync<List<Finca>>(request, token) ?? new List<Finca>();
        }

        public async Task<List<Finca>> GetFincasRechazadasAsync()
        {
            string token = this.GetToken();
            string request = "api/Fincas/GetFincasRechazadas";
            return await this.CallApiAsync<List<Finca>>(request, token) ?? new List<Finca>();
        }

        public async Task<List<Finca>> GetFincasUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Fincas/GetFincas/Usuario";
            return await this.CallApiAsync<List<Finca>>(request, token) ?? new List<Finca>();
        }

        public async Task<Finca> GetFincaByIdAsync(int idFinca)
        {
            string request = "api/Fincas/" + idFinca;
            return await this.CallApiAsync<Finca>(request);
        }

        public async Task<List<Finca>> GetFincasAdminAsync(int? estado)
        {
            if (estado.HasValue)
            {
                string request = "api/Fincas/GetFincasAdmin/" + estado.Value;
                return await this.CallApiAsync<List<Finca>>(request) ?? new List<Finca>();
            }

            string req1 = "api/Fincas/GetFincasAdmin/1";
            string req2 = "api/Fincas/GetFincasAdmin/2";
            string req3 = "api/Fincas/GetFincasAdmin/3";
            var pendientes = await this.CallApiAsync<List<Finca>>(req1) ?? new List<Finca>();
            var aprobadas = await this.CallApiAsync<List<Finca>>(req2) ?? new List<Finca>();
            var rechazadas = await this.CallApiAsync<List<Finca>>(req3) ?? new List<Finca>();
            return pendientes.Concat(aprobadas).Concat(rechazadas).DistinctBy(f => f.IdFinca).ToList();
        }
        public async Task InsertarFincaAsync(string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                FincaDto finca = new FincaDto
                {
                    Nombre = nombre,
                    Direccion = direccion,
                    Municipio = municipio,
                    Provincia = provincia,
                    Latitud = latitud,
                    Longitud = longitud
                };
                string json = JsonConvert.SerializeObject(finca);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/Actualizar/" + idFinca;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                FincaDto finca = new FincaDto
                {
                    Nombre = nombre,
                    Direccion = direccion,
                    Municipio = municipio,
                    Provincia = provincia,
                    Latitud = latitud,
                    Longitud = longitud
                };
                string json = JsonConvert.SerializeObject(finca);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PutAsync(request, content);
            }
        }

        public async Task CambiarEstadoFincaAsync(int idFinca)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/CambiarEstado/" + idFinca;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task CambiarEstadoValidacionFincaAsync(int idFinca, int nuevoEstado)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/CambiarEstadoValidacion/" + idFinca + "/" + nuevoEstado;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }
        #endregion

        #region Direcciones
        public async Task<Direccion> GetDireccionByIdAsync(int idDireccion)
        {
            string request = "api/Direcciones/" + idDireccion;
            return await this.CallApiAsync<Direccion>(request);
        }

        public async Task<List<Direccion>> GetDireccionesUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Direcciones/GetDirecciones/Usuario";
            return await this.CallApiAsync<List<Direccion>>(request, token) ?? new List<Direccion>();
        }

        public async Task InsertarDireccionAsync(string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                DireccionDto direccion = new DireccionDto
                {
                    NombreEtiqueta = etiqueta,
                    CalleNumero = calleNumero,
                    Piso = piso,
                    Puerta = puerta,
                    CodigoPostal = cp,
                    Municipio = municipio,
                    Provincia = provincia,
                    NotasAdicionales = notasAdicionales,
                    Latitud = latitud,
                    Longitud = longitud,
                    EsPrincipal = esPrincipal
                };
                string json = JsonConvert.SerializeObject(direccion);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarDireccionAsync(int idDireccion, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones/" + idDireccion;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                DireccionDto direccion = new DireccionDto
                {
                    NombreEtiqueta = etiqueta,
                    CalleNumero = calleNumero,
                    Piso = piso,
                    Puerta = puerta,
                    CodigoPostal = cp,
                    Municipio = municipio,
                    Provincia = provincia,
                    NotasAdicionales = notasAdicionales,
                    Latitud = latitud,
                    Longitud = longitud,
                    EsPrincipal = esPrincipal
                };
                string json = JsonConvert.SerializeObject(direccion);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PutAsync(request, content);
            }
        }

        public async Task EliminarDireccionAsync(int idDireccion)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones/" + idDireccion;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.DeleteAsync(request);
            }
        }
        #endregion

        #region Categorias y Subcategorias
        public async Task<List<Categoria>> GetCategoriasAsync()
        {
            string request = "api/Categorias";
            return await this.CallApiAsync<List<Categoria>>(request) ?? new List<Categoria>();
        }

        public async Task InsertarCategoriaAsync(string nombre, string descripcion)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Categorias";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                CategoriaDto categoria = new CategoriaDto { Nombre = nombre, Descripcion = descripcion };
                string json = JsonConvert.SerializeObject(categoria);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task CambiarEstadoCategoriaAsync(int idCategoria)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Categorias/CambiarEstadoCategoria/" + idCategoria;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task<List<Subcategoria>> GetSubcategoriasAsync()
        {
            string request = "api/Subcategorias";
            return await this.CallApiAsync<List<Subcategoria>>(request) ?? new List<Subcategoria>();
        }

        public async Task<List<Subcategoria>> GetSubcategoriasByCategoriaAsync(int idCategoria)
        {
            string request = "api/Subcategorias/GetSubcategoriaPorCategoria/" + idCategoria;
            return await this.CallApiAsync<List<Subcategoria>>(request) ?? new List<Subcategoria>();
        }

        public async Task<List<Subcategoria>> GetSubcategoriasConCategoriaAsync()
        {
            string request = "api/Subcategorias/GetSubcategoriaConCategoria";
            return await this.CallApiAsync<List<Subcategoria>>(request) ?? new List<Subcategoria>();
        }

        public async Task InsertarSubcategoriaAsync(string nombre, string descripcion, string imagen, int idCategoria)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Subcategorias";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                SubcategoriaDto subcategoria = new SubcategoriaDto
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Imagen = imagen,
                    IdCategoria = idCategoria
                };
                string json = JsonConvert.SerializeObject(subcategoria);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task CambiarEstadoSubcategoriaAsync(int idSubcategoria)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Subcategorias/CambiarEstadoSubcategoria/" + idSubcategoria;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }
        #endregion

        #region Pedidos
        public async Task<List<Pedido>> GetPedidosUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Pedidos/GetPedidosUsuario";
            return await this.CallApiAsync<List<Pedido>>(request, token) ?? new List<Pedido>();
        }

        public async Task<List<Pedido>> GetPedidosProductosPendientesAsync()
        {
            string token = this.GetToken();
            string request = "api/Pedidos/GetPedidosProductosPendientes";
            return await this.CallApiAsync<List<Pedido>>(request, token) ?? new List<Pedido>();
        }

        public async Task CambiarEstadoPedido(int idPedido, string nuevoEstado)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos/CambiarEstadoPedido/" + idPedido + "/" + Uri.EscapeDataString(nuevoEstado);
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task<int> CrearPedidoAsync(int idDireccion)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                PedidoDto pedido = new PedidoDto { IdDireccion = idDireccion };
                string json = JsonConvert.SerializeObject(pedido);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var document = await System.Text.Json.JsonDocument.ParseAsync(contentStream);
                return document.RootElement.GetProperty("idPedido").GetInt32();
            }
        }

        public async Task<Pedido> GetPedidoByIdAsync(int idPedido)
        {
            string request = "api/Pedidos/" + idPedido;
            return await this.CallApiAsync<Pedido>(request);
        }

        public async Task<List<Pedido>> GetPedidosAsync()
        {
            string request = "api/Pedidos";
            return await this.CallApiAsync<List<Pedido>>(request) ?? new List<Pedido>();
        }

        public async Task<DetallePedido> GetDetallePedidoByIdAsync(int idDetalle)
        {
            string token = this.GetToken();
            string request = "api/Pedidos/GetDetallePedido/" + idDetalle;
            return await this.CallApiAsync<DetallePedido>(request, token);
        }

        public async Task CambiarEstadoDetalleProductoAsync(int idDetalle)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos/CambiarEstadoDetallePedido/" + idDetalle;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task<List<DetallePedido>> GetDetallePedidosByPedidoAsync(int idPedido)
        {
            string token = this.GetToken();
            string request = "api/Pedidos/GetDetallesPedido/" + idPedido;
            return await this.CallApiAsync<List<DetallePedido>>(request, token) ?? new List<DetallePedido>();
        }
        #endregion

        #region Carrito
        public async Task InsertarProductoCarritoAsync(int cantidad, int idProducto)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                CarritoItemDto carrito = new CarritoItemDto { Cantidad = cantidad, IdProducto = idProducto };
                string json = JsonConvert.SerializeObject(carrito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task<CarritoItem> GetProductoCarritoAsync(int idProducto)
        {
            string token = this.GetToken();
            string request = "api/Carrito/GetProductoCarritoUsuario/" + idProducto;
            return await this.CallApiAsync<CarritoItem>(request, token);
        }

        public async Task<List<CarritoItem>> GetCarritoUsuarioAsync()
        {
            string token = this.GetToken();
            string request = "api/Carrito/GetCarritoUsuario";
            return await this.CallApiAsync<List<CarritoItem>>(request, token) ?? new List<CarritoItem>();
        }

        public async Task ActualizarCantidadCarritoAsync(int idProducto, int nuevaCantidad)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/ActualizarCantidad";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                CarritoItemDto carrito = new CarritoItemDto { IdProducto = idProducto, Cantidad = nuevaCantidad };
                string json = JsonConvert.SerializeObject(carrito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PutAsync(request, content);
            }
        }

        public async Task EliminarProductoCarritoAsync(int idProducto)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/EliminarProductoCarrito/" + idProducto;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.DeleteAsync(request);
            }
        }

        public async Task EliminarCarritoUsuarioAsync()
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/EliminarCarritoUsuario";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.DeleteAsync(request);
            }
        }

        public async Task<decimal> GetSubtotalCarrito()
        {
            string token = this.GetToken();
            string request = "api/Carrito/GetSubtotalCarritoUsuario";
            var response = await this.CallApiAsync<dynamic>(request, token);
            return response["subtotal"];
        }
        #endregion

        #region Pagos
        public async Task InsertarPagoUsuarioAsync(int idPedido, string pasarela, string metodo, string ultimosDigitos, string estado, string transactionId)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pagos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                PagoDto pago = new PagoDto
                {
                    IdPedido = idPedido,
                    Pasarela = pasarela,
                    MetodoPago = metodo,
                    UltimosDigitosTarjeta = ultimosDigitos,
                    EstadoPago = estado,
                    TransactionId = transactionId
                };
                string json = JsonConvert.SerializeObject(pago);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }
        #endregion

        #region Mensajes
        public async Task InsertarMensajeUsuarioAsync(string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/InsertarMensajeUsuario";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                MensajeDto mensajeObj = new MensajeDto
                {
                    Nombre = nombre,
                    Email = email,
                    TipoConsulta = tipoConsulta,
                    Asunto = asunto,
                    Contenido = mensaje
                };
                string json = JsonConvert.SerializeObject(mensajeObj);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }
        public async Task InsertarMensajeAsync(string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/InsertarMensaje";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                MensajeDto mensajeObj = new MensajeDto
                {
                    Nombre = nombre,
                    Email = email,
                    TipoConsulta = tipoConsulta,
                    Asunto = asunto,
                    Contenido = mensaje
                };
                string json = JsonConvert.SerializeObject(mensajeObj);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(request, content);
            }
        }

        public async Task<List<Mensaje>> GetMensajesAsync()
        {
            string request = "api/Mensajes";
            return await this.CallApiAsync<List<Mensaje>>(request) ?? new List<Mensaje>();
        }

        public async Task<Mensaje> GetMensajeByIdAsync(int idMensaje)
        {
            string request = "api/Mensajes/" + idMensaje;
            return await this.CallApiAsync<Mensaje>(request);
        }

        public async Task MarcarMensajeLeidoAsync(int idMensaje)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/MarcarLeido/" + idMensaje;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }

        public async Task MarcarComoRespondidoAsync(int idMensaje)
        {
            string token = this.GetToken();
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/MarcarRespondido/" + idMensaje;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PutAsync(request, null);
            }
        }
        #endregion
    }
}
