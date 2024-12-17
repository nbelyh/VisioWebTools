using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace VsdxTools;

public class DiagramInfoService
{
    public static T EnsureCollection<T>(XElement xmlRow, Func<Dictionary<string, T>> getPropInfos) where T : new()
    {
        var rowName = xmlRow.Attribute("ID")?.Value ?? xmlRow.Attribute("N")?.Value ?? xmlRow.Attribute("IX")?.Value;
        var propInfos = getPropInfos();
        if (!propInfos.TryGetValue(rowName, out var propertyInfo))
        {
            propertyInfo = new T();
            propInfos.Add(rowName, propertyInfo);
        }

        return propertyInfo;
    }
}
