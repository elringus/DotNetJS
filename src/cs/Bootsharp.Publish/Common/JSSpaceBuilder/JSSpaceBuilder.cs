﻿using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class JSSpaceBuilder
{
    private readonly List<JSSpaceConverter> converters = [];

    public void CollectConverters (string outDir, string entryAssembly)
    {
        using var context = CreateLoadContext(outDir);
        var assemblyPath = Path.Combine(outDir, entryAssembly);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        foreach (var attribute in CollectAttributes(assembly))
            converters.Add(new JSSpaceConverter(attribute));
    }

    public string Build (string fullname, bool global)
    {
        var prefix = global ? "Global." : "";
        var space = $"{prefix}{fullname.Replace("+", ".")}";
        foreach (var converter in converters)
            space = converter.Convert(space);
        return space;
    }

    public string Build (Type type) => Build(type.FullName!, type.Namespace is null);

    private IEnumerable<CustomAttributeData> CollectAttributes (Assembly assembly)
    {
        return assembly.CustomAttributes.Where(a =>
            a.AttributeType.FullName == typeof(JSNamespaceAttribute).FullName);
    }
}
