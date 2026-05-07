using Microsoft.EntityFrameworkCore;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Data
{
    public class SazonContext: DbContext
    {

        public SazonContext(DbContextOptions<SazonContext> options): base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<KeysUsuario> KeysUsuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Finca> Fincas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<UnidadMedida> UnidadMedidas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Subcategoria> Subcategorias { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallePedidos { get; set; }
        public DbSet<CarritoItem> CarritoItems { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Mensaje> Sugerencias { get; set; }
    }
}
