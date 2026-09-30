using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Calificacion
    {
        private const int LongitudMaximaComentario = 1000;

        // Ranking de espacios (ítem 8 de la segunda entrega). Para que una
        // sola reseña de 5 estrellas no deje a un espacio primero, el orden
        // usa un promedio ponderado (promedio bayesiano):
        //
        //   puntaje = (n * promedioEspacio + M * promedioGeneral) / (n + M)
        //
        // n = reseñas del espacio, M = MinimoResenasRanking, promedioGeneral =
        // promedio de todas las reseñas de espacios. Con pocas reseñas, el
        // puntaje queda cerca del promedio general; a medida que suma
        // reseñas, pesa más su propio promedio. Además, el ranking público
        // solo muestra espacios con al menos MinimoResenasRanking reseñas.
        public const int MinimoResenasRanking = 3;
        private readonly MPP_Calificacion _mppCalificacion = new MPP_Calificacion();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public ResultadoOperacion<int> CalificarEspacio(int idReserva, int idUsuarioAutor, int puntaje, string comentario)
        {
            return Calificar(idReserva, idUsuarioAutor, TipoCalificacion.Espacio, puntaje, comentario);
        }

        public ResultadoOperacion<int> CalificarSolicitante(int idReserva, int idUsuarioAutor, int puntaje, string comentario)
        {
            return Calificar(idReserva, idUsuarioAutor, TipoCalificacion.UsuarioSolicitante, puntaje, comentario);
        }

        public List<Calificacion> ListarPorEspacio(int idEspacioArtistico)
        {
            try
            {
                return _mppCalificacion.ListarPorEspacio(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Calificacion>();
            }
        }

        public List<Calificacion> ListarRecibidasPorUsuario(int idUsuarioExterno)
        {
            try
            {
                return _mppCalificacion.ListarRecibidasPorUsuario(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Calificacion>();
            }
        }

        public List<Calificacion> ListarRealizadasPorUsuario(int idUsuarioExterno)
        {
            try
            {
                return _mppCalificacion.ListarRealizadasPorUsuario(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Calificacion>();
            }
        }

        public ResumenReputacion ObtenerResumenEspacio(int idEspacioArtistico)
        {
            try
            {
                return _mppCalificacion.ObtenerResumenEspacio(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
            }
            catch (ErrorAccesoDatosException)
            {
                return new ResumenReputacion { IdReferencia = idEspacioArtistico };
            }
        }

        public ResumenReputacion ObtenerResumenUsuario(int idUsuarioExterno)
        {
            try
            {
                return _mppCalificacion.ObtenerResumenUsuario(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new ResumenReputacion { IdReferencia = idUsuarioExterno };
            }
        }

        public void CompletarReputacionesEspacios(IEnumerable<EspacioArtistico> espacios)
        {
            if (espacios == null)
            {
                return;
            }

            Dictionary<int, ResumenReputacion> resumenes;
            try
            {
                resumenes = _mppCalificacion.ListarResumenesEspacios();
            }
            catch (ErrorAccesoDatosException)
            {
                return;
            }

            decimal promedioGeneral = CalcularPromedioGeneral(resumenes.Values);

            foreach (EspacioArtistico espacio in espacios)
            {
                if (espacio == null)
                {
                    continue;
                }

                ResumenReputacion resumen;
                if (resumenes.TryGetValue(espacio.IdEspacioArtistico, out resumen))
                {
                    espacio.PromedioCalificacion = resumen.Promedio;
                    espacio.CantidadCalificaciones = resumen.CantidadCalificaciones;
                }

                espacio.PuntajeRanking = CalcularPuntajeRanking(
                    espacio.PromedioCalificacion, espacio.CantidadCalificaciones, promedioGeneral);
            }
        }

        // Ranking público: espacios con al menos MinimoResenasRanking reseñas,
        // ordenados por puntaje ponderado. Espera la lista ya completada con
        // CompletarReputacionesEspacios.
        public static List<EspacioArtistico> ObtenerRankingMejorValorados(IEnumerable<EspacioArtistico> espacios, int cantidad)
        {
            if (espacios == null)
            {
                return new List<EspacioArtistico>();
            }

            return espacios
                .Where(e => e != null && e.CantidadCalificaciones >= MinimoResenasRanking)
                .OrderByDescending(e => e.PuntajeRanking)
                .ThenByDescending(e => e.CantidadCalificaciones)
                .Take(cantidad)
                .ToList();
        }

        public static decimal CalcularPuntajeRanking(decimal promedio, int cantidad, decimal promedioGeneral)
        {
            if (cantidad <= 0)
            {
                return 0m;
            }

            return decimal.Round(
                (cantidad * promedio + MinimoResenasRanking * promedioGeneral) / (cantidad + MinimoResenasRanking), 3);
        }

        private static decimal CalcularPromedioGeneral(IEnumerable<ResumenReputacion> resumenes)
        {
            decimal suma = 0m;
            int cantidad = 0;
            foreach (ResumenReputacion resumen in resumenes)
            {
                suma += resumen.Promedio * resumen.CantidadCalificaciones;
                cantidad += resumen.CantidadCalificaciones;
            }

            return cantidad == 0 ? 0m : suma / cantidad;
        }

        public Dictionary<int, ResumenReputacion> ObtenerResumenesUsuarios()
        {
            try
            {
                return _mppCalificacion.ListarResumenesUsuarios();
            }
            catch (ErrorAccesoDatosException)
            {
                return new Dictionary<int, ResumenReputacion>();
            }
        }

        private ResultadoOperacion<int> Calificar(
            int idReserva, int idUsuarioAutor, TipoCalificacion tipo, int puntaje, string comentario)
        {
            try
            {
                if (puntaje < 1 || puntaje > 5)
                {
                    return ResultadoOperacion<int>.Error("Elegí una calificación entre 1 y 5 estrellas.");
                }

                string comentarioLimpio = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
                if (string.IsNullOrEmpty(comentarioLimpio))
                {
                    return ResultadoOperacion<int>.Error("Contanos brevemente cómo fue la experiencia.");
                }

                if (comentarioLimpio.Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion<int>.Error(
                        "El comentario no puede superar los " + LongitudMaximaComentario + " caracteres.");
                }

                _mppReserva.FinalizarVencidas();
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
                if (reserva == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la reserva indicada.");
                }

                if (reserva.EstadoReserva != EstadoReserva.Finalizada.ToString())
                {
                    return ResultadoOperacion<int>.Error("La calificación se habilita cuando termina una reserva aceptada.");
                }

                bool autorValido = tipo == TipoCalificacion.Espacio
                    ? reserva.IdUsuarioExternoSolicitante == idUsuarioAutor
                    : reserva.IdUsuarioGestor == idUsuarioAutor;
                if (!autorValido)
                {
                    return ResultadoOperacion<int>.Error("No tenés permiso para calificar esta reserva.");
                }

                string tipoTexto = tipo.ToString();
                Calificacion calificacion = new Calificacion
                {
                    IdReserva = idReserva,
                    IdUsuarioAutor = idUsuarioAutor,
                    TipoCalificacion = tipoTexto,
                    Puntaje = puntaje,
                    Comentario = comentarioLimpio
                };

                if (_mppCalificacion.Existe(calificacion))
                {
                    return ResultadoOperacion<int>.Error("Esta calificación ya fue enviada anteriormente.");
                }

                int idCalificacion = _mppCalificacion.Insertar(calificacion);

                _bitacora.Registrar(
                    idUsuarioAutor,
                    "ALTA",
                    "Calificacion",
                    idCalificacion,
                    tipo == TipoCalificacion.Espacio
                        ? "El solicitante calificó el espacio \"" + reserva.NombreEspacio + "\" después de la reserva."
                        : "El gestor calificó al solicitante después de la reserva del espacio \"" + reserva.NombreEspacio + "\".");

                return ResultadoOperacion<int>.Ok(
                    idCalificacion,
                    tipo == TipoCalificacion.Espacio
                        ? "Gracias. Tu reseña del espacio ya fue publicada."
                        : "Gracias. La calificación del solicitante quedó registrada.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<int>.Error(
                    "No pudimos guardar la calificación. Verificá que la reserva esté finalizada y que no la hayas calificado antes.");
            }
        }
    }
}
