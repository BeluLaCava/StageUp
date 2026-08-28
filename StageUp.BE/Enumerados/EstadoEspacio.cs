namespace StageUp.BE.Enumerados
{
    /// <summary>
    /// Estados posibles de un EspacioArtistico. No hay un catálogo fijo
    /// de estados definido explícitamente en la documentación de StageUp
    /// (CU-001-007 solo describe el flujo de alta/publicación/pausa/baja
    /// en texto) — este es un conjunto razonable elegido para cubrir ese
    /// flujo, a validar si la cátedra/cliente define otros nombres.
    /// El valor persistido en EspacioArtistico.estadoEspacio es el texto
    /// literal (ver MPP_EspacioArtistico / los stored procedures).
    /// </summary>
    public enum EstadoEspacio
    {
        Borrador,
        Publicado,
        Pausado,
        DadoDeBaja
    }
}
