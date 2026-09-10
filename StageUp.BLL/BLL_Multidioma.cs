using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Interfaces;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Multidioma : IObservableIdioma
    {
        private readonly MPP_Traduccion _mppTraduccion = new MPP_Traduccion();
        private readonly List<IObservadorIdioma> _observadores = new List<IObservadorIdioma>();

        public void RegistrarObservador(IObservadorIdioma observador)
        {
            if (observador != null && !_observadores.Contains(observador))
            {
                _observadores.Add(observador);
            }
        }

        public void RetirarObservador(IObservadorIdioma observador)
        {
            if (observador != null)
            {
                _observadores.Remove(observador);
            }
        }

        public void NotificarObservadores(Idioma idioma)
        {
            IList<Traduccion> traducciones;
            try
            {
                traducciones = _mppTraduccion.ListarDiccionario(idioma.IdIdioma);
            }
            catch (ErrorAccesoDatosException)
            {
                traducciones = new List<Traduccion>();
            }

            foreach (IObservadorIdioma observador in _observadores.ToArray())
            {
                observador.ActualizarIdioma(idioma, traducciones);
            }
        }
    }
}
