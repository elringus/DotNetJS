﻿using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspector (Preferences prefs, string entryAssemblyName)
{
    private readonly List<InterfaceMeta> interfaces = [];
    private readonly List<MethodMeta> methods = [];
    private readonly List<string> warnings = [];
    private readonly TypeConverter converter = new(prefs);

    public AssemblyInspection InspectInDirectory (string directory, IEnumerable<string> paths)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in paths)
            try { InspectAssemblyFile(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        return CreateInspection(ctx);
    }

    private void InspectAssemblyFile (string assemblyPath, MetadataLoadContext ctx)
    {
        if (!ShouldIgnoreAssembly(assemblyPath))
            InspectAssembly(ctx.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{assemblyName}' assembly; " +
                      $"affected methods won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private AssemblyInspection CreateInspection (MetadataLoadContext ctx) => new(ctx) {
        Interfaces = [..interfaces],
        Methods = [..methods],
        Crawled = [..converter.CrawledTypes],
        Warnings = [..warnings]
    };

    private void InspectAssembly (Assembly assembly)
    {
        foreach (var exported in assembly.GetExportedTypes())
            InspectExportedType(exported);
        foreach (var attribute in assembly.CustomAttributes)
            InspectAssemblyAttribute(attribute);
    }

    private void InspectExportedType (Type type)
    {
        if (type.Namespace?.StartsWith("Bootsharp.Generated") ?? false) return;
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        foreach (var attr in method.CustomAttributes)
            if (attr.AttributeType.FullName == typeof(JSInvokableAttribute).FullName)
                methods.Add(CreateMethod(method, MethodKind.Invokable));
            else if (attr.AttributeType.FullName == typeof(JSFunctionAttribute).FullName)
                methods.Add(CreateMethod(method, MethodKind.Function));
            else if (attr.AttributeType.FullName == typeof(JSEventAttribute).FullName)
                methods.Add(CreateMethod(method, MethodKind.Event));
    }

    private void InspectAssemblyAttribute (CustomAttributeData attribute)
    {
        var name = attribute.AttributeType.FullName;
        var kind = name == typeof(JSExportAttribute).FullName ? InterfaceKind.Export
            : name == typeof(JSImportAttribute).FullName ? InterfaceKind.Import
            : (InterfaceKind?)null;
        if (!kind.HasValue) return;
        foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!)
            AddInterface((Type)arg.Value!, kind.Value);
    }

    private void AddInterface (Type iType, InterfaceKind kind)
    {
        var meta = CreateInterface(iType, kind);
        interfaces.Add(meta);
        foreach (var method in meta.Methods)
            methods.Add(method.Generated);
    }

    private MethodMeta CreateMethod (MethodInfo info, MethodKind kind) => new() {
        Kind = kind,
        Assembly = info.DeclaringType!.Assembly.GetName().Name!,
        Space = info.DeclaringType.FullName!,
        Name = info.Name,
        Arguments = info.GetParameters().Select(CreateArgument).ToArray(),
        ReturnValue = new() {
            Type = info.ReturnType,
            TypeSyntax = BuildSyntax(info.ReturnType, info.ReturnParameter),
            JSTypeSyntax = converter.ToTypeScript(info.ReturnType, GetNullability(info.ReturnParameter)),
            Nullable = IsNullable(info),
            Async = IsTaskLike(info.ReturnType),
            Void = IsVoid(info.ReturnType),
            Serialized = ShouldSerialize(info.ReturnType)
        },
        JSSpace = BuildMethodSpace(info),
        JSName = WithPrefs(prefs.Function, info.Name, ToFirstLower(info.Name))
    };

    private ArgumentMeta CreateArgument (ParameterInfo info) => new() {
        Name = info.Name!,
        JSName = info.Name == "function" ? "fn" : info.Name!,
        Value = new() {
            Type = info.ParameterType,
            TypeSyntax = BuildSyntax(info.ParameterType, info),
            JSTypeSyntax = converter.ToTypeScript(info.ParameterType, GetNullability(info)),
            Nullable = IsNullable(info),
            Async = false,
            Void = false,
            Serialized = ShouldSerialize(info.ParameterType)
        }
    };

    private InterfaceMeta CreateInterface (Type iType, InterfaceKind kind)
    {
        var space = "Bootsharp.Generated." + (kind == InterfaceKind.Export ? "Exports" : "Imports");
        if (iType.Namespace != null) space += $".{iType.Namespace}";
        var name = "JS" + iType.Name[1..];
        return new InterfaceMeta {
            Kind = kind,
            TypeSyntax = BuildSyntax(iType),
            Namespace = space,
            Name = name,
            Methods = iType.GetMethods().Select(m => CreateInterfaceMethod(m, kind, $"{space}.{name}")).ToArray()
        };
    }

    private InterfaceMethodMeta CreateInterfaceMethod (MethodInfo info, InterfaceKind iKind, string space)
    {
        var name = WithPrefs(prefs.Event, info.Name, info.Name);
        var mKind = iKind == InterfaceKind.Export ? MethodKind.Invokable
            : name != info.Name ? MethodKind.Event : MethodKind.Function;
        return new() {
            Name = info.Name,
            Generated = CreateMethod(info, mKind) with {
                Assembly = entryAssemblyName,
                Space = space,
                Name = name,
                JSName = ToFirstLower(name)
            }
        };
    }

    private string BuildMethodSpace (MethodInfo info)
    {
        var space = info.DeclaringType!.Namespace ?? "";
        var name = BuildJSSpaceName(info.DeclaringType);
        if (info.DeclaringType.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
