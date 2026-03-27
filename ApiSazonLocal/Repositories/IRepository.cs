using SazonLocalModels.Models;

namespace ApiSazonLocal.Repositories
{
    public interface IRepository
    {

        #region HASHEAR PASSWORDS
        Task<int> UsuariosSinHashAsync();
        #endregion

        #region USUARIOS
        Task RegisterUserAsync(string nombre, string apellidos, string email, string password, string imagen, string telefono, int idRol);
        Task<Usuario> LogInAsync(string email, string password);
        Task<List<Usuario>> GetUsuariosSinHashAsync();
        Task<Usuario> GetUsuarioByIdAsync(int idUsuario);
        Task UpdateUsuario(int idUsuario, string nombre, string apellidos, string telefono, string imagen);
        Task<Usuario> GetUsuarioByEmailAsync(string email);
        Task<List<Usuario>> GetUsuariosAsync();
        Task UpdateEstadoUsuarioAsync(int idUsuario);
        #endregion

        #region KEYS_USUARIO
        Task<KeysUsuario> GetKeysUsuarioAsync(int idUsuario);
        Task ActualizarPassword(int idUsuario, byte[] salt, byte[] password, string contrasena);
        #endregion

        #region PRODUCTOS
        Task<ProductosPaginacion> GetProductosFiltroAsync(int posicion, string buscador, int? idCategoria, int? idSubcategoria, int? idFinca, decimal? precio);
        Task<List<Producto>> GetProductosUsuarioAsync(int idUsuario);
        Task<Producto> GetProductoByIdAsync(int idProducto);
        Task InsertarProductoAsync(string nombre, string? descripcion, string? imagen, decimal precioUnidad, int unidadMedida, int stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria);
        Task ActualizarProductoAsync(int idProducto, decimal precio, int stock);
        Task CambiarEstadoProductoAsync(int idProducto);
        Task ActualizarStockCompraAsync(int idProducto, int cantidadComprada);
        Task<int> GetStockProductoAsync(int idProducto);
        #endregion

        #region UNIDAD MEDIDA
        Task<List<UnidadMedida>> GetUnidadesMedidaAsync();
        Task InsertarUnidadMedidaAsync(string nombre);
        Task<UnidadMedida> GetUnidadMedidaByIdAsync(int idUnidad);
        Task CambiarEstadoUnidadMedidaAsync(int idUnidad);
        #endregion

        #region CATEGORIAS
        Task<List<Categoria>> GetCategoriasAsync();
        Task InsertarCategoriaAsync(string nombre, string descripcion);
        Task<Categoria> GetCategoriaByIdAsync(int idCategoria);
        Task CambiarEstadoCategoriaAsync(int idCategoria);
        #endregion

        #region SUBCATEGORIAS
        Task<List<Subcategoria>> GetSubcategoriasAsync();
        Task<List<Subcategoria>> GetSubcategoriasByCategoriaAsync(int idCategoria);
        Task<List<Subcategoria>> GetSubcategoriasConCategoriaAsync();
        Task InsertarSubcategoriaAsync(string nombre, string descripcion, string imagen, int idCategoria);
        Task<Subcategoria> GetSubcategoriaByIdAsync(int idSubcategoria);
        Task CambiarEstadoSubcategoriaAsync(int idSubcategoria);
        #endregion

        #region FINCAS
        Task<List<Finca>> GetFincasActivasAsync();
        Task<List<Finca>> GetFincasPendientesAsync();
        Task<List<Finca>> GetFincasRechazadasAsync();
        Task<List<Finca>> GetFincasUsuarioAsync(int idUsuario);
        Task<Finca> GetFincaByIdAsync(int idFinca);
        Task InsertarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario);
        Task ActualizarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario);
        Task CambiarEstadoFincaAsync(int idFinca);
        Task CambiarEstadoValidacionFincaAsync(int idFinca, int nuevoEstado);
        Task<List<Finca>> GetFincasAdminAsync(int? estado);
        #endregion

        #region DIRECCIONES
        Task<Direccion> GetDireccionByIdAsync(int idDireccion);
        Task<List<Direccion>> GetDireccionesUsuarioAsync(int idUsuario);
        Task InsertarDireccionAsync(int idUsuario, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal lat, decimal lon, bool esPrincipal);
        Task ActualizarDireccionAsync(int idDireccion, string? etiqueta, string calleNumero, string? piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal, int idUsuario);
        Task EliminarDireccionAsync(int idDireccion);
        #endregion

        #region CARRITO ITEMS
        Task InsertarProductoCarritoAsync(int cantidad, int idUsuario, int idProducto);
        Task<CarritoItem> GetProductoCarritoAsync(int idUsuario, int idProducto);
        Task<List<CarritoItem>> GetCarritoUsuarioAsync(int idUsuario);
        Task ActualizarCantidadCarritoAsync(int idUsuario, int idProducto, int nuevaCantidad);
        Task EliminarProductoCarritoAsync(int idUsuario, int idProducto);
        Task EliminarCarritoUsuarioAsync(int idUsuario);
        Task<decimal> GetSubtotalCarrito(int idUsuario);

        #endregion

        #region PEDIDOS
        Task<List<Pedido>> GetPedidosUsuarioAsync(int idUsuario);
        Task<List<Pedido>> GetPedidosProductosPendientesAsync(int idUsuario);
        Task CambiarEstadoPedido(int idPedido, string nuevoEstado);
        Task<int> CrearPedidoAsync(int idUsuario, int idDireccion);
        Task<Pedido> GetPedidoByIdAsync(int idPedido);
        Task<List<Pedido>> GetPedidosAsync();
        #endregion

        #region DETALLE PEDIDOS
        Task InsertarDetallePedidosAsync(int idUsuario, int idPedido);
        Task<DetallePedido> GetDetallePedidoByIdAsync(int idDetalle);
        Task CambiarEstadoDetalleProductoAsync(int idDetalle);
        Task<List<DetallePedido>> GetDetallePedidosByPedidoAsync(int idPedido);
        #endregion

        #region PAGOS
        Task InsertarPagoUsuarioAsync(int idPedido, string pasarela, string metodo, string ultimosDigitos, string estado, string transactionId);
        #endregion

        #region MENSAJES
        Task InsertarMensajeAsync(int? idUsuario, string nombre, string email, string tipoConsulta, string asunto, string mensaje);
        Task<List<Mensaje>> GetMensajesAsync();
        Task<Mensaje> GetMensajeByIdAsync(int idMensaje);
        Task MarcarMensajeLeidoAsync(int idMnesaje);
        Task MarcarComoRespondidoAsync(int idMensaje);
        #endregion

    }
}
