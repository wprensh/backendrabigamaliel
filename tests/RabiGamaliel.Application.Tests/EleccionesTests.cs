using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Tests;

public sealed class EleccionesTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly FakeMiembros _miembros = new();
    private readonly FakeElecciones _elecciones = new();
    private readonly FakeReloj _reloj = new();
    private readonly EleccionService _servicio;
    private readonly VotacionService _votacion;
    private readonly MiembroCampusService _comunidad;
    private readonly CampusAutenticacionService _ingreso;

    public EleccionesTests()
    {
        var unitOfWork = new FakeUnitOfWork();
        _servicio = new EleccionService(
            _elecciones, _miembros, unitOfWork, _reloj,
            new GuardarEleccionValidator(), new CrearCandidatoValidator(), new ActualizarCandidatoValidator());
        _votacion = new VotacionService(_elecciones, _miembros, _reloj);
        var hasher = new FakeHasher();
        _comunidad = new MiembroCampusService(
            _miembros, unitOfWork, hasher, _reloj,
            new CrearMiembroValidator(), new ActualizarMiembroValidator(), new ImportarMiembrosValidator());
        _ingreso = new CampusAutenticacionService(_miembros, unitOfWork, hasher, new FakeEmisor(), _reloj);
    }

    private MiembroCampus Estudiante(string nombre, int grado, string? grupo = "A")
    {
        var m = MiembroCampus.Crear(RolCampus.Estudiante, Guid.NewGuid().ToString("N")[..10], nombre, grado, grupo);
        _miembros.Lista.Add(m);
        return m;
    }

    private static VotanteActual Votante(MiembroCampus m) => new(m.Id, m.Rol, m.Grado, m.Grupo);

    /// <summary>Elección abierta desde hace una hora, con todos los cargos.</summary>
    private async Task<EleccionResponse> NuevaEleccion(params CargoEleccion[] cargos)
    {
        var ahora = _reloj.GetUtcNow();
        var creada = await _servicio.CrearAsync(
            new GuardarEleccionRequest(
                "Gobierno escolar 2027", null, ahora.AddHours(-1), ahora.AddHours(6),
                cargos.Length > 0 ? cargos : Enum.GetValues<CargoEleccion>()),
            Ct);
        return creada.Value!;
    }

    private async Task<CandidatoResponse> Inscribir(Guid eleccionId, CargoEleccion cargo, MiembroCampus estudiante) =>
        (await _servicio.CrearCandidatoAsync(eleccionId, new CrearCandidatoRequest(cargo, estudiante.Id, null, null, null, null), Ct)).Value!;

    [Fact]
    public async Task Solo_inscribe_candidatos_que_cumplen_el_grado_del_cargo()
    {
        var eleccion = await NuevaEleccion();
        var de10 = Estudiante("Ana", 10);
        var de2 = Estudiante("Beto", 2);
        var de3 = Estudiante("Caro", 3);

        var personero = await _servicio.CrearCandidatoAsync(eleccion.Id, new(CargoEleccion.Personero, de10.Id, null, null, null, null), Ct);
        var contralor = await _servicio.CrearCandidatoAsync(eleccion.Id, new(CargoEleccion.Contralor, de10.Id, null, null, null, null), Ct);
        var voceroDe2 = await _servicio.CrearCandidatoAsync(eleccion.Id, new(CargoEleccion.VoceroGrado, de2.Id, null, null, null, null), Ct);
        var voceroDe3 = await _servicio.CrearCandidatoAsync(eleccion.Id, new(CargoEleccion.VoceroGrado, de3.Id, null, null, null, null), Ct);

        Assert.Equal(ErrorType.Validacion, personero.Error?.Tipo);
        Assert.True(contralor.IsSuccess);
        Assert.Equal(ErrorType.Validacion, voceroDe2.Error?.Tipo);
        Assert.Equal(ReglasCargo.AmbitoPrimerosGrados, voceroDe3.Value!.Ambito);
        Assert.Equal(1, voceroDe3.Value.Numero);
    }

    [Fact]
    public async Task El_tarjeton_muestra_solo_las_contiendas_del_estudiante()
    {
        var eleccion = await NuevaEleccion();
        await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Pedro", 11));
        await Inscribir(eleccion.Id, CargoEleccion.VoceroGrado, Estudiante("Vocera 7", 7, "B"));
        await Inscribir(eleccion.Id, CargoEleccion.VoceroGrado, Estudiante("Vocero 8", 8));
        await Inscribir(eleccion.Id, CargoEleccion.RepresentanteCurso, Estudiante("Repre 7A", 7, "A"));
        await Inscribir(eleccion.Id, CargoEleccion.RepresentanteCurso, Estudiante("Repre 7B", 7, "B"));
        var votante = Estudiante("Votante 7A", 7, "A");

        var tarjeton = (await _votacion.TarjetonAsync(eleccion.Id, Votante(votante), Ct)).Value!;

        Assert.True(tarjeton.PuedeVotar);
        Assert.Equal(
            [(CargoEleccion.Personero, ""), (CargoEleccion.VoceroGrado, "7"), (CargoEleccion.RepresentanteCurso, "7-A")],
            tarjeton.Contiendas.Select(c => (c.Cargo, c.Ambito)));
    }

    [Fact]
    public async Task Cada_estudiante_vota_una_vez_y_el_voto_no_guarda_quien_lo_emitio()
    {
        var eleccion = await NuevaEleccion(CargoEleccion.Personero, CargoEleccion.VoceroGrado);
        var pedro = await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Pedro", 11));
        var vocero = await Inscribir(eleccion.Id, CargoEleccion.VoceroGrado, Estudiante("Caro", 3));
        var votante = Votante(Estudiante("Niño de transición", 0));

        var incompleto = await _votacion.VotarAsync(eleccion.Id, votante, new VotarRequest([new(CargoEleccion.Personero, pedro.Id)]), Ct);
        var ajeno = await _votacion.VotarAsync(
            eleccion.Id, votante, new VotarRequest([new(CargoEleccion.Personero, vocero.Id), new(CargoEleccion.VoceroGrado, null)]), Ct);
        var valido = await _votacion.VotarAsync(
            eleccion.Id, votante, new VotarRequest([new(CargoEleccion.Personero, pedro.Id), new(CargoEleccion.VoceroGrado, null)]), Ct);
        var repetido = await _votacion.VotarAsync(
            eleccion.Id, votante, new VotarRequest([new(CargoEleccion.Personero, pedro.Id), new(CargoEleccion.VoceroGrado, vocero.Id)]), Ct);

        Assert.Equal(ErrorType.Validacion, incompleto.Error?.Tipo);
        Assert.Equal(ErrorType.Validacion, ajeno.Error?.Tipo);
        Assert.True(valido.IsSuccess);
        Assert.Equal(ErrorType.Conflicto, repetido.Error?.Tipo);
        Assert.Single(_elecciones.Participaciones);
        Assert.Equal(2, _elecciones.Votos.Count);
        Assert.Contains(_elecciones.Votos, v => v.Cargo == CargoEleccion.VoceroGrado && v.CandidatoId is null && v.Ambito == "0-3");
    }

    [Fact]
    public async Task No_se_vota_fuera_del_horario_ni_como_docente()
    {
        var eleccion = await NuevaEleccion(CargoEleccion.Personero);
        var pedro = await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Pedro", 11));
        var votos = new VotarRequest([new(CargoEleccion.Personero, pedro.Id)]);
        var docente = MiembroCampus.Crear(RolCampus.Docente, "900100", "Profe", null, null);

        var comoDocente = await _votacion.VotarAsync(eleccion.Id, Votante(docente), votos, Ct);
        _reloj.Avanzar(TimeSpan.FromHours(7));
        var tarde = await _votacion.VotarAsync(eleccion.Id, Votante(Estudiante("Luis", 9)), votos, Ct);

        Assert.Equal(ErrorType.Conflicto, comoDocente.Error?.Tipo);
        Assert.Equal(ErrorType.Conflicto, tarde.Error?.Tipo);
    }

    [Fact]
    public async Task Los_resultados_marcan_ganador_empate_y_voto_en_blanco()
    {
        var eleccion = await NuevaEleccion(CargoEleccion.Personero, CargoEleccion.Contralor);
        var ana = await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Ana", 11));
        var beto = await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Beto", 11));
        var caro = await Inscribir(eleccion.Id, CargoEleccion.Contralor, Estudiante("Caro", 10));

        async Task Votar(Guid? personero, Guid? contralor) => Assert.True((await _votacion.VotarAsync(
            eleccion.Id, Votante(Estudiante("Votante", 6)),
            new VotarRequest([new(CargoEleccion.Personero, personero), new(CargoEleccion.Contralor, contralor)]), Ct)).IsSuccess);

        await Votar(ana.Id, null);
        await Votar(ana.Id, null);
        await Votar(beto.Id, caro.Id);

        var enCurso = await _votacion.ResultadosAsync(eleccion.Id, Votante(Estudiante("Curioso", 6)), Ct);
        var panel = (await _servicio.ResultadosAsync(eleccion.Id, Ct)).Value!;

        Assert.Equal(ErrorType.Conflicto, enCurso.Error?.Tipo);
        var personero = panel.Contiendas.Single(c => c.Cargo == CargoEleccion.Personero);
        Assert.Equal(ana.Id, personero.GanadorId);
        Assert.Equal([2, 1], personero.Candidatos.Select(c => c.Votos));
        var contralor = panel.Contiendas.Single(c => c.Cargo == CargoEleccion.Contralor);
        Assert.True(contralor.GanaVotoEnBlanco);
        Assert.Null(contralor.GanadorId);
        Assert.Equal(3, panel.Participacion.Votantes);
        Assert.Equal(3, panel.Participacion.PorGrado.Single(g => g.Grado == 6).Votantes);
    }

    [Fact]
    public async Task Con_votos_no_se_agregan_candidatos_ni_se_elimina_la_eleccion()
    {
        var eleccion = await NuevaEleccion(CargoEleccion.Personero);
        var ana = await Inscribir(eleccion.Id, CargoEleccion.Personero, Estudiante("Ana", 11));
        await _votacion.VotarAsync(eleccion.Id, Votante(Estudiante("Luis", 9)), new VotarRequest([new(CargoEleccion.Personero, ana.Id)]), Ct);

        var nuevo = await _servicio.CrearCandidatoAsync(eleccion.Id, new(CargoEleccion.Personero, Estudiante("Tarde", 11).Id, null, null, null, null), Ct);
        var eliminar = await _servicio.EliminarAsync(eleccion.Id, Ct);
        var perfil = await _servicio.ActualizarCandidatoAsync(eleccion.Id, ana.Id, new(ana.Numero, "Lema nuevo", null, null), Ct);

        Assert.Equal(ErrorType.Conflicto, nuevo.Error?.Tipo);
        Assert.Equal(ErrorType.Conflicto, eliminar.Error?.Tipo);
        Assert.Equal("Lema nuevo", perfil.Value?.Lema);
    }

    [Fact]
    public async Task Importar_crea_actualiza_y_reporta_filas_con_error()
    {
        await _comunidad.CrearAsync(new CrearMiembroRequest(RolCampus.Estudiante, "1.043.555", "Nombre viejo", 6, "A"), Ct);

        var resultado = (await _comunidad.ImportarAsync(new ImportarMiembrosRequest(RolCampus.Estudiante,
        [
            new("1043555", "Nombre nuevo", 7, "b"),
            new("2002002", "Estudiante nueva", 11, "A"),
            new("2002002", "Repetida", 11, "A"),
            new("3003003", "Sin grado", null, "A"),
        ]), Ct)).Value!;

        Assert.Equal(1, resultado.Creados);
        Assert.Equal(1, resultado.Actualizados);
        Assert.Equal([3, 4], resultado.Errores.Select(e => e.Fila));
        var actualizado = _miembros.Lista.Single(m => m.Documento == "1043555");
        Assert.Equal((7, "B", "Nombre nuevo"), (actualizado.Grado!.Value, actualizado.Grupo, actualizado.NombreCompleto));
    }

    [Fact]
    public async Task Ingresa_al_campus_con_la_clave_generada_y_se_bloquea_tras_cinco_errores()
    {
        var estudiante = Estudiante("Ana", 11);
        var clave = (await _comunidad.GenerarClavesAsync(new GenerarClavesRequest(Ids: [estudiante.Id]), Ct)).Value!.Single().Clave;

        var conMinusculas = await _ingreso.IniciarSesionAsync(new(estudiante.Documento, clave.ToLowerInvariant().Replace("-", " ")), Ct);
        for (var i = 0; i < MiembroCampus.IntentosAntesDeBloquear; i++)
        {
            await _ingreso.IniciarSesionAsync(new(estudiante.Documento, "AAAA-AAAA"), Ct);
        }
        var bloqueado = await _ingreso.IniciarSesionAsync(new(estudiante.Documento, clave), Ct);

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", clave);
        Assert.Equal(estudiante.Id, conMinusculas.Value?.Perfil.Id);
        Assert.Equal("Campus.Bloqueado", bloqueado.Error?.Codigo);
    }

    [Fact]
    public void Los_modulos_nuevos_aparecen_en_la_matriz_de_permisos()
    {
        Assert.Contains(new Permiso(ModuloSistema.Elecciones, AccionPermiso.Crear), Permiso.Todos);
        Assert.True(Permiso.TryParse("Campus:Editar", out _));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeHasher : IHasherCodigosAcceso
    {
        public string Hashear(string clave) => "hash:" + clave;

        public VerificacionClave Verificar(string hash, string clave) =>
            hash == "hash:" + clave ? VerificacionClave.Correcta : VerificacionClave.Incorrecta;
    }

    private sealed class FakeEmisor : IEmisorTokens
    {
        public TokenEmitido Emitir(Usuario usuario) => throw new NotSupportedException();

        public TokenEmitido EmitirCampus(MiembroCampus miembro) => new($"campus-{miembro.Id}", DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class FakeReloj : TimeProvider
    {
        private DateTimeOffset _ahora = new(2027, 2, 20, 8, 0, 0, TimeSpan.Zero);

        public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;

        public override DateTimeOffset GetUtcNow() => _ahora;
    }

    private sealed class FakeMiembros : MiembrosEnMemoria;

    private sealed class FakeElecciones : IEleccionRepository
    {
        private readonly List<Eleccion> _elecciones = [];
        private readonly List<Candidato> _candidatos = [];

        public List<ParticipacionVoto> Participaciones { get; } = [];

        public List<Voto> Votos { get; } = [];

        public Task<Eleccion?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_elecciones.FirstOrDefault(e => e.Id == id));

        public Task<List<Eleccion>> ListarAsync(CancellationToken ct) => Task.FromResult(_elecciones.ToList());

        public Task<List<ResumenEleccion>> ResumirAsync(IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct) =>
            Task.FromResult(eleccionIds.Select(id => new ResumenEleccion(
                id, _candidatos.Count(c => c.EleccionId == id), Participaciones.Count(p => p.EleccionId == id))).ToList());

        public void Agregar(Eleccion eleccion) => _elecciones.Add(eleccion);

        public Task EliminarAsync(Eleccion eleccion, CancellationToken ct)
        {
            _elecciones.Remove(eleccion);
            return Task.CompletedTask;
        }

        public Task<List<Candidato>> CandidatosAsync(Guid eleccionId, CancellationToken ct) =>
            Task.FromResult(_candidatos.Where(c => c.EleccionId == eleccionId).ToList());

        public Task<Candidato?> ObtenerCandidatoAsync(Guid eleccionId, Guid candidatoId, CancellationToken ct) =>
            Task.FromResult(_candidatos.FirstOrDefault(c => c.EleccionId == eleccionId && c.Id == candidatoId));

        public void AgregarCandidato(Candidato candidato) => _candidatos.Add(candidato);

        public void EliminarCandidato(Candidato candidato) => _candidatos.Remove(candidato);

        public Task<bool> TieneVotosAsync(Guid eleccionId, CancellationToken ct) => Task.FromResult(Participaciones.Any(p => p.EleccionId == eleccionId));

        public Task<bool> YaVotoAsync(Guid eleccionId, Guid estudianteId, CancellationToken ct) =>
            Task.FromResult(Participaciones.Any(p => p.EleccionId == eleccionId && p.EstudianteId == estudianteId));

        public Task<HashSet<Guid>> EleccionesVotadasAsync(Guid estudianteId, IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct) =>
            Task.FromResult(Participaciones.Where(p => p.EstudianteId == estudianteId).Select(p => p.EleccionId).ToHashSet());

        public Task<bool> RegistrarVotoAsync(ParticipacionVoto participacion, IReadOnlyList<Voto> votos, CancellationToken ct)
        {
            Participaciones.Add(participacion);
            Votos.AddRange(votos);
            return Task.FromResult(true);
        }

        public Task<List<ConteoVotos>> ContarVotosAsync(Guid eleccionId, CancellationToken ct) =>
            Task.FromResult(Votos.Where(v => v.EleccionId == eleccionId).GroupBy(v => (v.Cargo, v.Ambito, v.CandidatoId))
                .Select(g => new ConteoVotos(g.Key.Cargo, g.Key.Ambito, g.Key.CandidatoId, g.Count())).ToList());

        public Task<Dictionary<int, int>> VotantesPorGradoAsync(Guid eleccionId, CancellationToken ct) =>
            Task.FromResult(Participaciones.Where(p => p.EleccionId == eleccionId).GroupBy(p => p.Grado).ToDictionary(g => g.Key, g => g.Count()));
    }
}
