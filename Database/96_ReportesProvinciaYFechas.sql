USE [StageUp];
GO
CREATE OR ALTER PROCEDURE dbo.sp_Reporte_IngresosPorZona
    @desde       DATE,
    @hasta       DATE,
    @moneda      NVARCHAR(3) = N'ARS',
    @tipoEspacio NVARCHAR(200) = NULL,
    @provincia   NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(f.provincia, N'Sin ubicación') AS provincia,
        ISNULL(f.ciudad, N'-') AS ciudad,
        COUNT(*) AS cantidadReservas,
        SUM(ISNULL(r.importeEstimado, 0)) AS importeReservas
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE r.estadoReserva IN (N'Aceptada', N'Finalizada')
      AND r.estadoPago IN (N'Pagado', N'NoRequerido')
      AND ISNULL(r.moneda, N'ARS') = @moneda
      AND r.fechaSolicitada BETWEEN @desde AND @hasta
      AND (@provincia IS NULL OR f.provincia = @provincia)
      AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
    GROUP BY f.provincia, f.ciudad
    ORDER BY importeReservas DESC;
END
GO
