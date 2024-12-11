using System.Collections.Generic;

namespace VisioWebTools
{
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class PropertyInfo
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Format { get; set; }
    }

    public class ShapeInfo
    {
        public string Text { get; set; }
        public Dictionary<string, FieldInfo> FieldInfos { get; set; }
        public Dictionary<string, PropertyInfo> PropertyInfos { get; set; }
    }

    public class PageInfo
    {
        public string Name { get; set; }
        public string NameU { get; set; }
        public Dictionary<int, ShapeInfo> Shapes { get; set; }
    }

    public class DocumentInfo
    {
        public Dictionary<int, PageInfo> Pages { get; set; }
    }
}