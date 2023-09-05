﻿using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.CodeAnalysis;

namespace Bootsharp.Builder.Test;

public sealed class MockProject : IDisposable
{
    public BuildBootsharp BuildTask { get; }
    public byte[] MockWasmBinary { get; } = "MockWasmContent"u8.ToArray();

    private readonly MockCompiler compiler = new();

    public MockProject ()
    {
        BuildTask = CreateBuildTask();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(BuildTask.BuildDirectory, true);

    public void AddAssembly (MockAssembly assembly)
    {
        var assemblyPath = Path.Combine(BuildTask.BuildDirectory, assembly.Name);
        compiler.Compile(assembly.Sources, assemblyPath);
        BuildTask.EntryAssemblyName = assembly.Name;
    }

    private BuildBootsharp CreateBuildTask ()
    {
        var testDir = CreateRandomTestDirectory();
        return new BuildBootsharp {
            BuildDirectory = testDir,
            InspectedDirectory = testDir,
            EntryAssemblyName = "System.Runtime.dll",
            BuildEngine = BuildEngine.Create(),
            EmbedBinaries = false
        };
    }

    private void CreateBuildResources ()
    {
        Directory.CreateDirectory(BuildTask.BuildDirectory);
        foreach (var path in GetReferencePaths())
            File.Copy(path, Path.Combine(BuildTask.BuildDirectory, Path.GetFileName(path)), true);
        File.WriteAllBytes(Path.Combine(BuildTask.BuildDirectory, "dotnet.native.wasm"), MockWasmBinary);
    }

    private static string[] GetReferencePaths ()
    {
        var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        return new[] {
            MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")).FilePath,
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).FilePath,
            MetadataReference.CreateFromFile(typeof(JSFunctionAttribute).Assembly.Location).FilePath,
            MetadataReference.CreateFromFile(typeof(JSInvokableAttribute).Assembly.Location).FilePath
        };
    }

    private static string CreateRandomTestDirectory ()
    {
        var testAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Directory.CreateDirectory(Path.Combine(assemblyDir, $"temp{Guid.NewGuid():N}")).FullName;
    }
}
