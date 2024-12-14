using System.Collections.Generic;

namespace VisioWebTools
{
    public class MasterInfo 
    {
        public string Name { get; set; }
        public string NameU { get; set; }
    }

    public class FieldInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class UserRowInfo
    {
        public string Value { get; set; }
        public string Prompt { get; set; }
    }

    public class PropertyInfo
    {
        public string Label { get; set; }
        public string Prompt { get; set; }
        public string Type { get; set; }
        public string Format { get; set; }
        public string Value { get; set; }
    }

    public class ShapeInfo
    {
        public string Text { get; set; }
        public Dictionary<string, FieldInfo> FieldRows { get; set; }
        public Dictionary<string, PropertyInfo> PropRows { get; set; }
        public Dictionary<string, UserRowInfo> UserRows { get; set; }
    }

    public class PageInfo
    {
        public string Name { get; set; }
        public int? Index { get; set; }
        public Dictionary<string, UserRowInfo> UserRows { get; set; }
        public Dictionary<int, ShapeInfo> Shapes { get; set; }
    }

    public class DocumentInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }

        public Dictionary<string, UserRowInfo> UserRows { get; set; }
        public Dictionary<int, PageInfo> Pages { get; set; }
        public Dictionary<string, MasterInfo> Masters { get; set; }
    }
}