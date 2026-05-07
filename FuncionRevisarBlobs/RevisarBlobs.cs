using ApiSazonLocal.Data;
using ApiSazonLocal.Services;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace FuncionRevisarBlobs;

public class RevisarBlobs
{
    private readonly ILogger _logger;
    private SazonContext context;
    private BlobService service;

    public RevisarBlobs(ILoggerFactory loggerFactory, BlobService service, SazonContext context)
    {
        _logger = loggerFactory.CreateLogger<RevisarBlobs>();
        this.context = context;
        this.service = service;
    }

    [Function("Function1")]
    public async Task RunAsync([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Iniciando limpieza de blobs: {executionTime}", DateTime.Now);

        List<string> fotosUsuarios = await this.context.Usuarios.Where(u => u.Imagen != null).Select(u => u.Imagen).ToListAsync();
        await LimpiarContenedorAsync("usuarios-sl", fotosUsuarios);

        List<string> fotosProductos = await this.context.Productos.Where(p => p.Imagen != null).Select(p => p.Imagen).ToListAsync();
        await LimpiarContenedorAsync("productos-sl", fotosProductos);

        List<string> fotosSubcategorias = await this.context.Subcategorias.Where(c => c.Imagen != null).Select(c => c.Imagen).ToListAsync();
        await LimpiarContenedorAsync("subcategorias-sl", fotosSubcategorias);

    }

    private async Task LimpiarContenedorAsync(string nombreContenedor, List<string> imagenesEnDB)
    {
        var hashEnUso = imagenesEnDB.Where(i => i != null).ToHashSet();

        List<string> blobsEnAzure = await this.service.GetBlobsInContainerAsync(nombreContenedor);

        foreach (var nombreBlob in blobsEnAzure)
        {
            if (!hashEnUso.Contains(nombreBlob) && !EsImagenProtegida(nombreBlob))
            {
                _logger.LogWarning("Borrando de {contenedor}: {archivo}", nombreContenedor, nombreBlob);
                await this.service.DeleteBlobAsync(nombreContenedor, nombreBlob);
            }
        }
    }

    private bool EsImagenProtegida(string fileName)
    {
        var protegidas = new List<string> {
            "usuario-generico.png",
            "Logo_Sazon_Local.png"
        };
        return protegidas.Contains(fileName.ToLower());
    }
}