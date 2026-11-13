using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Usuarios;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Tests;

public sealed class UsuarioServiceTests
{
    private readonly FakeUsuarioRepository _repository = new();
    private readonly FakeReloj _reloj = new();
    private readonly UsuarioService _usuarios;
    private readonly AutenticacionService _autenticacion;

    public UsuarioServiceTests()
    {
        var unitOfWork = new FakeUnitOfWork();
        var hasher = new FakeHasher();
        _usuarios = new UsuarioService(
            _repository, unitOfWork, hasher, _reloj,
            new CrearUsuarioValidator(), new ActualizarUsuarioValidator(), new RestablecerClaveValidator());
        _autenticacion = new AutenticacionService(
            _repository, unitOfWork, hasher, new FakeEmisor(), _reloj,
            new ConfiguracionInicialValidator(), new CambiarMiClaveValidator());
    }

    private static CrearUsuarioRequest NuevoUsuario(string usuario = "secretaria", params string[] permisos) =>
        new(usuario, "Secretaría Académica", null, "Clave1234", false, permisos);

    private Task<Result<SesionResponse>> Ingresar(string usuario, string clave) =>
        _autenticacion.IniciarSesionAsync(new IniciarSesionRequest(usuario, clave), CancellationToken.None);

    [Fact]
    public async Task Configuracion_inicial_crea_un_superadministrador_una_sola_vez()
    {
        var solicitud = new ConfiguracionInicialRequest("Rectoria", "Rectora de Prueba", null, "Clave1234");

        Assert.True((await _autenticacion.EstadoAsync(CancellationToken.None)).RequiereConfiguracion);
        var primera = await _autenticacion.ConfigurarAsync(solicitud, CancellationToken.None);
        var segunda = await _autenticacion.ConfigurarAsync(solicitud with { NombreUsuario = "otro" }, CancellationToken.None);

        Assert.True(primera.Value?.Usuario.EsSuperAdministrador);
        Assert.Equal("rectoria", primera.Value!.Usuario.NombreUsuario);
        Assert.Equal(Permiso.Todos.Count, primera.Value.Usuario.Permisos.Count);
        Assert.Equal(ErrorType.Conflicto, segunda.Error?.Tipo);
    }

    [Fact]
    public async Task Quien_no_es_superadministrador_no_ve_ni_modifica_a_los_superadministradores()
    {
        var super = (await _usuarios.CrearAsync(
            new CrearUsuarioRequest("dueno", "Superadministrador", null, "Clave1234", true, []), CancellationToken.None)).Value!;
        var ct = CancellationToken.None;

        Assert.Equal(ErrorType.NoEncontrado, (await _usuarios.ObtenerAsync(super.Id, ct, actorEsSuper: false)).Error?.Tipo);
        Assert.Equal(ErrorType.NoEncontrado, (await _usuarios.EliminarAsync(super.Id, Guid.NewGuid(), ct, actorEsSuper: false)).Error?.Tipo);
        Assert.Equal(ErrorType.NoEncontrado,
            (await _usuarios.RestablecerClaveAsync(super.Id, new RestablecerClaveRequest("OtraClave123"), ct, actorEsSuper: false)).Error?.Tipo);
        Assert.Equal(ErrorType.NoAutorizado,
            (await _usuarios.CrearAsync(new CrearUsuarioRequest("otro", "Otro", null, "Clave1234", true, []), ct, actorEsSuper: false)).Error?.Tipo);
        Assert.True((await _usuarios.ObtenerAsync(super.Id, ct)).IsSuccess);
    }

    [Fact]
    public async Task Crear_con_permiso_de_editar_agrega_el_de_ver()
    {
        var creado = await _usuarios.CrearAsync(NuevoUsuario(permisos: "Noticias:Editar"), CancellationToken.None);

        Assert.Equal(["Noticias:Ver", "Noticias:Editar"], creado.Value!.Permisos);
        var usuario = _repository.Usuarios.Single();
        Assert.True(usuario.Puede(ModuloSistema.Noticias, AccionPermiso.Ver));
        Assert.False(usuario.Puede(ModuloSistema.Noticias, AccionPermiso.Eliminar));
        Assert.False(usuario.Puede(ModuloSistema.Certificados, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Crear_rechaza_nombre_repetido_sin_distinguir_mayusculas_y_permisos_inexistentes()
    {
        await _usuarios.CrearAsync(NuevoUsuario("Secretaria"), CancellationToken.None);

        var repetido = await _usuarios.CrearAsync(NuevoUsuario("SECRETARIA"), CancellationToken.None);
        var inventado = await _usuarios.CrearAsync(NuevoUsuario("otro", "Notas:Ver"), CancellationToken.None);

        Assert.Equal(ErrorType.Conflicto, repetido.Error?.Tipo);
        Assert.Equal(ErrorType.Validacion, inventado.Error?.Tipo);
    }

    [Fact]
    public async Task Iniciar_sesion_bloquea_la_cuenta_tras_cinco_claves_erradas()
    {
        await _usuarios.CrearAsync(NuevoUsuario(), CancellationToken.None);

        for (var i = 0; i < Usuario.IntentosAntesDeBloquear; i++)
        {
            Assert.Equal(ErrorType.NoAutorizado, (await Ingresar("secretaria", "errada")).Error?.Tipo);
        }

        var conClaveCorrecta = await Ingresar("secretaria", "Clave1234");
        Assert.Equal("Autenticacion.Bloqueado", conClaveCorrecta.Error?.Codigo);

        _reloj.Avanzar(Usuario.DuracionBloqueo);
        Assert.True((await Ingresar(" SECRETARIA ", "Clave1234")).IsSuccess);
    }

    [Fact]
    public async Task Usuario_inexistente_y_clave_errada_dan_el_mismo_mensaje()
    {
        await _usuarios.CrearAsync(NuevoUsuario(), CancellationToken.None);

        var inexistente = await Ingresar("nadie", "Clave1234");
        var errada = await Ingresar("secretaria", "otra");

        Assert.Equal(inexistente.Error?.Mensaje, errada.Error?.Mensaje);
    }

    [Fact]
    public async Task Usuario_desactivado_no_puede_iniciar_sesion_y_pierde_su_sello()
    {
        var creado = (await _usuarios.CrearAsync(NuevoUsuario(permisos: "Noticias:Ver"), CancellationToken.None)).Value!;
        var selloAnterior = _repository.Usuarios.Single().SelloSeguridad;

        await _usuarios.ActualizarAsync(
            creado.Id, new ActualizarUsuarioRequest("Secretaría", null, false, Activo: false, ["Noticias:Ver"]),
            Guid.Empty, CancellationToken.None);

        Assert.Equal("Autenticacion.Inactivo", (await Ingresar("secretaria", "Clave1234")).Error?.Codigo);
        Assert.NotEqual(selloAnterior, _repository.Usuarios.Single().SelloSeguridad);
    }

    [Fact]
    public async Task No_se_puede_quitar_el_ultimo_superadministrador_ni_eliminarse_a_si_mismo()
    {
        var admin = (await _autenticacion.ConfigurarAsync(
            new ConfiguracionInicialRequest("rectoria", "Rectoría", null, "Clave1234"), CancellationToken.None)).Value!.Usuario;

        var degradar = await _usuarios.ActualizarAsync(
            admin.Id, new ActualizarUsuarioRequest("Rectoría", null, false, true, []), Guid.Empty, CancellationToken.None);
        var eliminarse = await _usuarios.EliminarAsync(admin.Id, admin.Id, CancellationToken.None);
        var eliminarUltimo = await _usuarios.EliminarAsync(admin.Id, Guid.Empty, CancellationToken.None);

        Assert.Equal("Usuario.UltimoSuperAdministrador", degradar.Error?.Codigo);
        Assert.Equal("Usuario.EliminarseASiMismo", eliminarse.Error?.Codigo);
        Assert.Equal("Usuario.UltimoSuperAdministrador", eliminarUltimo.Error?.Codigo);
        Assert.Single(_repository.Usuarios);
    }

    [Fact]
    public async Task Cambiar_mi_clave_exige_la_actual_y_cambia_el_sello()
    {
        var sesion = (await _autenticacion.ConfigurarAsync(
            new ConfiguracionInicialRequest("rectoria", "Rectoría", null, "Clave1234"), CancellationToken.None)).Value!;
        var sello = _repository.Usuarios.Single().SelloSeguridad;

        var errada = await _autenticacion.CambiarMiClaveAsync(
            sesion.Usuario.Id, new CambiarMiClaveRequest("otra", "Nueva12345"), CancellationToken.None);
        var correcta = await _autenticacion.CambiarMiClaveAsync(
            sesion.Usuario.Id, new CambiarMiClaveRequest("Clave1234", "Nueva12345"), CancellationToken.None);

        Assert.Equal(ErrorType.Validacion, errada.Error?.Tipo);
        Assert.True(correcta.IsSuccess);
        Assert.NotEqual(sello, _repository.Usuarios.Single().SelloSeguridad);
        Assert.True((await Ingresar("rectoria", "Nueva12345")).IsSuccess);
    }

    [Theory]
    [InlineData("corta1")]
    [InlineData("solamenteletras")]
    [InlineData("1234567890")]
    public async Task Rechaza_claves_debiles(string clave)
    {
        var resultado = await _usuarios.CrearAsync(NuevoUsuario() with { Clave = clave }, CancellationToken.None);

        Assert.Equal(ErrorType.Validacion, resultado.Error?.Tipo);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeHasher : IHasherClaves
    {
        public string Hashear(string clave) => "hash:" + clave;

        public VerificacionClave Verificar(string hash, string clave) =>
            hash == "hash:" + clave ? VerificacionClave.Correcta : VerificacionClave.Incorrecta;
    }

    private sealed class FakeEmisor : IEmisorTokens
    {
        public TokenEmitido Emitir(Usuario usuario) => new($"token-{usuario.Id}", DateTimeOffset.UtcNow.AddHours(8));

        public TokenEmitido EmitirCampus(MiembroCampus miembro) => new($"campus-{miembro.Id}", DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class FakeReloj : TimeProvider
    {
        private DateTimeOffset _ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;

        public override DateTimeOffset GetUtcNow() => _ahora;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios { get; } = [];

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

        public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct) =>
            Task.FromResult(Usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));

        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct) =>
            Task.FromResult(Usuarios.Any(u => u.NombreUsuario == nombreUsuario));

        public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Usuarios.Count > 0);

        public Task<int> ContarOtrosSuperAdministradoresActivosAsync(Guid excluirId, CancellationToken ct) =>
            Task.FromResult(Usuarios.Count(u => u.Id != excluirId && u.EsSuperAdministrador && u.Activo));

        public Task<PagedResult<Usuario>> ListarAsync(UsuarioQuery query, CancellationToken ct) =>
            Task.FromResult(new PagedResult<Usuario>([.. Usuarios], query.Pagina, query.TamanoPagina, Usuarios.Count));

        public void Agregar(Usuario usuario) => Usuarios.Add(usuario);

        public void Eliminar(Usuario usuario) => Usuarios.Remove(usuario);
    }
}
