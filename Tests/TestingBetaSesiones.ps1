$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $repo 'StageUp.UI\bin'
$null = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'StageUp.BE.dll'))
$null = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'StageUp.Seguridad.dll'))

$stub = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.Seguridad;
using StageUp.BLL;
namespace StageUp.BLL {
 public class BLL_SesionBaseDatos {
  public static int Calls;
  public static string Mark = "same";
  public static UsuarioInterno Account;
  public static List<string> Permissions = new List<string>();
  public static ResultadoRevalidacionSesion Result = ResultadoRevalidacionSesion.Valida;
  public string ObtenerMarcaActual(out bool reliable) { reliable = true; return Mark; }
  public ResultadoRevalidacionSesion RevalidarUsuarioInterno(int id, DateTime? created, out UsuarioInterno account, out List<string> permissions) {
   Calls++; account = Account; permissions = Permissions; return Result;
  }
  public ResultadoRevalidacionSesion RevalidarUsuarioExterno(int id, DateTime? created, out UsuarioExterno account) { account = null; throw new Exception("Unexpected external account"); }
 }
}
public static class BetaSessionFixture {
 public static void Reset() {
  var context = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
  var state = new HttpSessionStateContainer("test", new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 30, true, HttpCookieMode.UseCookies, SessionStateMode.InProc, false);
  SessionStateUtility.AddHttpSessionStateToContext(context, state); HttpContext.Current = context;
  var account = new UsuarioInterno {IdUsuarioInterno=1,IdRolInterno=1,FechaAlta=new DateTime(2026,1,1),Nombre="Test",Activo=true};
  GestorDeSesion.IniciarSesionInterna(account, new List<string> {"VER_BITACORA"});
  GestorDeSesion.EstablecerMarcaBaseDatos("same");
  StageUp.BLL.BLL_SesionBaseDatos.Account = account;
  StageUp.BLL.BLL_SesionBaseDatos.Calls = 0;
  StageUp.BLL.BLL_SesionBaseDatos.Mark = "same";
  StageUp.BLL.BLL_SesionBaseDatos.Permissions = new List<string>();
  StageUp.BLL.BLL_SesionBaseDatos.Result = ResultadoRevalidacionSesion.Valida;
 }
}
'@
$source = Get-Content (Join-Path $repo 'StageUp.UI\Infraestructura\ControlSesionBaseDatos.cs') -Raw

$source = $source -replace '(?m)^using [^;]+;\r?\n', ''
Add-Type -TypeDefinition ($stub + $source) -ReferencedAssemblies @('System.Web', (Join-Path $bin 'StageUp.BE.dll'), (Join-Path $bin 'StageUp.Seguridad.dll'))
function Check([bool]$condition,[string]$name) { if(!$condition){throw "FAILED: $name"}; Write-Output "PASS: $name" }
[BetaSessionFixture]::Reset()
$result = [StageUp.UI.Infraestructura.ControlSesionBaseDatos]::Verificar()
Check ($result.ToString() -eq 'Valida' -and [StageUp.BLL.BLL_SesionBaseDatos]::Calls -eq 1) 'unchanged restore marker still revalidates account'
Check (![StageUp.Seguridad.GestorDeSesion]::TienePermisoInterno('VER_BITACORA')) 'removed permission does not survive in session'
Check (![StageUp.Seguridad.GestorDeSesion]::TomarAvisoRestauracion()) 'normal revalidation does not announce a restore'
[BetaSessionFixture]::Reset()
[StageUp.BLL.BLL_SesionBaseDatos]::Result = [StageUp.BE.Enumerados.ResultadoRevalidacionSesion]::Invalida
$result = [StageUp.UI.Infraestructura.ControlSesionBaseDatos]::Verificar()
Check (!$result.Equals([StageUp.BE.Enumerados.ResultadoRevalidacionSesion]::Valida) -and ![StageUp.Seguridad.GestorDeSesion]::EstaAutenticadoComoInterno()) 'inactive account closes session'
[BetaSessionFixture]::Reset()
[StageUp.BLL.BLL_SesionBaseDatos]::Result = [StageUp.BE.Enumerados.ResultadoRevalidacionSesion]::NoVerificada
$result = [StageUp.UI.Infraestructura.ControlSesionBaseDatos]::Verificar()
Check (![StageUp.Seguridad.GestorDeSesion]::EstaAutenticadoComoInterno() -and ![StageUp.Seguridad.GestorDeSesion]::TienePermisoInterno('VER_BITACORA')) 'database failure blocks previous permissions'
[BetaSessionFixture]::Reset()
[StageUp.BLL.BLL_SesionBaseDatos]::Account.FechaAlta = [DateTime]::new(2026,2,1)
$result = [StageUp.UI.Infraestructura.ControlSesionBaseDatos]::Verificar()
Check ($result.ToString() -eq 'Invalida') 'a different account with the same ID is rejected'
[BetaSessionFixture]::Reset()
[StageUp.BLL.BLL_SesionBaseDatos]::Mark = 'restored'
$result = [StageUp.UI.Infraestructura.ControlSesionBaseDatos]::Verificar()
Check ($result.ToString() -eq 'Valida' -and [StageUp.Seguridad.GestorDeSesion]::TomarAvisoRestauracion()) 'same active account survives a restore and receives notice'
Write-Output '7 session checks passed using a BLL test double; no real accounts modified.'
