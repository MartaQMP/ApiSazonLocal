using SazonLocalModels.Models;
using System.Globalization;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using SazonLocalModels.Dto;

namespace MvcSazonLocal.Services
{
    public class SazonApiService
    {
        private string ApiUrl;
        private MediaTypeWithQualityHeaderValue header;

        public SazonApiService(IConfiguration configuration)
        {
            this.ApiUrl = configuration.GetValue<string>("ApiUrls:ApiSazon");
            this.header = new MediaTypeWithQualityHeaderValue("application/json");
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

        #region Usuarios
        public async Task<int> UsuariosSinHashAsync()
        {
            var usuarios = await this.GetUsuariosSinHashAsync();
            return usuarios.Count;
        }

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
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task<Usuario> LogInAsync(string email, string password)
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
                  return await response.Content.ReadAsAsync<Usuario>();
              }
        }


        public async Task<List<Usuario>> GetUsuariosSinHashAsync()
        {
            string request = "api/Usuarios/UsuariosSinHash";
            return await this.CallApiAsync<List<Usuario>>(request) ?? new List<Usuario>();
        }

        public async Task<Usuario> GetUsuarioByIdAsync(int idUsuario)
        {
            string request = "api/Usuarios/" + idUsuario;
            return await this.CallApiAsync<Usuario>(request);
        }

        public async Task UpdateUsuario(int idUsuario, string nombre, string apellidos, string telefono, string imagen)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarPerfil/" + idUsuario;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
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
            }
        }

        public async Task<Usuario> GetUsuarioByEmailAsync(string email)
        {
            string request = "api/Usuarios/GetUsuarioByEmail/" + Uri.EscapeDataString(email);
            return await this.CallApiAsync<Usuario>(request);
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            string request = "api/Usuarios";
            return await this.CallApiAsync<List<Usuario>>(request) ?? new List<Usuario>();
        }

        public async Task UpdateEstadoUsuarioAsync(int idUsuario)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarEstadoUsuario/" + idUsuario;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task<KeysUsuario> GetKeysUsuarioAsync(int idUsuario)
        {
            string request = "api/Usuarios/GetKeysUsuario/" + idUsuario;
            return await this.CallApiAsync<KeysUsuario>(request);
        }

        public async Task ActualizarPassword(int idUsuario, byte[] salt, byte[] password, string contrasena)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Usuarios/ActualizarPasswordUsuario";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                var usuarioKeys = new KeysUsuario
                {
                    IdUsuario = idUsuario,
                    Salt = salt,
                    Password = password
                };
                string json = JsonConvert.SerializeObject(usuarioKeys);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);
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

        public async Task<List<Producto>> GetProductosUsuarioAsync(int idUsuario)
        {
            string request = "api/Productos/GetProductos/Usuario/" + idUsuario;
            return await this.CallApiAsync<List<Producto>>(request) ?? new List<Producto>();
        }

        public async Task<Producto> GetProductoByIdAsync(int idProducto)
        {
            string request = "api/Productos/" + idProducto;
            return await this.CallApiAsync<Producto>(request);
        }

        public async Task InsertarProductoAsync(string nombre, string? descripcion, string? imagen, decimal precioUnidad, int unidadMedida, int stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
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
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarProductoAsync(int idProducto, decimal nuevoPrecio, int nuevoStock)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos/ActualizarDatosProductos/" + idProducto + "/" + nuevoPrecio.ToString(CultureInfo.InvariantCulture) + "/" + nuevoStock;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task CambiarEstadoProductoAsync(int idProducto)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Productos/CambiarEstadoProductos/" + idProducto;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task<int> GetStockProductoAsync(int idProducto)
        {
            string request = "api/Productos/GetStockProducto/" + idProducto;
            return await this.CallApiAsync<int>(request);
        }

        public async Task<List<UnidadMedida>> GetUnidadesMedidaAsync()
        {
            string request = "api/UnidadMedida";
            return await this.CallApiAsync<List<UnidadMedida>>(request) ?? new List<UnidadMedida>();
        }

        public async Task InsertarUnidadMedidaAsync(string nombre)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/UnidadMedida/" + Uri.EscapeDataString(nombre);
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PostAsync(request, null);
            }
        }

        public async Task CambiarEstadoUnidadMedidaAsync(int idUnidad)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/UnidadMedida/CambiarEstadoUnidadMedida/" + idUnidad;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
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
            string request = "api/Fincas/GetFincasPendientes";
            return await this.CallApiAsync<List<Finca>>(request) ?? new List<Finca>();
        }

        public async Task<List<Finca>> GetFincasRechazadasAsync()
        {
            string request = "api/Fincas/GetFincasRechazadas";
            return await this.CallApiAsync<List<Finca>>(request) ?? new List<Finca>();
        }

        public async Task<List<Finca>> GetFincasUsuarioAsync(int idUsuario)
        {
            string request = "api/Fincas/GetFincas/Usuario/" + idUsuario;
            return await this.CallApiAsync<List<Finca>>(request) ?? new List<Finca>();
        }

        public async Task<Finca> GetFincaByIdAsync(int idFinca)
        {
            string request = "api/Fincas/" + idFinca;
            return await this.CallApiAsync<Finca>(request);
        }

        public async Task InsertarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                FincaDto finca = new FincaDto
                {
                    Nombre = nombre,
                    Direccion = direccion,
                    Municipio = municipio,
                    Provincia = provincia,
                    Latitud = latitud,
                    Longitud = longitud,
                    IdUsuario = idUsuario
                };
                string json = JsonConvert.SerializeObject(finca);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/Actualizar/" + idFinca;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                FincaDto finca = new FincaDto
                {
                    Nombre = nombre,
                    Direccion = direccion,
                    Municipio = municipio,
                    Provincia = provincia,
                    Latitud = latitud,
                    Longitud = longitud,
                    IdUsuario = idUsuario
                };
                string json = JsonConvert.SerializeObject(finca);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);
            }
        }

        public async Task CambiarEstadoFincaAsync(int idFinca)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/CambiarEstado/" + idFinca;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task CambiarEstadoValidacionFincaAsync(int idFinca, int nuevoEstado)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Fincas/CambiarEstadoValidacion/" + idFinca + "/" + nuevoEstado;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
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
        #endregion

        #region Direcciones
        public async Task<Direccion> GetDireccionByIdAsync(int idDireccion)
        {
            string request = "api/Direcciones/" + idDireccion;
            return await this.CallApiAsync<Direccion>(request);
        }

        public async Task<List<Direccion>> GetDireccionesUsuarioAsync(int idUsuario)
        {
            string request = "api/Direcciones/GetDirecciones/Usuario/" + idUsuario;
            return await this.CallApiAsync<List<Direccion>>(request) ?? new List<Direccion>();
        }

        public async Task InsertarDireccionAsync(int idUsuario, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                DireccionDto direccion = new DireccionDto
                {
                    IdUsuario = idUsuario,
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
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task ActualizarDireccionAsync(int idDireccion, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal, int idUsuario)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones/" + idDireccion;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                DireccionDto direccion = new DireccionDto
                {
                    IdUsuario = idUsuario,
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
                HttpResponseMessage response = await client.PutAsync(request, content);
            }
        }

        public async Task EliminarDireccionAsync(int idDireccion)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Direcciones/" + idDireccion;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.DeleteAsync(request);
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
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Categorias";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                CategoriaDto categoria = new CategoriaDto
                {
                    Nombre = nombre,
                    Descripcion = descripcion
                };
                string json = JsonConvert.SerializeObject(categoria);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task CambiarEstadoCategoriaAsync(int idCategoria)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Categorias/CambiarEstado/" + idCategoria;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
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
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Subcategorias";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                SubcategoriaDto subcategoria = new SubcategoriaDto
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Imagen = imagen,
                    IdCategoria = idCategoria
                };
                string json = JsonConvert.SerializeObject(subcategoria);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task CambiarEstadoSubcategoriaAsync(int idSubcategoria)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Subcategorias/CambiarEstadoSubcategoria/" + idSubcategoria;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }
        #endregion

        #region Pedidos
        public async Task<List<Pedido>> GetPedidosUsuarioAsync(int idUsuario)
        {
            string request = "api/Pedidos/GetPedidos/Usuario/" + idUsuario;
            return await this.CallApiAsync<List<Pedido>>(request) ?? new List<Pedido>();
        }

        public async Task<List<Pedido>> GetPedidosProductosPendientesAsync(int idUsuario)
        {
            string request = "api/Pedidos/GetPedidosProductosPendientes/" + idUsuario;
            return await this.CallApiAsync<List<Pedido>>(request) ?? new List<Pedido>();
        }

        public async Task CambiarEstadoPedido(int idPedido, string nuevoEstado)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos/CambiarEstadoPedido/" + idPedido + "/" + Uri.EscapeDataString(nuevoEstado);
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task<int> CrearPedidoAsync(int idUsuario, int idDireccion)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                PedidoDto pedido = new PedidoDto
                {
                    IdUsuario = idUsuario,
                    IdDireccion = idDireccion
                };
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
            string request = "api/Pedidos/GetDetallePedido/" + idDetalle;
            return await this.CallApiAsync<DetallePedido>(request);
        }

        public async Task CambiarEstadoDetalleProductoAsync(int idDetalle)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pedidos/CambiarEstadoDetallePedido/" + idDetalle;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task<List<DetallePedido>> GetDetallePedidosByPedidoAsync(int idPedido)
        {
            string request = "api/Pedidos/GetDetallesPedido/" + idPedido;
            return await this.CallApiAsync<List<DetallePedido>>(request) ?? new List<DetallePedido>();
        }
        #endregion

        #region Carrito
        public async Task InsertarProductoCarritoAsync(int cantidad, int idUsuario, int idProducto)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                CarritoItemDto carrito = new CarritoItemDto
                {
                    Cantidad = cantidad,
                    IdUsuario = idUsuario,
                    IdProducto = idProducto
                };
                string json = JsonConvert.SerializeObject(carrito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }

        public async Task<CarritoItem> GetProductoCarritoAsync(int idUsuario, int idProducto)
        {
            string request = "api/Carrito/GetProductoCarrito/" + idUsuario + "/" + idProducto;
            return await this.CallApiAsync<CarritoItem>(request);
        }

        public async Task<List<CarritoItem>> GetCarritoUsuarioAsync(int idUsuario)
        {
            string request = "api/Carrito/GetCarrito/Usuario/" + idUsuario;
            return await this.CallApiAsync<List<CarritoItem>>(request) ?? new List<CarritoItem>();
        }

        public async Task ActualizarCantidadCarritoAsync(int idUsuario, int idProducto, int nuevaCantidad)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/ActualizarCantidad";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                CarritoItemDto carrito = new CarritoItemDto
                {
                    IdUsuario = idUsuario,
                    IdProducto = idProducto,
                    Cantidad = nuevaCantidad
                };
                string json = JsonConvert.SerializeObject(carrito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);
            }
        }

        public async Task EliminarProductoCarritoAsync(int idUsuario, int idProducto)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/EliminarProductoCarrito/Usuario/" + idUsuario + "/" + idProducto;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.DeleteAsync(request);
            }
        }

        public async Task EliminarCarritoUsuarioAsync(int idUsuario)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Carrito/EliminarCarrito/" + idUsuario;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.DeleteAsync(request);
            }
        }

        public async Task<decimal> GetSubtotalCarrito(int idUsuario)
        {
            string request = "api/Carrito/GetSubtotalCarrito/Usuario/" + idUsuario;
            var response = await this.CallApiAsync<dynamic>(request);
            return response["subtotal"];
        }
        #endregion

        #region Pagos
        public async Task InsertarPagoUsuarioAsync(int idPedido, string pasarela, string metodo, string ultimosDigitos, string estado, string transactionId)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Pagos";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
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
                HttpResponseMessage response = await client.PostAsync(request, content);
            }
        }
        #endregion

        #region Mensajes
        public async Task InsertarMensajeAsync(int? idUsuario, string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes";
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                MensajeDto mensajeObj = new MensajeDto
                {
                    IdUsuario = idUsuario,
                    Nombre = nombre,
                    Email = email,
                    TipoConsulta = tipoConsulta,
                    Asunto = asunto,
                    Contenido = mensaje
                };
                string json = JsonConvert.SerializeObject(mensajeObj);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);
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
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/MarcarLeido/" + idMensaje;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }

        public async Task MarcarComoRespondidoAsync(int idMensaje)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/Mensajes/MarcarRespondido/" + idMensaje;
                client.BaseAddress = new Uri(this.ApiUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.header);
                HttpResponseMessage response = await client.PutAsync(request, null);
            }
        }
        #endregion
    }
}
