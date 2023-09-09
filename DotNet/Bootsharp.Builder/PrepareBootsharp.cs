﻿using Microsoft.Build.Framework;

namespace Bootsharp.Builder;

public sealed class PrepareBootsharp : Microsoft.Build.Utilities.Task
{
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required string SerializerFilePath { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspector = InspectAssemblies(spaceBuilder);
        GenerateSerializer(inspector);
        return true;
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(InspectedDirectory, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(InspectedDirectory);
        inspector.Report(Log);
        return inspector;
    }

    private void GenerateSerializer (AssemblyInspector inspector)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspector);
        Directory.CreateDirectory(Path.GetDirectoryName(SerializerFilePath)!);
        File.WriteAllText(SerializerFilePath, content);
    }
}
