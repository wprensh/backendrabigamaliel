using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Tests;

public sealed class CertificadoServiceTests
{
    private readonly FakeCertificadoRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CertificadoService _service;

    public CertificadoServiceTests() =>
        _service = new CertificadoService(
            _repository, _unitOfWork, new ExpedirCertificadoValidator(), new AnularCertificadoValidator());

    private static ExpedirCertificadoRequest RequestValido(string nombre = "Ana María Pérez Gómez") =>
        new(
            TipoCertificado.Estudios, "CERTIFICADO DE ESTUDIOS", nombre, TipoDocumento.TarjetaIdentidad,
            "1043567890", "Noveno (9°)", 2026, "Cursó y aprobó el grado noveno.", new DateOnly(2026, 10, 19),
            "Rector de prueba", "Rector");

    [Fact]
    public async Task Expedir_asigna_codigo_de_verificacion_con_el_anio_de_expedicion()
    {
        var resultado = await _service.ExpedirAsync(RequestValido(), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Matches("^CERT-2026-[0-9A-F]{6}$", resultado.Value.Codigo);
        Assert.False(resultado.Value.Anulado);
        Assert.Equal(1, _unitOfWork.Guardados);
    }

    [Fact]
    public async Task Expedir_sin_beneficiario_devuelve_error_de_validacion()
    {
        var resultado = await _service.ExpedirAsync(RequestValido(nombre: ""), CancellationToken.None);

        Assert.Equal(ErrorType.Validacion, resultado.Error?.Tipo);
        Assert.Contains(nameof(ExpedirCertificadoRequest.NombreBeneficiario), resultado.Error!.ErroresValidacion.Keys);
        Assert.Empty(_repository.Certificados);
    }

    [Fact]
    public async Task Anular_dos_veces_devuelve_conflicto()
    {
        var expedido = await _service.ExpedirAsync(RequestValido(), CancellationToken.None);
        var motivo = new AnularCertificadoRequest("Error en el nombre");

        var primera = await _service.AnularAsync(expedido.Value!.Id, motivo, CancellationToken.None);
        var segunda = await _service.AnularAsync(expedido.Value.Id, motivo, CancellationToken.None);

        Assert.True(primera.Value?.Anulado);
        Assert.Equal(ErrorType.Conflicto, segunda.Error?.Tipo);
    }

    [Fact]
    public async Task Verificar_acepta_el_codigo_en_minusculas_y_oculta_el_documento()
    {
        var expedido = await _service.ExpedirAsync(RequestValido(), CancellationToken.None);

        var resultado = await _service.VerificarAsync($"  {expedido.Value!.Codigo.ToLowerInvariant()} ", CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("******7890", resultado.Value.DocumentoParcial);
    }

    [Fact]
    public async Task Verificar_codigo_inexistente_devuelve_no_encontrado()
    {
        var resultado = await _service.VerificarAsync("CERT-2026-FFFFFF", CancellationToken.None);

        Assert.Equal(ErrorType.NoEncontrado, resultado.Error?.Tipo);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Guardados { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCertificadoRepository : ICertificadoRepository
    {
        public List<Certificado> Certificados { get; } = [];

        public Task<Certificado?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Certificados.FirstOrDefault(c => c.Id == id));

        public Task<Certificado?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct) =>
            Task.FromResult(Certificados.FirstOrDefault(c => c.Codigo == codigo));

        public Task<PagedResult<Certificado>> ListarAsync(CertificadoQuery query, CancellationToken ct) =>
            Task.FromResult(new PagedResult<Certificado>([.. Certificados], query.Pagina, query.TamanoPagina, Certificados.Count));

        public void Agregar(Certificado certificado) => Certificados.Add(certificado);
    }
}
