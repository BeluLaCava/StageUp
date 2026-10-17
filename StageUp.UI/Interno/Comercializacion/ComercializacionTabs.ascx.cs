using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI.Interno.Comercializacion
{
    public partial class ComercializacionTabs : UserControl
    {
        private sealed class Solapa
        {
            public string Permiso { get; set; }
            public string Url { get; set; }
            public string Texto { get; set; }
            public bool Actual { get; set; }
        }

        private static readonly Solapa[] Solapas =
        {
            new Solapa { Permiso = "GESTIONAR_CAMPANIAS_COMERCIALES", Url = "~/Interno/Comercializacion/CampaniasComerciales.aspx", Texto = "Campañas" },
            new Solapa { Permiso = "GESTIONAR_CONTACTOS_COMERCIALES", Url = "~/Interno/Comercializacion/ContactosComerciales.aspx", Texto = "Contactos" }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            string actual = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            List<Solapa> visibles = Solapas
                .Where(s => GestorDeSesion.TienePermisoInterno(s.Permiso))
                .Select(s => new Solapa
                {
                    Permiso = s.Permiso,
                    Url = s.Url,
                    Texto = s.Texto,
                    Actual = string.Equals(s.Url, actual, StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            Visible = visibles.Count > 1;
            rptSolapas.DataSource = visibles;
            rptSolapas.DataBind();
        }
    }
}
