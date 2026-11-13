using RabiGamaliel.Application.Noticias;

namespace RabiGamaliel.Application.Tests;

public sealed class GuardarNoticiaValidatorTests
{
    [Theory]
    [InlineData("https://res.cloudinary.com/demo/image/upload/foto.png")]
    [InlineData("http://colegio.edu.co/img/foto.jpg")]
    [InlineData("/img/noticias/semana-gastronomica.png")]
    [InlineData("  /img/noticias/foto.png  ")]
    public void Acepta_urls_http_y_rutas_del_sitio(string url) =>
        Assert.True(GuardarNoticiaValidator.EsUrlDeImagenValida(url));

    [Theory]
    [InlineData("//otro-sitio.com/foto.png")]     // protocolo relativo: apunta a otro dominio
    [InlineData("/img/../../secreto.png")]        // intenta salir de la carpeta pública
    [InlineData("/img/mi foto.png")]              // espacios sin codificar
    [InlineData("ftp://servidor/foto.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("img/foto.png")]                  // relativa sin "/" inicial
    public void Rechaza_urls_no_permitidas(string url) =>
        Assert.False(GuardarNoticiaValidator.EsUrlDeImagenValida(url));
}
