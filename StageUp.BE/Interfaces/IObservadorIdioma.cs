using System.Collections.Generic;
using StageUp.BE.Entidades;

namespace StageUp.BE.Interfaces
{
    public interface IObservadorIdioma
    {
        void ActualizarIdioma(Idioma idioma, IList<Traduccion> traducciones);
    }
}
