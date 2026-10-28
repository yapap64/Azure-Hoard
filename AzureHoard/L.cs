using System.Collections.Generic;

namespace AzureHoard
{
    /// <summary>Textos de la interfaz. Inglés por defecto; la clave es el texto en inglés.</summary>
    public static class L
    {
        public static string Lang = "en";

        // orden: es, gl, pt
        static readonly Dictionary<string, string[]> D = new Dictionary<string, string[]>
        {
            { "Desktop", new[] { "Escritorio", "Escritorio", "Área de trabalho" } },
            { "New fence", new[] { "Nueva zona", "Nova zona", "Nova zona" } },
            { "Show or hide fences", new[] { "Mostrar u ocultar zonas", "Amosar ou agochar zonas", "Mostrar ou ocultar zonas" } },
            { "Start with Windows", new[] { "Iniciar con Windows", "Iniciar con Windows", "Iniciar com o Windows" } },
            { "Language", new[] { "Idioma", "Idioma", "Idioma" } },
            { "Open data folder", new[] { "Abrir carpeta de datos", "Abrir cartafol de datos", "Abrir pasta de dados" } },
            { "Exit", new[] { "Salir", "Saír", "Sair" } },
            { "Rename", new[] { "Renombrar", "Renomear", "Mudar o nome" } },
            { "Delete fence", new[] { "Eliminar zona", "Eliminar zona", "Eliminar zona" } },
            { "Roll up or unroll", new[] { "Enrollar o desenrollar", "Enrolar ou desenrolar", "Enrolar ou desenrolar" } },
            { "Open", new[] { "Abrir", "Abrir", "Abrir" } },
            { "Show in folder", new[] { "Mostrar en la carpeta", "Amosar no cartafol", "Mostrar na pasta" } },
            { "Remove from this fence", new[] { "Quitar de esta zona", "Quitar desta zona", "Remover desta zona" } },
        };

        public static string T(string key)
        {
            int i = Lang == "es" ? 0 : Lang == "gl" ? 1 : Lang == "pt" ? 2 : -1;
            string[] v;
            if (i < 0 || !D.TryGetValue(key, out v)) return key;
            return v[i];
        }
    }
}
