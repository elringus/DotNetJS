using System.Text;

namespace Bootsharp.Publish;

internal sealed class InternalPatcher (string dotnet, string runtime, string native)
{
    private const string url =
        """
        ((typeof window === "object" && "Deno" in window && Deno.build.os === "windows") || (typeof process === "object" && process.platform === "win32")) ? "file://dotnet.native.wasm" : "file:///dotnet.native.wasm"
        """;

    public void Patch ()
    {
        // Remove unnecessary environment-specific calls in .NET's internals,
        // that are offending bundlers and breaking usage in restricted environments,
        // such as VS Code web extensions. (https://github.com/elringus/bootsharp/issues/139)

        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("import.meta.url", url)
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);

        File.WriteAllText(runtime, File.ReadAllText(runtime, Encoding.UTF8)
            .Replace("pt('WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.')", "true")
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);

        File.WriteAllText(native, File.ReadAllText(native, Encoding.UTF8)
            .Replace("var _scriptDir = import.meta.url", "var _scriptDir = \"file:/\"")
            .Replace("require('url').fileURLToPath(new URL('./', import.meta.url))", "\"./\"")
            .Replace("require(\"url\").fileURLToPath(new URL(\"./\",import.meta.url))", "\"./\"") // when aggressive trimming enabled
            .Replace("new URL('dotnet.native.wasm', import.meta.url).href", "\"file:/\"")
            .Replace("new URL(\"dotnet.native.wasm\",import.meta.url).href", "\"file:/\"") // when aggressive trimming enabled
            .Replace("import.meta.url", url)
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);
    }
}
