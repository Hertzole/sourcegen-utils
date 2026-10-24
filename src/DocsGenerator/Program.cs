using System.Text;
using System.Text.RegularExpressions;
using Hertzole.SourceGenUtils;

namespace DocsGenerator;

public static partial class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Must provide output path");
            return;
        }

        string path = args[0];

        if (!Path.IsPathFullyQualified(path))
        {
            Console.WriteLine("Invalid path.");
            return;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        List<GeneratorType> types = new List<GeneratorType>();

        foreach ((string typeName, TypeSource type) in Generator.TypesToGenerate)
        {
            types.Add(new GeneratorType(typeName, type));
        }

        types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        int index = 0;
        for (int i = 0; i < types.Count; i++)
        {
            WriteType(path, types[i], ref index);
        }

        AppendIndex(path);
    }

    private static void AppendIndex(string path)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: API Reference");
        sb.AppendLine("description: API Reference for Hertzole.SourceGen");
        sb.AppendLine("sidebar:");
        sb.AppendLine("  hidden: true");
        sb.AppendLine("---");

        List<GeneratorType> types = new List<GeneratorType>();

        foreach ((string name, TypeSource type) in Generator.TypesToGenerate)
        {
            types.Add(new GeneratorType(name, type));
        }

        types.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        for (int i = 0; i < types.Count; i++)
        {
            AppendType(sb, types[i]);
        }

        File.WriteAllText(Path.Combine(path, "index.md"), sb.ToString());

        static void AppendType(StringBuilder sb, in GeneratorType type)
        {
            if (!type.IsAccessible)
            {
                return;
            }

            sb.Append("### [").Append(type.Name).Append("](../api/").Append(type.Name.ToLowerInvariant()).AppendLine(")").AppendLine();
            sb.AppendLine(Format(type.Summary, true)).AppendLine();

            if (type.Type.Types is Dictionary<string, TypeSource> nestedTypesSource)
            {
                List<GeneratorType> nestedTypes = new List<GeneratorType>();
                foreach ((string key, TypeSource value) in nestedTypesSource)
                {
                    nestedTypes.Add(new GeneratorType(key, value));
                }

                nestedTypes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

                for (int i = 0; i < nestedTypes.Count; i++)
                {
                    AppendType(sb, nestedTypes[i]);
                }
            }
        }
    }

    private static void WriteType(string path, in GeneratorType type, ref int index)
    {
        if (!type.IsAccessible)
        {
            return;
        }

        string filePath = Path.Combine(path, type.Name + ".md");

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine("title: " + type.Name);
        if (!string.IsNullOrEmpty(type.Summary))
        {
            sb.AppendLine("description: " + type.Summary);
        }

        sb.AppendLine("sidebar:");
        sb.AppendLine("  order: " + index++);
        sb.AppendLine("---");

        if (!string.IsNullOrEmpty(type.Summary))
        {
            sb.AppendLine(Format(type.Summary)).AppendLine();
        }

        sb.AppendLine("```csharp title=\"C# Signature\"").AppendLine(type.Signature).AppendLine("```").AppendLine();

        AppendExample(sb, type.Example);

        if (type.Type.Trivia != null && !string.IsNullOrEmpty(type.Type.Trivia.Remarks))
        {
            sb.AppendLine("## Remarks").AppendLine().AppendLine(Format(type.Type.Trivia.Remarks)).AppendLine();
        }

        AppendConstructors(sb, in type);
        AppendProperties(sb, type.Type);
        AppendMethods(sb, in type);
        AppendOperators(sb, in type);

        File.WriteAllText(filePath, sb.ToString());

        if (type.Type.Types != null && type.Type.Types.Count > 0)
        {
            List<GeneratorType> nestedTypes = new List<GeneratorType>();

            foreach ((string nestedTypeName, TypeSource nestedType) in type.Type.Types)
            {
                nestedTypes.Add(new GeneratorType(nestedTypeName, nestedType));
            }

            nestedTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            for (int i = 0; i < nestedTypes.Count; i++)
            {
                WriteType(path, nestedTypes[i], ref index);
            }
        }
    }

    private static void AppendExample(StringBuilder sb, string? example, int depth = 0)
    {
        if (string.IsNullOrEmpty(example))
        {
            return;
        }

        sb.Append('#', depth);
        sb.AppendLine("## Examples").AppendLine();

        sb.AppendLine(FixLangWords(FixCode(example.Trim().Replace("<!code>", "<code>").Replace("<code>", "```csharp title=\"C#\"").Replace("</code>", "```"))))
          .AppendLine();
    }

    private static void AppendConstructors(StringBuilder sb, in GeneratorType type)
    {
        if (type.Type.Methods == null || type.Type.Methods.Length == 0)
        {
            return;
        }

        bool hasWrittenTitle = false;

        List<MethodSource> constructors = new List<MethodSource>();
        foreach (MethodSource method in type.Type.Methods)
        {
            if (method.Name == type.Name)
            {
                constructors.Add(method);
            }
        }

        constructors.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        for (int i = 0; i < constructors.Count; i++)
        {
            MethodSource method = constructors[i];

            if (!hasWrittenTitle)
            {
                sb.AppendLine("## Constructors").AppendLine();
                hasWrittenTitle = true;
            }

            AppendMethod(sb, method);
        }
    }

    private readonly record struct GeneratedProperty(string Name, PropertySource Property);

    private static void AppendProperties(StringBuilder sb, TypeSource type)
    {
        if (type.Properties == null || type.Properties.Count == 0)
        {
            return;
        }

        List<GeneratedProperty> properties = new List<GeneratedProperty>();

        foreach (KeyValuePair<string, PropertySource> property in type.Properties)
        {
            properties.Add(new GeneratedProperty(property.Key, property.Value));
        }

        sb.AppendLine("## Properties").AppendLine();

        for (int i = 0; i < properties.Count; i++)
        {
            string name = properties[i].Name;
            PropertySource value = properties[i].Property;

            sb.Append("### ").AppendLine(name == "indexer" ? "Item[]" : name).AppendLine();

            TriviaSource? trivia = value.Trivia;
            if (trivia != null && !string.IsNullOrEmpty(trivia.Summary))
            {
                sb.AppendLine(Format(trivia.Summary)).AppendLine();
            }

            sb.AppendLine("```csharp title=\"C# Signature\"");
            sb.Append(value.Signature);
            if (!value.Signature.Contains("get;") && !value.Signature.Contains("set;"))
            {
                sb.Append(" {");

                if (value.GetImplementation != null)
                {
                    sb.Append(" get;");
                }

                if (value.SetImplementation != null)
                {
                    sb.Append(" set;");
                }

                if (value.GetImplementation == null && value.SetImplementation == null)
                {
                    sb.Append(" get; set;");
                }

                sb.AppendLine(" }");
            }

            sb.AppendLine("```").AppendLine();
        }
    }

    private static void AppendMethods(StringBuilder sb, in GeneratorType type)
    {
        if (type.Type.Methods is not MethodSource[] methodsSource)
        {
            return;
        }

        List<MethodSource> methods = new List<MethodSource>();
        foreach (MethodSource method in methodsSource)
        {
            if (method.Name == type.Name || method.Signature.Contains('~') || method.Signature.Contains("operator") || method.Signature.Contains("private"))
            {
                // Skip constructors and finalizers
                continue;
            }

            methods.Add(method);
        }

        methods.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        bool hasWrittenTitle = false;

        for (int i = 0; i < methods.Count; i++)
        {
            if (!hasWrittenTitle)
            {
                sb.AppendLine("## Methods").AppendLine();
                hasWrittenTitle = true;
            }

            AppendMethod(sb, methods[i]);
        }
    }

    private static void AppendOperators(StringBuilder sb, in GeneratorType type)
    {
        if (type.Type.Methods is not MethodSource[] methods)
        {
            return;
        }

        bool hasWrittenTitle = false;

        for (int i = 0; i < methods.Length; i++)
        {
            MethodSource method = methods[i];
            if (!method.Signature.Contains("operator"))
            {
                // Skip non-operators
                continue;
            }

            if (!hasWrittenTitle)
            {
                sb.AppendLine("## Operators").AppendLine();
                hasWrittenTitle = true;
            }

            bool isImplicit = method.Signature.Contains("implicit", StringComparison.Ordinal);

            string name;
            if (isImplicit)
            {
                name = OperatorRegex().Replace(FixGenerics(ShortenTypes(method.Signature)), "Implicit($2 to $1)");
            }
            else
            {
                name = method.Name switch
                {
                    "==" or "op_Equality" => $"Equality({FixGenerics(ShortenTypes(method.ParameterTypesKey))})",
                    "!=" or "op_Inequality" => $"Inequality({FixGenerics(ShortenTypes(method.ParameterTypesKey))})",
                    _ => method.Name
                };
            }

            AppendMethod(sb, method, name);
        }
    }

    [GeneratedRegex(@".*?operator ([^(]*)\(([^)]*) [^)]*\)")]
    private static partial Regex OperatorRegex();

    private static void AppendMethod(StringBuilder sb, MethodSource method, string? name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = $"{method.Name}({FixGenerics(ShortenTypes(method.ParameterTypesKey))})";
        }

        sb.AppendLine($"### {name}").AppendLine();

        TriviaSource? trivia = method.Trivia;

        if (trivia != null && !string.IsNullOrEmpty(trivia.Summary))
        {
            sb.AppendLine(Format(trivia.Summary)).AppendLine();
        }

        sb.AppendLine("```csharp title=\"C# Signature\"")
          .Append(ShortenTypes(method.Signature).Replace("partial ", string.Empty)).AppendLine(";")
          .AppendLine("```").AppendLine();

        if (trivia != null)
        {
            if (trivia.Parameters is Dictionary<string, string> parameters)
            {
                foreach ((string key, string value) in parameters)
                {
                    sb.AppendLine($"##### `{key}`").AppendLine().AppendLine(Format(value)).AppendLine();
                }
            }

            AppendExample(sb, trivia.Example, 2);

            if (!string.IsNullOrEmpty(trivia.Remarks))
            {
                sb.AppendLine("#### Remarks").AppendLine().AppendLine(Format(trivia.Remarks)).AppendLine();
            }

            if (!string.IsNullOrEmpty(trivia.Returns))
            {
                sb.AppendLine("#### Returns").AppendLine().AppendLine(Format(trivia.Returns)).AppendLine();
            }
        }
    }

    private readonly record struct GeneratorType(string Name, TypeSource Type)
    {
        public bool IsAccessible
        {
            get { return Type.Signature.Contains("internal", StringComparison.Ordinal); }
        }

        public string Summary
        {
            get { return Type.Trivia?.Summary ?? string.Empty; }
        }

        public string Signature
        {
            get { return Type.Signature.Replace("partial ", string.Empty).Replace("global::", string.Empty).Replace("internal", string.Empty); }
        }

        public string Example
        {
            get { return Type.Trivia?.Example ?? string.Empty; }
        }
    }

    [GeneratedRegex("<see cref=\"([^\"]*)\"(?: ?>([^\"]*)?</see>|/>)")]
    private static partial Regex SolveTypeReferencesRegex();

    private static string SolveTypeReferences(string value, bool isIndex)
    {
        if (!value.Contains("<see cref="))
        {
            return value;
        }

        StringBuilder sb = new StringBuilder(value);

        MatchCollection matches = SolveTypeReferencesRegex().Matches(value);

        for (int i = 0; i < matches.Count; i++)
        {
            string path = GetPath(matches[i], isIndex);

            if (matches[i].Groups.Count == 3 && !string.IsNullOrEmpty(matches[i].Groups[2].Value))
            {
                // Custom name
                sb.Replace(matches[i].Value, $"[{matches[i].Groups[2].Value}]({path})");
            }
            else
            {
                string text = matches[i].Groups[1].Value;
                int paren = text.IndexOf('(');
                if (paren >= 0)
                {
                    text = text.Substring(0, paren);
                }

                // No name
                sb.Replace(matches[i].Value, $"[{text}]({path})");
            }
        }

        return sb.ToString();

        static string GetPath(Match match, bool isIndex)
        {
            string value = match.Groups[1].Value;

            if (!value.StartsWith("global::", StringComparison.Ordinal))
            {
                // Method reference on the current type page
                int paren = value.IndexOf('(');
                string methodPart = paren >= 0 ? value.Substring(0, paren) : value;
                methodPart = methodPart.Substring(methodPart.LastIndexOf('.') + 1);
                string paramPart = paren >= 0 ? value.Substring(paren) : string.Empty;
                // Strip namespace qualifiers from params
                paramPart = Regex.Replace(paramPart, @"[A-Za-z_]\w*\.", string.Empty);
                string slug = (methodPart + paramPart).ToLowerInvariant().Replace(" ", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty)
                                                      .Replace(",", string.Empty).Replace("{", string.Empty)
                                                      .Replace("}", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);

                return "#" + slug;
            }

            StringBuilder sb = new StringBuilder(value.ToLowerInvariant());

            if (value.Contains("Hertzole.SourceGen", StringComparison.Ordinal))
            {
                sb.Remove(value.IndexOf("Hertzole.SourceGen", StringComparison.Ordinal), 19);

                int generic = sb.ToString().IndexOf('{');
                if (generic != -1)
                {
                    int closeGeneric = sb.ToString().IndexOf('}', generic);
                    sb.Remove(generic, closeGeneric - generic + 1);
                }

                int globalIndex = sb.ToString().IndexOf("global::", StringComparison.Ordinal);

                if (globalIndex != -1)
                {
                    sb.Remove(globalIndex, 8);
                }

                if (isIndex)
                {
                    sb.Append('/');
                }
                else
                {
                    sb.Insert(0, "../").Append('/');
                }
            }
            else
            {
                string v = value;
                if (v.StartsWith("global::", StringComparison.Ordinal))
                {
                    v = v.Substring("global::".Length);
                }

                sb.Clear();
                sb.Append(v.ToLowerInvariant());

                ReadOnlySpan<char> span = sb.ToString().AsSpan();
                int generic = span.IndexOf('{');
                if (generic != -1)
                {
                    int closeGeneric = span.LastIndexOf('}');

                    int genericCount = span.Slice(generic, closeGeneric - generic).Count(',');

                    sb.Replace(span.Slice(generic, closeGeneric - generic + 1), $"-{genericCount + 1}");
                }

                sb.Insert(0, "https://learn.microsoft.com/dotnet/api/");
            }

            return sb.ToString();
        }
    }

    [GeneratedRegex("<c>([^<]*)< ?/c>")]
    private static partial Regex CodeRegex();

    private static string Format(string value, bool isIndex = false)
    {
        value = value.Replace("<br/>", "\n\n");
        value = FixCode(value);
        value = FixParamref(value);
        value = SolveTypeReferences(value, isIndex);
        value = FixLangWords(value);
        value = FixGenerics(value);

        return value;
    }

    [GeneratedRegex("<see langword=\"([^\"]*)\" ?/>")]
    private static partial Regex LangWordRegex();

    private static string FixCode(string value)
    {
        return CodeRegex().Replace(value, "`$1`");
    }

    private static string FixLangWords(string value)
    {
        return LangWordRegex().Replace(value, "`$1`");
    }

    [GeneratedRegex("<paramref name=\"([^\"]*)\" ?/>")]
    private static partial Regex ParamrefRegex();

    private static string FixParamref(string value)
    {
        return ParamrefRegex().Replace(value, "`$1`");
    }

    private static string ShortenTypes(string type)
    {
        return ShortenTypeRegex().Replace(type.Replace("global::", string.Empty), "$1");
    }

    private static string FixGenerics(string value)
    {
        return value.Replace("<", "\\<").Replace(">", "\\>");
    }

    [GeneratedRegex(@"(?:[A-Za-z_]\w*\.)+([A-Za-z_]\w*)")]
    private static partial Regex ShortenTypeRegex();
}