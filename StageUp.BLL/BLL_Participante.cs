using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Directorio de participantes de un gestor (no son usuarios de StageUp),
    // usado para asociarlos después a sus actividades internas.
    public class BLL_Participante
    {
        private readonly MPP_Participante _mppParticipante = new MPP_Participante();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Participante";

        public ResultadoOperacion<int> Guardar(Participante participante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(participante.Nombre) || string.IsNullOrWhiteSpace(participante.Apellido))
                {
                    return ResultadoOperacion<int>.Error("Ingresá nombre y apellido del participante.");
                }

                if (string.IsNullOrWhiteSpace(participante.Dni))
                {
                    return ResultadoOperacion<int>.Error("Ingresá el DNI del participante.");
                }

                participante.IdUsuarioGestor = idUsuarioGestor;

                bool yaExiste = _mppParticipante.ListarPorUsuarioGestor(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor })
                    .Any(existente => existente.Dni == participante.Dni && existente.IdParticipante != participante.IdParticipante);
                if (yaExiste)
                {
                    return ResultadoOperacion<int>.Error("Ya tenés cargado un participante con ese DNI.");
                }

                bool esNuevo = participante.IdParticipante == 0;
                int idParticipante = esNuevo
                    ? _mppParticipante.Insertar(participante)
                    : ModificarYDevolverId(participante);

                _bitacora.Registrar(
                    idUsuarioGestor, esNuevo ? "ALTA" : "MODIFICACION", TipoEntidadBitacora, idParticipante,
                    (esNuevo ? "Alta del participante " : "Modificación del participante ") + participante.NombreCompleto + ".");

                return ResultadoOperacion<int>.Ok(idParticipante, "Participante guardado correctamente.");
            });
        }

        private int ModificarYDevolverId(Participante participante)
        {
            _mppParticipante.Modificar(participante);
            return participante.IdParticipante;
        }

        public ResultadoOperacion DarDeBaja(int idParticipante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
                if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("El participante indicado no existe o no te pertenece.");
                }

                _mppParticipante.DarDeBaja(participante);

                _bitacora.Registrar(
                    idUsuarioGestor, "BAJA", TipoEntidadBitacora, idParticipante,
                    "Baja del participante " + participante.NombreCompleto + ".");

                return ResultadoOperacion.Ok();
            });
        }

        public List<Participante> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            try
            {
                return _mppParticipante.ListarPorUsuarioGestor(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Participante>();
            }
        }

        public Participante ObtenerParaEditar(int idParticipante, int idUsuarioGestor)
        {
            Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
            return participante != null && participante.IdUsuarioGestor == idUsuarioGestor ? participante : null;
        }

        private static ResultadoOperacion<T> EjecutarProtegido<T>(Func<ResultadoOperacion<T>> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<T>.Error(ex.Message);
            }
        }

        private static ResultadoOperacion EjecutarProtegido(Func<ResultadoOperacion> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }
    }
}
