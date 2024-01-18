﻿namespace Bootsharp;

/// <summary>
/// When applied to WASM entry point assembly, specified interfaces will
/// be automatically exported for consumption on JavaScript side.
/// </summary>
/// <remarks>
/// Generated bindings have to be initialized with the handler implementation.
/// For example, given "IHandler" interface is exported, "JSHandler" class will be generated,
/// which has to be instantiated with an "IHandler" implementation instance.
/// </remarks>
/// <example>
/// Expose "IHandlerA" and "IHandlerB" C# APIs to JavaScript and wrap invocations in "Utils.Try()":
/// <code>
/// [assembly: JSExport(
///     typeof(IHandlerA),
///     typeof(IHandlerB),
///     invokePattern = "(.+)",
///     invokeReplacement = "Utils.Try(() => $1)"
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSExportAttribute : Attribute
{
    /// <summary>
    /// The interface types to generated export bindings for.
    /// </summary>
    public Type[] Types { get; }

    /// <param name="types">The interface types to generate export bindings for.</param>
    public JSExportAttribute (params Type[] types)
    {
        Types = types;
    }
}
