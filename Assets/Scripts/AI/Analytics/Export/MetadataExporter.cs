using UnityEngine;
using System.IO;
using System.Text;
using Economics.Managers;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta informatii despre rularea curenta (versiune, platforma, numar entitati).
    /// </summary>
    public class MetadataExporter : ICsvExporter
    {
        public void Export(ExportContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"simulation_run,{ctx.RunId}");
            sb.AppendLine($"start_time,{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"unity_version,{Application.unityVersion}");
            sb.AppendLine($"platform,{Application.platform}");
            sb.AppendLine($"parcels,{(ParcelCache.HasInstance ? ParcelCache.Parcels.Count : 0)}");

            int robotCount = RobotEconomicsManager.Instance != null
                ? RobotEconomicsManager.Instance.RobotStatsMap.Count : 0;
            sb.AppendLine($"robots,{robotCount}");

            File.WriteAllText(Path.Combine(ctx.FolderPath, "metadata.csv"), sb.ToString());
        }
    }
}
