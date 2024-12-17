namespace VsdxTools;

public class JsonExportOptions
{
    public bool IncludeShapeText { get; set; }
    public bool IncludeShapeFields { get; set; }
    public bool IncludePropertyRows { get; set; }
    public bool IncludeUserRows { get; set; }
    public bool IncludeMasters { get; set; }
    public bool IncludeDocumentProperties { get; set; }
    public bool TranslatableOnly { get; set; }
}
