using StageUp.BE.Entidades;

namespace StageUp.BE.Interfaces
{
    public interface IObservableIdioma
    {
        void RegistrarObservador(IObservadorIdioma observador);
        void RetirarObservador(IObservadorIdioma observador);
        void NotificarObservadores(Idioma idioma);
    }
}
