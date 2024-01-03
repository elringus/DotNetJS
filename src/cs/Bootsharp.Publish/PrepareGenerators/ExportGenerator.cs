namespace Bootsharp.Publish;

internal sealed class ExportGenerator
{
    public string Generate (AssemblyInspection inspection)
    {
        var bySpace = inspection.Methods
            .Where(m => m.Type == MethodType.Invokable)
            .GroupBy(i => i.DeclaringName).ToArray();
        return bySpace.Length == 0 ? "" :
            $"""
             #nullable enable
             #pragma warning disable

             using System.Runtime.InteropServices.JavaScript;
             using static Bootsharp.Serializer;

             namespace Bootsharp.Exports;

             {JoinLines(bySpace.Select(g => GenerateSpace(g.Key, g)), 0)}

             #pragma warning restore
             #nullable restore
             """;
    }

    private string GenerateSpace (string space, IEnumerable<Method> invokable) =>
        $$"""
          public partial class {{space.Replace('.', '_')}}
          {
              {{JoinLines(invokable.Select(GenerateExport))}}
          }
          """;

    private string GenerateExport (Method inv)
    {
        const string attr = "[System.Runtime.InteropServices.JavaScript.JSExport]";
        var date = MarshalAmbiguous(inv.ReturnTypeSyntax, true);
        var wait = inv.ReturnsTaskLike && inv.ShouldSerializeReturnType;
        return $"{attr} {date}internal static {GenerateSignature(inv, wait)} => {GenerateBody(inv, wait)};";
    }

    private string GenerateSignature (Method inv, bool wait)
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateSignatureArg));
        var @return = inv.ReturnsVoid ? "void" : (inv.ShouldSerializeReturnType
            ? $"global::System.String{(inv.ReturnsNullable ? "?" : "")}"
            : inv.ReturnTypeSyntax);
        if (inv.ShouldSerializeReturnType && inv.ReturnsTaskLike)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var signature = $"{@return} {inv.Name} ({args})";
        if (wait) signature = $"async {signature}";
        return signature;
    }

    private string GenerateBody (Method inv, bool wait)
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateBodyArg));
        var body = $"global::{inv.DeclaringName}.{inv.Name}({args})";
        if (wait) body = $"await {body}";
        if (inv.ShouldSerializeReturnType) body = $"Serialize({body})";
        return body;
    }

    private string GenerateSignatureArg (Argument arg)
    {
        var type = arg.ShouldSerialize
            ? $"global::System.String{(arg.Nullable ? "?" : "")}"
            : arg.TypeSyntax;
        return $"{MarshalAmbiguous(arg.TypeSyntax, false)}{type} {arg.Name}";
    }

    private string GenerateBodyArg (Argument arg)
    {
        if (!arg.ShouldSerialize) return arg.Name;
        return $"Deserialize<{arg.TypeSyntax}>({arg.Name})";
    }
}
