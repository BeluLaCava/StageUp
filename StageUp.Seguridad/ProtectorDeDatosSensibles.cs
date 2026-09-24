using System;

namespace StageUp.Seguridad
{
    // Ítem 12 del checklist de correcciones: antes, StageUp.MPP/MPP_UsuarioExterno.cs
    // decidía directamente cuándo cifrar/descifrar el teléfono (un "if está vacío
    // guardo DBNull, si no llamo a EncriptadorSimetrico.Encriptar" suelto dentro
    // del mapper). Esa regla de seguridad no le corresponde al MPP: el MPP debería
    // limitarse a mapear y persistir, no decidir cómo se protegen los datos
    // sensibles.
    //
    // Esta clase centraliza esa decisión en Seguridad, con el mismo espíritu que
    // ProtectorDeCredenciales (que ya hace esto para la contraseña). La diferencia
    // es que la contraseña vive en dos propiedades separadas del objeto
    // (Password en claro que llega desde la UI/BLL, PasswordHash que se persiste),
    // así que ProtectorDeCredenciales puede tomar el objeto completo y completarle
    // la propiedad protegida. El teléfono, en cambio, es una única propiedad
    // (Telefono) que representa tanto el valor en claro como el valor persistido
    // según el contexto — mutarla en el propio objeto de entidad sería peligroso
    // (cualquier código que siga usando ese mismo objeto en memoria después de
    // guardar se encontraría con el teléfono cifrado en vez del real). Por eso acá
    // se expone una función de ida (preparar para guardar) y una de vuelta
    // (recuperar al leer), en vez de mutar el objeto.
    public static class ProtectorDeDatosSensibles
    {
        // Devuelve el valor ya listo para bindear como parámetro SQL: DBNull si
        // no hay teléfono, o el texto cifrado si lo hay. El MPP no vuelve a tocar
        // EncriptadorSimetrico ni a decidir el caso "vacío" — solo llama a esto.
        public static object PrepararTelefonoParaGuardar(string telefonoEnClaro)
        {
            return string.IsNullOrEmpty(telefonoEnClaro)
                ? (object)DBNull.Value
                : EncriptadorSimetrico.Encriptar(telefonoEnClaro);
        }

        // Inversa de la anterior: toma el valor tal cual viene de una fila de la
        // base (puede ser DBNull) y devuelve el teléfono en claro, o null si no
        // había ninguno. Usa DesencriptarOMantener para no romper con datos
        // cargados antes de que existiera esta protección.
        public static string RecuperarTelefono(object valorAlmacenado)
        {
            if (valorAlmacenado == null || valorAlmacenado == DBNull.Value)
            {
                return null;
            }

            return EncriptadorSimetrico.DesencriptarOMantener(valorAlmacenado.ToString());
        }
    }
}
