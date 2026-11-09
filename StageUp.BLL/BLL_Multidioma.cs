using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Interfaces;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Multidioma : IObservableIdioma
    {
        private static readonly object CacheLock = new object();
        private static long VersionCache;
        private static readonly Dictionary<int, IList<Traduccion>> CacheTraducciones =
            new Dictionary<int, IList<Traduccion>>();

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
                traducciones = ObtenerDiccionario(idioma.IdIdioma);
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

        public static void InvalidarCache(int idIdioma)
        {
            lock (CacheLock)
            {
                CacheTraducciones.Remove(idIdioma);
                VersionCache++;
            }
        }

        private IList<Traduccion> ObtenerDiccionario(int idIdioma)
        {
            while (true)
            {
                long version;
                lock (CacheLock)
                {
                    IList<Traduccion> traduccionesCacheadas;
                    if (CacheTraducciones.TryGetValue(idIdioma, out traduccionesCacheadas))
                    {
                        return traduccionesCacheadas;
                    }
                    version = VersionCache;
                }

                IList<Traduccion> traducciones = _mppTraduccion.ListarDiccionario(
                    new Idioma { IdIdioma = idIdioma });

                lock (CacheLock)
                {
                    if (version != VersionCache) continue;
                    IList<Traduccion> traduccionesCacheadas;
                    if (CacheTraducciones.TryGetValue(idIdioma, out traduccionesCacheadas))
                    {
                        return traduccionesCacheadas;
                    }

                    CacheTraducciones[idIdioma] = traducciones;
                    return traducciones;
                }
            }
        }
    }
}
