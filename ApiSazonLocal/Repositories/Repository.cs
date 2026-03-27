using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ApiSazonLocal.Data;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiSazonLocal.Repositories
{
    public class Repository: IRepository
    {
        private SazonContext context;

        public Repository(SazonContext context)
        {
            this.context = context;
        }

        #region HASHEAR PASSWORDS
        public async Task<int> UsuariosSinHashAsync()
        {
            return await this.context.Usuarios.Where(u => !this.context.KeysUsuarios.Any(k => k.IdUsuario == u.IdUsuario)).CountAsync();
        }
        #endregion

        #region BUSCAR ID
        private async Task<int> GetNextIdAsync(string nombreTabla)
        {
            string sql = "SP_NEXT_ID @tabla, @siguienteId OUT";
            var pamTab = new SqlParameter("@Tabla", nombreTabla);
            var pamId = new SqlParameter("SiguienteId", -1);
            pamId.Direction = ParameterDirection.Output;
            
            await this.context.Database.ExecuteSqlRawAsync(sql, pamTab, pamId);

            return (int)pamId.Value;
        }
        #endregion
        #region USUARIOS
        public async Task RegisterUserAsync(string nombre, string apellidos, string email, string password, string imagen, string telefono, int idRol)
        {
            int id = await this.GetNextIdAsync("USUARIOS");
            Usuario user = new Usuario {
                IdUsuario = id,
                Nombre = nombre,
                Apellidos = apellidos,
                Email = email,
                Contrasena = password,
                Imagen = imagen,
                Telefono = telefono,
                EstaActivo = true,
                IdRol = idRol
            };
            await this.context.Usuarios.AddAsync(user);
            await this.context.SaveChangesAsync();

            byte[] salt = HelperAuth.GenerarSalt();
            byte[] passwordHash = HelperAuth.EncryptPassword(password, salt);
            KeysUsuario key = new KeysUsuario {
                IdUsuario = id,
                Salt = salt,
                Password = passwordHash,
            };
            await this.context.KeysUsuarios.AddAsync(key);
            await this.context.SaveChangesAsync();
        }

        public async Task<Usuario> LogInAsync(string email, string password)
        {
            Usuario user = await this.context.Usuarios.Where(u => u.Email == email).Include(u => u.Keys).FirstOrDefaultAsync();

            if (user != null)
            {
                byte[] salt = user.Keys.Salt;
                byte[] temp = HelperAuth.EncryptPassword(password, salt);
                byte[] passBytes = user.Keys.Password;

                if (HelperAuth.CompararPasswords(temp, passBytes))
                {
                    return user;
                }
            }
            return null;
        }

        public async Task<List<Usuario>> GetUsuariosSinHashAsync()
        {
            return await this.context.Usuarios.Where(u => !this.context.KeysUsuarios.Any(k => k.IdUsuario == u.IdUsuario)).ToListAsync();
        }

        public async Task<Usuario> GetUsuarioByIdAsync(int idUsuario)
        {
            return await this.context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        }

        public async Task UpdateUsuario(int idUsuario, string nombre, string apellidos, string telefono, string imagen)
        {
            var usuario = await this.GetUsuarioByIdAsync(idUsuario);
            if (usuario != null)
            {
                usuario.Nombre = nombre;
                usuario.Apellidos = apellidos;
                usuario.Telefono = telefono;

                if (!string.IsNullOrEmpty(imagen) && usuario.Imagen != imagen)
                {
                    usuario.Imagen = imagen;
                }
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<Usuario> GetUsuarioByEmailAsync(string email)
        {
            return await this.context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            return await this.context.Usuarios.ToListAsync();
        }

        public async Task UpdateEstadoUsuarioAsync(int idUsuario)
        {
            Usuario usuario = await this.GetUsuarioByIdAsync(idUsuario);
            usuario.EstaActivo = !usuario.EstaActivo;
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region KEYS_USUARIOS
        public async Task<KeysUsuario> GetKeysUsuarioAsync(int idUsuario)
        {
            return await this.context.KeysUsuarios.FirstOrDefaultAsync(k => k.IdUsuario == idUsuario);
        }

        public async Task ActualizarPassword(int idUsuario, byte[] salt, byte[] password, string contrasena)
        {
            KeysUsuario key = await this.GetKeysUsuarioAsync(idUsuario);
            key.Salt = salt;
            key.Password = password;
            Usuario usuario = await this.GetUsuarioByIdAsync(idUsuario);
            usuario.Contrasena = contrasena;
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region PRODUCTOS
        public async Task<ProductosPaginacion> GetProductosFiltroAsync(int posicion, string buscador, int? idCategoria, int? idSubcategoria, int? idFinca, decimal? precio)
        {
            var consulta = this.context.Productos.AsQueryable();

            if (!string.IsNullOrEmpty(buscador))
                consulta = consulta.Where(p => EF.Functions.Collate(p.Nombre, "Modern_Spanish_CI_AI").Contains(EF.Functions.Collate(buscador, "Modern_Spanish_CI_AI")));

            if (idCategoria.HasValue) consulta = consulta.Where(p => p.IdCategoria == idCategoria);
            if (idSubcategoria.HasValue) consulta = consulta.Where(p => p.IdSubcategoria == idSubcategoria);
            if (idFinca.HasValue) consulta = consulta.Where(p => p.IdFinca == idFinca);
            if (precio.HasValue) consulta = consulta.Where(p => p.PrecioUnidad <= precio);


            return new ProductosPaginacion
            {
                Productos = await consulta.Where(p => p.EstaActivo == true)
                                .Include(p => p.UnidadMedida)
                               .OrderBy(p => p.IdProducto)
                               .Skip(posicion)
                               .Take(40)
                               .ToListAsync(),
                TotalProductos = await consulta.CountAsync(),
                NumeroRegistros = (int)Math.Ceiling((double)(await consulta.CountAsync()) / 40)
            };
        }
        public async Task<List<Producto>> GetProductosUsuarioAsync(int idUsuario)
        {
            return await this.context.Productos
                .Where(p => p.Finca.IdAgricultor == idUsuario)
                .Select(p => new Producto
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Imagen = p.Imagen,
                    PrecioUnidad = p.PrecioUnidad,
                    Stock = p.Stock,
                    EstaActivo = p.EstaActivo,
                    IdUnidadMedida = p.IdUnidadMedida,
                    IdFinca = p.IdFinca,
                    IdCategoria = p.IdCategoria,
                    IdSubcategoria = p.IdSubcategoria,
                    UnidadMedida = new UnidadMedida
                    {
                        IdUnidadMedida = p.UnidadMedida.IdUnidadMedida,
                        Nombre = p.UnidadMedida.Nombre,
                        EstaActiva = p.UnidadMedida.EstaActiva
                    },
                    Finca = new Finca
                    {
                        IdFinca = p.Finca.IdFinca,
                        Nombre = p.Finca.Nombre,
                        Direccion = p.Finca.Direccion,
                        Municipio = p.Finca.Municipio,
                        Provincia = p.Finca.Provincia,
                        EstaActiva = p.Finca.EstaActiva
                    },
                    Categoria = new Categoria
                    {
                        IdCategoria = p.Categoria.IdCategoria,
                        Nombre = p.Categoria.Nombre,
                        Descripcion = p.Categoria.Descripcion,
                        EstaActiva = p.Categoria.EstaActiva
                    },
                    Subcategoria = new Subcategoria
                    {
                        IdSubcategoria = p.Subcategoria.IdSubcategoria,
                        Nombre = p.Subcategoria.Nombre,
                        Descripcion = p.Subcategoria.Descripcion,
                        Imagen = p.Subcategoria.Imagen,
                        EstaActiva = p.Subcategoria.EstaActiva
                    }
                })
                .ToListAsync();
        }
        public async Task<Producto> GetProductoByIdAsync(int idProducto)
        {
            return await this.context.Productos
                .Where(p => p.IdProducto == idProducto)
                .Select(p => new Producto
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Imagen = p.Imagen,
                    PrecioUnidad = p.PrecioUnidad,
                    Stock = p.Stock,
                    EstaActivo = p.EstaActivo,
                    IdUnidadMedida = p.IdUnidadMedida,
                    IdFinca = p.IdFinca,
                    IdCategoria = p.IdCategoria,
                    IdSubcategoria = p.IdSubcategoria,
                    UnidadMedida = new UnidadMedida
                    {
                        IdUnidadMedida = p.UnidadMedida.IdUnidadMedida,
                        Nombre = p.UnidadMedida.Nombre,
                        EstaActiva = p.UnidadMedida.EstaActiva
                    },
                    Subcategoria = new Subcategoria
                    {
                        IdSubcategoria = p.Subcategoria.IdSubcategoria,
                        Nombre = p.Subcategoria.Nombre,
                        Descripcion = p.Subcategoria.Descripcion,
                        Imagen = p.Subcategoria.Imagen,
                        EstaActiva = p.Subcategoria.EstaActiva
                    }
                })
                .FirstOrDefaultAsync();
        }

        public async Task InsertarProductoAsync(string nombre, string? descripcion, string? imagen, decimal precioUnidad, int unidadMedida, int stock, bool estaActivo, int idFinca, int idCategoria, int idSubcategoria)
        {
            int idProducto = await this.GetNextIdAsync("PRODUCTOS");
            Producto pro = new Producto
            {
                IdProducto = idProducto,
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
            this.context.Productos.Add(pro);
            this.context.SaveChangesAsync();
        }

        public async Task ActualizarProductoAsync(int idProducto, decimal nuevoPrecio, int nuevoStock)
        {
            Producto pro = await this.GetProductoByIdAsync(idProducto);
            pro.PrecioUnidad = nuevoPrecio;
            pro.Stock = nuevoStock;
            if(pro.Stock == 0){
                pro.EstaActivo = false;
            }
            await this.context.SaveChangesAsync();
        }

        public async Task CambiarEstadoProductoAsync(int idProducto)
        {
            Producto pro = await this.GetProductoByIdAsync(idProducto);
            pro.EstaActivo = !pro.EstaActivo;
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarStockCompraAsync(int idProducto, int cantidadComprada)
        {
            Producto producto = await this.GetProductoByIdAsync(idProducto);
            if (producto != null)
            {
                producto.Stock -= cantidadComprada;
                if (producto.Stock <= 0)
                {
                    producto.Stock = 0;
                    producto.EstaActivo = false;
                }
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<int> GetStockProductoAsync(int idProducto)
        {
            return await this.context.Productos.Where(p => p.IdProducto == idProducto).Select(p => p.Stock).FirstOrDefaultAsync();
        }
        #endregion
        #region UNIDAD MEDIDA
        public async Task<List<UnidadMedida>> GetUnidadesMedidaAsync()
        {
            return await this.context.UnidadMedidas.ToListAsync();
        }

        public async Task InsertarUnidadMedidaAsync(string nombre)
        {
            int idUnidad = await this.GetNextIdAsync("UNIDAD_MEDIDAS");
            UnidadMedida unidad = new UnidadMedida
            {
                IdUnidadMedida = idUnidad,
                Nombre = nombre,
                EstaActiva = true
            };
            this.context.UnidadMedidas.Add(unidad);
            await this.context.SaveChangesAsync();
        }

        public async Task<UnidadMedida> GetUnidadMedidaByIdAsync(int idUnidad)
        {
            return await this.context.UnidadMedidas.FirstOrDefaultAsync(u => u.IdUnidadMedida == idUnidad);
        }

        public async Task CambiarEstadoUnidadMedidaAsync(int idUnidad)
        {
            UnidadMedida unidad = await this.GetUnidadMedidaByIdAsync(idUnidad);
            unidad.EstaActiva = !unidad.EstaActiva;
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region FINCAS
        public async Task<List<Finca>> GetFincasActivasAsync()
        {
            return await this.context.Fincas.Where(f => f.EstaActiva == true).ToListAsync();
        }
        public async Task<List<Finca>> GetFincasPendientesAsync()
        {
            return await this.context.Fincas.Where(f => f.EstaValidada == 1).ToListAsync();
        }
        public async Task<List<Finca>> GetFincasRechazadasAsync()
        {
            return await this.context.Fincas.Where(f => f.EstaValidada == 3).ToListAsync();
        }

        
        public async Task<List<Finca>> GetFincasUsuarioAsync(int idUsuario)
        {
            return await this.context.Fincas
                .Where(f => f.IdAgricultor == idUsuario)
                .Select(f => new Finca
                {
                    IdFinca = f.IdFinca,
                    Nombre = f.Nombre,
                    Direccion = f.Direccion,
                    Municipio = f.Municipio,
                    Provincia = f.Provincia,
                    Latitud = f.Latitud,
                    Longitud = f.Longitud,
                    EstaActiva = f.EstaActiva,
                    EstaValidada = f.EstaValidada,
                    IdAgricultor = f.IdAgricultor,
                    Agricultor = new Usuario
                    {
                        IdUsuario = f.Agricultor.IdUsuario,
                        Nombre = f.Agricultor.Nombre,
                        Apellidos = f.Agricultor.Apellidos,
                        Email = f.Agricultor.Email,
                        Imagen = f.Agricultor.Imagen,
                        Telefono = f.Agricultor.Telefono,
                        EstaActivo = f.Agricultor.EstaActivo
                    }
                })
                .ToListAsync();
        }
        public async Task<Finca> GetFincaByIdAsync(int idFinca)
        {
            return await this.context.Fincas.Include(f => f.Productos).FirstOrDefaultAsync(f => f.IdFinca == idFinca);
        }
        public async Task InsertarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            idFinca = await this.GetNextIdAsync("FINCAS");
            Finca finca = new Finca
            {
                IdFinca = idFinca,
                Nombre = nombre,
                Direccion = direccion,
                Municipio = municipio,
                Provincia = provincia,
                Latitud = latitud,
                Longitud = longitud,
                EstaActiva = false,
                EstaValidada = 1,
                IdAgricultor = idUsuario,
            };
            this.context.Fincas.Add(finca);
            await this.context.SaveChangesAsync();
        }
        public async Task ActualizarFincaAsync(int idFinca, string nombre, string direccion, string municipio, string provincia, decimal latitud, decimal longitud, int idUsuario)
        {
            Finca finca = await this.GetFincaByIdAsync(idFinca);
            finca.Nombre = nombre;
            finca.Direccion = direccion;
            finca.Municipio = municipio;
            finca.Provincia = provincia;
            finca.Latitud = latitud;
            finca.Longitud = longitud;
            await this.context.SaveChangesAsync();
        }

        public async Task CambiarEstadoFincaAsync(int idFinca)
        {
            Finca finca = await this.GetFincaByIdAsync(idFinca);
            bool estabaActiva = finca.EstaActiva;
            finca.EstaActiva = !finca.EstaActiva;

            foreach (Producto pro in finca.Productos)
            {
                if (estabaActiva)
                {
                    if (pro.EstaActivo)
                    {
                        pro.EstaActivo = false;
                    }
                }
                else
                {
                    if (pro.Stock > 0)
                    {
                        pro.EstaActivo = true;
                    }
                }
            }
            await this.context.SaveChangesAsync();
        }

        public async Task CambiarEstadoValidacionFincaAsync(int idFinca, int nuevoEstado)
        {
            Finca finca = await this.GetFincaByIdAsync(idFinca);
            finca.EstaValidada = nuevoEstado;
            if(nuevoEstado == 3)
            {
                finca.EstaActiva = false;
            }
            else if(nuevoEstado == 2)
            {
                finca.EstaActiva = false;
            }
                await this.context.SaveChangesAsync();
        }

        public async Task<List<Finca>> GetFincasAdminAsync(int? estado)
        {
            var consulta = this.context.Fincas.AsQueryable();
            if (estado.HasValue)
            {
                consulta = consulta.Where(f => f.EstaValidada == estado.Value);
            }
            return await consulta
                .OrderBy(f => f.EstaValidada)
                .ThenByDescending(f => f.IdFinca)
                .Select(f => new Finca
                {
                    IdFinca = f.IdFinca,
                    Nombre = f.Nombre,
                    Direccion = f.Direccion,
                    Municipio = f.Municipio,
                    Provincia = f.Provincia,
                    Latitud = f.Latitud,
                    Longitud = f.Longitud,
                    EstaActiva = f.EstaActiva,
                    EstaValidada = f.EstaValidada,
                    IdAgricultor = f.IdAgricultor,
                    Agricultor = new Usuario
                    {
                        IdUsuario = f.Agricultor.IdUsuario,
                        Nombre = f.Agricultor.Nombre,
                        Apellidos = f.Agricultor.Apellidos,
                        Email = f.Agricultor.Email,
                        Imagen = f.Agricultor.Imagen,
                        Telefono = f.Agricultor.Telefono,
                        EstaActivo = f.Agricultor.EstaActivo
                    }
                })
                .ToListAsync();
        }
        #endregion
        #region DIRECCIONES
        public async Task<Direccion> GetDireccionByIdAsync(int idDireccion) {
            return await this.context.Direcciones.FirstOrDefaultAsync(d => d.IdDireccion == idDireccion);
        }
        public async Task<List<Direccion>> GetDireccionesUsuarioAsync(int idUsuario)
        {
            return await this.context.Direcciones
                .Where(d => d.IdUsuario == idUsuario)
                .Select(d => new Direccion
                {
                    IdDireccion = d.IdDireccion,
                    IdUsuario = d.IdUsuario,
                    NombreEtiqueta = d.NombreEtiqueta,
                    CalleNumero = d.CalleNumero,
                    Piso = d.Piso,
                    Puerta = d.Puerta,
                    CodigoPostal = d.CodigoPostal,
                    Municipio = d.Municipio,
                    Provincia = d.Provincia,
                    NotasAdicionales = d.NotasAdicionales,
                    Latitud = d.Latitud,
                    Longitud = d.Longitud,
                    EsPrincipal = d.EsPrincipal,
                    Usuario = new Usuario
                    {
                        IdUsuario = d.Usuario.IdUsuario,
                        Nombre = d.Usuario.Nombre,
                        Apellidos = d.Usuario.Apellidos,
                        Email = d.Usuario.Email,
                        Imagen = d.Usuario.Imagen,
                        Telefono = d.Usuario.Telefono,
                        EstaActivo = d.Usuario.EstaActivo
                    }
                })
                .ToListAsync();
        }
        public async Task InsertarDireccionAsync(int idUsuario, string? etiqueta, string calleNumero,string? piso, string?puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal)
        {
            int idDir = await this.GetNextIdAsync("DIRECCIONES");
            if (esPrincipal)
            {
                var dirAntiguas = await this.context.Direcciones.Where(d => d.IdUsuario == idUsuario && d.EsPrincipal == true).ToListAsync();

                foreach (var d in dirAntiguas)
                {
                    d.EsPrincipal = false;
                }
            }

            Direccion nuevaDir = new Direccion
            {
                IdDireccion = idDir,
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
            this.context.Direcciones.Add(nuevaDir);
            await this.context.SaveChangesAsync();
        }
        public async Task ActualizarDireccionAsync(int idDireccion, string? etiqueta, string calleNumero, string?piso, string? puerta, string cp, string municipio, string provincia, string? notasAdicionales, decimal latitud, decimal longitud, bool esPrincipal, int idUsuario)
        {
            if (esPrincipal)
            {
                var dirAntiguas = await this.context.Direcciones.Where(d => d.IdUsuario == idUsuario && d.EsPrincipal == true).ToListAsync();

                foreach (var d in dirAntiguas)
                {
                    d.EsPrincipal = false;
                }
            }

            Direccion dir = await this.GetDireccionByIdAsync(idDireccion);
            dir.NombreEtiqueta = etiqueta;
            dir.CalleNumero = calleNumero;
            dir.Piso = piso;
            dir.Puerta = puerta;
            dir.Municipio = municipio;
            dir.Provincia = provincia;
            dir.NotasAdicionales = notasAdicionales;
            dir.CodigoPostal = cp;
            dir.Latitud = latitud;
            dir.Longitud = longitud;
            dir.EsPrincipal = esPrincipal;
            await this.context.SaveChangesAsync();
        }
        public async Task EliminarDireccionAsync(int idDireccion)
        {
            Direccion dir = await this.GetDireccionByIdAsync(idDireccion);
            this.context.Direcciones.Remove(dir);
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region CATEGORIAS
        public async Task<List<Categoria>> GetCategoriasAsync()
        {
            return await this.context.Categorias.ToListAsync();
        }

        public async Task InsertarCategoriaAsync(string nombre, string descripcion)
        {
            int idCategoria = await this.GetNextIdAsync("CATEGORIAS");
            Categoria categoria = new Categoria
            {
                IdCategoria = idCategoria,
                Nombre = nombre,
                Descripcion = descripcion,
                EstaActiva = true
            };
            this.context.Categorias.Add(categoria);
            await this.context.SaveChangesAsync();
        }

        public async Task<Categoria> GetCategoriaByIdAsync(int idCategoria)
        {
            return await this.context.Categorias.Include(c => c.Subcategorias).FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);
        }

        public async Task CambiarEstadoCategoriaAsync(int idCategoria)
        {
            Categoria categoria = await this.GetCategoriaByIdAsync(idCategoria);
            bool estabaActiva = categoria.EstaActiva;
            categoria.EstaActiva = !categoria.EstaActiva;
            foreach(Subcategoria sub in categoria.Subcategorias)
            {
                if (estabaActiva)
                {
                    sub.EstaActiva = false;
                }
                else
                {
                    sub.EstaActiva = true;
                }
            }
            
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region SUBCATEGORIAS
        public async Task<List<Subcategoria>> GetSubcategoriasAsync()
        {
            return await this.context.Subcategorias.ToListAsync();
        }

        public async Task<List<Subcategoria>> GetSubcategoriasByCategoriaAsync(int idCategoria)
        {
            return await this.context.Subcategorias.Where(s => s.IdCategoria == idCategoria).ToListAsync();
        }

        public async Task<List<Subcategoria>> GetSubcategoriasConCategoriaAsync() {
            return await this.context.Subcategorias
                .Select(s => new Subcategoria
                {
                    IdSubcategoria = s.IdSubcategoria,
                    IdCategoria = s.IdCategoria,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    EstaActiva = s.EstaActiva,
                    Imagen = s.Imagen,
                    Categoria = new Categoria
                    {
                        IdCategoria = s.Categoria.IdCategoria,
                        Nombre = s.Categoria.Nombre,
                        Descripcion = s.Categoria.Descripcion,
                        EstaActiva = s.Categoria.EstaActiva
                    }
                })
                .ToListAsync();
        }

        public async Task InsertarSubcategoriaAsync(string nombre, string descripcion, string imagen, int idCategoria)
        {
            int idSubategoria = await this.GetNextIdAsync("SUBCATEGORIAS");
            Subcategoria subcategoria = new Subcategoria
            {
                IdSubcategoria = idSubategoria,
                IdCategoria = idCategoria,
                Nombre = nombre,
                Descripcion = descripcion,
                EstaActiva = true,
                Imagen = imagen
            };
            this.context.Subcategorias.Add(subcategoria);
            await this.context.SaveChangesAsync();
        }

        public async Task<Subcategoria> GetSubcategoriaByIdAsync(int idSubcategoria)
        {
            return await this.context.Subcategorias.Include(s =>s.Categoria).FirstOrDefaultAsync(s => s.IdSubcategoria == idSubcategoria);
        }

        public async Task CambiarEstadoSubcategoriaAsync(int idSubcategoria)
        {
            Subcategoria subcategoria = await this.GetSubcategoriaByIdAsync(idSubcategoria);
            if (subcategoria.Categoria.EstaActiva)
            {
                subcategoria.EstaActiva = !subcategoria.EstaActiva;
                await this.context.SaveChangesAsync();
            }
            
        }
        #endregion
        #region PEDIDOS
        public async Task<List<Pedido>> GetPedidosUsuarioAsync(int idUsuario)
        {
            return await this.context.Pedidos
                .Where(p => p.IdUsuario == idUsuario)
                .Select(p => new Pedido
                {
                    IdPedido = p.IdPedido,
                    IdUsuario = p.IdUsuario,
                    IdDireccion = p.IdDireccion,
                    FechaPedido = p.FechaPedido,
                    Subtotal = p.Subtotal,
                    TasaGestion = p.TasaGestion,
                    GastosEnvio = p.GastosEnvio,
                    Total = p.Total,
                    Estado = p.Estado,
                    DetallesPedido = p.DetallesPedido.Select(d => new DetallePedido
                    {
                        IdDetalle = d.IdDetalle,
                        IdPedido = d.IdPedido,
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Listo = d.Listo,
                        Producto = new Producto
                        {
                            IdProducto = d.Producto.IdProducto,
                            Nombre = d.Producto.Nombre,
                            Descripcion = d.Producto.Descripcion,
                            Imagen = d.Producto.Imagen,
                            PrecioUnidad = d.Producto.PrecioUnidad,
                            Stock = d.Producto.Stock,
                            EstaActivo = d.Producto.EstaActivo
                        }
                    }).ToList()
                })
                .ToListAsync();
        }
        public async Task<List<Pedido>> GetPedidosProductosPendientesAsync(int idUsuario) 
        {  
            return await this.context.Pedidos
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Finca)
                .Where(p => p.DetallesPedido.Any(d =>
                    d.Producto.Finca.IdAgricultor == idUsuario &&
                    d.Listo == false))
                .ToListAsync();
        }
        
        public async Task CambiarEstadoPedido(int idPedido, string nuevoEstado)
        {
            Pedido pedido = await this.context.Pedidos.Where(p => p.IdPedido == idPedido).FirstOrDefaultAsync();
            pedido.Estado = nuevoEstado.ToUpper();
            this.context.SaveChangesAsync();
        }

        public async Task<int> CrearPedidoAsync(int idUsuario, int idDireccion)
        {
            decimal subtotal = await this.GetSubtotalCarrito(idUsuario);
            decimal tasaGestion = subtotal * 0.05m;
            decimal gastosEnvio = 0;
            if(subtotal < 50)
            {
                gastosEnvio = 3.99m;
            }
            decimal total = subtotal + gastosEnvio + tasaGestion;

            int idPedido = await this.GetNextIdAsync("PEDIDOS");

            Pedido pedido = new Pedido
            {
                IdPedido = idPedido,
                IdUsuario = idUsuario,
                IdDireccion = idDireccion,
                FechaPedido = DateTime.Now,
                Subtotal = subtotal,
                TasaGestion = tasaGestion,
                GastosEnvio = gastosEnvio,
                Total = total,
                Estado = "PENDIENTE"
            };
            this.context.Pedidos.Add(pedido);
            await this.context.SaveChangesAsync();
            await this.InsertarDetallePedidosAsync(idUsuario, idPedido);
            return pedido.IdPedido;
        }

        public async Task<Pedido> GetPedidoByIdAsync(int idPedido)
        {
            return await this.context.Pedidos
                .Where(p => p.IdPedido == idPedido)
                .Select(p => new Pedido
                {
                    IdPedido = p.IdPedido,
                    IdUsuario = p.IdUsuario,
                    IdDireccion = p.IdDireccion,
                    FechaPedido = p.FechaPedido,
                    Subtotal = p.Subtotal,
                    TasaGestion = p.TasaGestion,
                    GastosEnvio = p.GastosEnvio,
                    Total = p.Total,
                    Estado = p.Estado,
                    Cliente = new Usuario
                    {
                        IdUsuario = p.Cliente.IdUsuario,
                        Nombre = p.Cliente.Nombre,
                        Apellidos = p.Cliente.Apellidos,
                        Email = p.Cliente.Email,
                        Telefono = p.Cliente.Telefono,
                        Imagen = p.Cliente.Imagen
                    },
                    Direccion = new Direccion
                    {
                        IdDireccion = p.Direccion.IdDireccion,
                        NombreEtiqueta = p.Direccion.NombreEtiqueta,
                        CalleNumero = p.Direccion.CalleNumero,
                        Piso = p.Direccion.Piso,
                        Puerta = p.Direccion.Puerta,
                        CodigoPostal = p.Direccion.CodigoPostal,
                        Municipio = p.Direccion.Municipio,
                        Provincia = p.Direccion.Provincia,
                        Latitud = p.Direccion.Latitud,
                        Longitud = p.Direccion.Longitud
                    },
                    DetallesPedido = p.DetallesPedido.Select(d => new DetallePedido
                    {
                        IdDetalle = d.IdDetalle,
                        IdPedido = d.IdPedido,
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Listo = d.Listo,
                        Producto = new Producto
                        {
                            IdProducto = d.Producto.IdProducto,
                            Nombre = d.Producto.Nombre,
                            Descripcion = d.Producto.Descripcion,
                            Imagen = d.Producto.Imagen,
                            PrecioUnidad = d.Producto.PrecioUnidad,
                            Stock = d.Producto.Stock,
                            EstaActivo = d.Producto.EstaActivo
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<Pedido>> GetPedidosAsync()
        {
            return await this.context.Pedidos
                .Select(p => new Pedido
                {
                    IdPedido = p.IdPedido,
                    IdUsuario = p.IdUsuario,
                    IdDireccion = p.IdDireccion,
                    FechaPedido = p.FechaPedido,
                    Subtotal = p.Subtotal,
                    TasaGestion = p.TasaGestion,
                    GastosEnvio = p.GastosEnvio,
                    Total = p.Total,
                    Estado = p.Estado,
                    Cliente = new Usuario
                    {
                        IdUsuario = p.Cliente.IdUsuario,
                        Nombre = p.Cliente.Nombre,
                        Apellidos = p.Cliente.Apellidos,
                        Email = p.Cliente.Email,
                        Telefono = p.Cliente.Telefono
                    },
                    DetallesPedido = p.DetallesPedido.Select(d => new DetallePedido
                    {
                        IdDetalle = d.IdDetalle,
                        IdPedido = d.IdPedido,
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Listo = d.Listo,
                        Producto = new Producto
                        {
                            IdProducto = d.Producto.IdProducto,
                            Nombre = d.Producto.Nombre,
                            PrecioUnidad = d.Producto.PrecioUnidad,
                            Stock = d.Producto.Stock,
                            EstaActivo = d.Producto.EstaActivo,
                            IdFinca = d.Producto.IdFinca,
                            Finca = new Finca
                            {
                                IdFinca = d.Producto.Finca.IdFinca,
                                Nombre = d.Producto.Finca.Nombre,
                                EstaActiva = d.Producto.Finca.EstaActiva
                            }
                        }
                    }).ToList()
                })
                .ToListAsync();
        }
        #endregion
        #region DETALLE PEDIDOS
        public async Task InsertarDetallePedidosAsync(int idUsuario, int idPedido)
        {
            List<CarritoItem> carritoItems = await this.GetCarritoUsuarioAsync(idUsuario);
            int nextIdDetalle = await this.GetNextIdAsync("DETALLE_PEDIDOS");

            foreach (var item in carritoItems)
            {
                DetallePedido detalle = new DetallePedido
                {
                    IdDetalle = nextIdDetalle,
                    IdPedido = idPedido,
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto.PrecioUnidad
                };
                this.context.DetallePedidos.Add(detalle);
                await this.ActualizarStockCompraAsync(item.IdProducto, item.Cantidad);
                nextIdDetalle++;
            }
            await this.context.SaveChangesAsync();
        }

        public async Task<DetallePedido> GetDetallePedidoByIdAsync(int idDetalle)
        {
            return await this.context.DetallePedidos.FirstOrDefaultAsync(d => d.IdDetalle == idDetalle);
        }

        public async Task CambiarEstadoDetalleProductoAsync(int idDetalle)
        {
            DetallePedido detalle = await this.GetDetallePedidoByIdAsync(idDetalle);
            detalle.Listo = !detalle.Listo;
            await this.context.SaveChangesAsync();
        }

        public async Task<List<DetallePedido>> GetDetallePedidosByPedidoAsync(int idPedido)
        {
            return await this.context.DetallePedidos
                .Where(d => d.IdPedido == idPedido)
                .Select(d => new DetallePedido
                {
                    IdDetalle = d.IdDetalle,
                    IdPedido = d.IdPedido,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Listo = d.Listo,
                    Producto = new Producto
                    {
                        IdProducto = d.Producto.IdProducto,
                        Nombre = d.Producto.Nombre,
                        Descripcion = d.Producto.Descripcion,
                        PrecioUnidad = d.Producto.PrecioUnidad,
                        Stock = d.Producto.Stock,
                        IdFinca = d.Producto.IdFinca,
                        Finca = new Finca
                        {
                            IdFinca = d.Producto.Finca.IdFinca,
                            Nombre = d.Producto.Finca.Nombre,
                            Direccion = d.Producto.Finca.Direccion,
                            IdAgricultor = d.Producto.Finca.IdAgricultor,
                            Agricultor = new Usuario
                            {
                                IdUsuario = d.Producto.Finca.Agricultor.IdUsuario,
                                Nombre = d.Producto.Finca.Agricultor.Nombre,
                                Apellidos = d.Producto.Finca.Agricultor.Apellidos,
                                Email = d.Producto.Finca.Agricultor.Email,
                                Telefono = d.Producto.Finca.Agricultor.Telefono
                            }
                        }
                    }
                })
                .ToListAsync();
        }
        #endregion
        #region CARRITO
        public async Task InsertarProductoCarritoAsync(int cantidad, int idUsuario, int idProducto)
        {
            int idCarrito = await this.GetNextIdAsync("CARRITO_ITEMS");
            var itemExistente = await this.context.CarritoItems.FirstOrDefaultAsync(z => z.IdUsuario == idUsuario && z.IdProducto == idProducto);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                CarritoItem nuevoItem = new CarritoItem
                {
                    IdCarrito = idCarrito,
                    IdUsuario = idUsuario,
                    IdProducto = idProducto,
                    Cantidad = cantidad
                };
                this.context.CarritoItems.Add(nuevoItem);
            }
            await this.context.SaveChangesAsync();
        }
        public async Task<CarritoItem> GetProductoCarritoAsync(int idUsuario, int idProducto)
        {
            return await this.context.CarritoItems.FirstOrDefaultAsync(c => c.IdUsuario == idUsuario && c.IdProducto == idProducto);
        }
        public async Task<List<CarritoItem>> GetCarritoUsuarioAsync(int idUsuario)
        {
            return await this.context.CarritoItems
                .Where(c => c.IdUsuario == idUsuario)
                .Select(c => new CarritoItem
                {
                    IdCarrito = c.IdCarrito,
                    IdUsuario = c.IdUsuario,
                    IdProducto = c.IdProducto,
                    Cantidad = c.Cantidad,
                    Producto = new Producto
                    {
                        IdProducto = c.Producto.IdProducto,
                        Nombre = c.Producto.Nombre,
                        Descripcion = c.Producto.Descripcion,
                        Imagen = c.Producto.Imagen,
                        PrecioUnidad = c.Producto.PrecioUnidad,
                        Stock = c.Producto.Stock,
                        EstaActivo = c.Producto.EstaActivo,
                        IdUnidadMedida = c.Producto.IdUnidadMedida,
                        IdFinca = c.Producto.IdFinca,
                        IdSubcategoria = c.Producto.IdSubcategoria,
                        Subcategoria = new Subcategoria
                        {
                            IdSubcategoria = c.Producto.Subcategoria.IdSubcategoria,
                            Nombre = c.Producto.Subcategoria.Nombre,
                            Descripcion = c.Producto.Subcategoria.Descripcion,
                            Imagen = c.Producto.Subcategoria.Imagen,
                            EstaActiva = c.Producto.Subcategoria.EstaActiva
                        },
                        UnidadMedida = new UnidadMedida
                        {
                            IdUnidadMedida = c.Producto.UnidadMedida.IdUnidadMedida,
                            Nombre = c.Producto.UnidadMedida.Nombre,
                            EstaActiva = c.Producto.UnidadMedida.EstaActiva
                        }
                    }
                })
                .ToListAsync();
        }

        public async Task ActualizarCantidadCarritoAsync(int idUsuario, int idProducto, int nuevaCantidad)
        {
            var item = await this.GetProductoCarritoAsync(idUsuario, idProducto);
            if (item != null)
            {
                item.Cantidad = nuevaCantidad;
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<List<CarritoItem>> GetProductoByIdsAsync(int idUsuario)
        {
            return await this.context.CarritoItems.Where(c => c.IdUsuario == idUsuario).Include(c => c.Producto).ToListAsync();
        }

        public async Task EliminarProductoCarritoAsync(int idUsuario, int idProducto)
        {
            CarritoItem item = await this.GetProductoCarritoAsync(idUsuario, idProducto);
            this.context.CarritoItems.Remove(item);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarCarritoUsuarioAsync(int idUsuario)
        {
            List<CarritoItem> carritoItems = await this.context.CarritoItems.Where(c => c.IdUsuario == idUsuario).ToListAsync();
            foreach(CarritoItem item in carritoItems)
            {
                this.context.CarritoItems.Remove(item);
            }
            await this.context.SaveChangesAsync();
        }

        public async Task<decimal> GetSubtotalCarrito(int idUsuario)
        {
            return await this.context.CarritoItems.Where(c => c.IdUsuario == idUsuario).SumAsync(c => c.Cantidad * c.Producto.PrecioUnidad);
        }
        #endregion
        #region PAGOS
        public async Task InsertarPagoUsuarioAsync(int idPedido, string pasarela, string metodo, string ultimosDigitos, string estado, string transactionId)
        {
            int idPago = await this.GetNextIdAsync("PAGOS");
            Pago pago = new Pago
            {
                IdPago = idPago,
                IdPedido = idPedido,
                Pasarela = pasarela,
                MetodoPago = metodo,
                UltimosDigitosTarjeta = ultimosDigitos,
                EstadoPago = estado,
                TransactionId = transactionId,
                FechaPago = DateTime.Now
            };
            this.context.Pagos.Add(pago);
            await this.context.SaveChangesAsync();
        }
        #endregion
        #region MENSAJES
        public async Task InsertarMensajeAsync(int? idUsuario, string nombre, string email, string tipoConsulta, string asunto, string mensaje)
        {
            int idMensaje = await this.GetNextIdAsync("MENSAJES");
            Mensaje nuevoMensaje = new Mensaje
            {
                IdMensaje = idMensaje,
                IdUsuario = idUsuario,
                Nombre = nombre,
                Email = email,
                TipoConsulta = tipoConsulta,
                Asunto = asunto,
                Contenido = mensaje,
                FechaEnvio = DateTime.Now,
                EsLeido = false,
                EsRespondido = false
            };
            this.context.Sugerencias.Add(nuevoMensaje);
            await this.context.SaveChangesAsync();
        }

        public async Task<List<Mensaje>> GetMensajesAsync()
        {
            return await this.context.Sugerencias.OrderByDescending(s => s.FechaEnvio).ToListAsync();
        }

        public async Task<Mensaje> GetMensajeByIdAsync(int idMensaje)
        {
            return await this.context.Sugerencias.FirstOrDefaultAsync(s => s.IdMensaje == idMensaje);
        }

        public async Task MarcarMensajeLeidoAsync(int idMensaje)
        {
            Mensaje mensaje = await this.GetMensajeByIdAsync(idMensaje);
            mensaje.EsLeido = true;
            await this.context.SaveChangesAsync();
        }

        public async Task MarcarComoRespondidoAsync(int idMensaje)
        {
            Mensaje mensaje = await this.GetMensajeByIdAsync(idMensaje);
            mensaje.EsRespondido = true;
            await this.context.SaveChangesAsync();
        }
        #endregion

    }
}
