namespace AI.Analytics.Export
{
    /// <summary>
    /// Contract comun pentru toti exporterii CSV.
    /// Permite orchestrarea lor uniforma din DataExporter.
    /// </summary>
    public interface ICsvExporter
    {
        void Export(ExportContext ctx);
    }
}
