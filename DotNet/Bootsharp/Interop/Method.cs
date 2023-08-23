﻿using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to C# methods via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Namespace of the methods is expected to equal assembly name.
/// Both arguments and return types of the methods are expected to be JSON-serializable.
/// </remarks>
public static partial class Method
{
    /// <summary>
    /// Invokes C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string Invoke (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = MethodCache.Get(endpoint);
        if (method.Invoke(null, args != null ? Serializer.DeserializeArgs(@params, args) : null) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return any value.");
        return Serializer.Serialize(result);
    }

    /// <summary>
    /// Invokes void C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static void InvokeVoid (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = MethodCache.Get(endpoint);
        method.Invoke(null, args != null ? Serializer.DeserializeArgs(@params, args) : null);
    }

    /// <summary>
    /// Invokes asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>Task with JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task<string> InvokeAsync (string endpoint, string[]? args = null)
    {
        var (method, @params, taskResult) = MethodCache.Get(endpoint);
        if (method.Invoke(null, args != null ? Serializer.DeserializeArgs(@params, args) : null) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
        if (taskResult?.GetValue(task) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': missing task result.");
        return Serializer.Serialize(result);
    }

    /// <summary>
    /// Invokes void asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>Task representing completion status of the method.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task InvokeVoidAsync (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = MethodCache.Get(endpoint);
        if (method.Invoke(null, args != null ? Serializer.DeserializeArgs(@params, args) : null) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
    }
}
