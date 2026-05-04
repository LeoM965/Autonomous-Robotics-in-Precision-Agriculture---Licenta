using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Utilitar simplu pentru scrierea fisierelor CSV cu formatare InvariantCulture.
    /// Elimina duplicarea StringBuilder + File.WriteAllText din fiecare exporter.
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// Formateaza segmente interpolate cu InvariantCulture (punct decimal, nu virgula).
        /// Accepta mai multe segmente — fiecare e formatat individual cu InvariantCulture.
        /// Exemplu: Csv.Line($"{price:F2},", $"{weight:F3}") => "12.50,3.140"
        /// </summary>
        public static string Line(params FormattableString[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
                sb.Append(part.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        /// <summary>
        /// Scrie un CSV complet (suprascrie fisierul daca exista).
        /// </summary>
        public static void Write(string path, string header, List<string> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var row in rows)
                sb.AppendLine(row);
            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>
        /// Adauga randuri la un CSV existent (header-ul se scrie doar daca fisierul nu exista).
        /// </summary>
        public static void Append(string path, string header, List<string> rows)
        {
            var sb = new StringBuilder();
            if (!File.Exists(path))
                sb.AppendLine(header);
            foreach (var row in rows)
                sb.AppendLine(row);
            File.AppendAllText(path, sb.ToString());
        }
    }
}
