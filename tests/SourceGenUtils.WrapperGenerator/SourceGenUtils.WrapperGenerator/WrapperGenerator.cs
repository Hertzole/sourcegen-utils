using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Hertzole.SourceGen;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using CodeWriter = Hertzole.SourceGen.CodeWriter;
using Log = Hertzole.SourceGen.Log;

[assembly: EnableRecordSupport]
[assembly: EnableRequiredSupport]

namespace SourceGenUtils.WrapperGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class WrapperGenerator : IIncrementalGenerator
{
    private static readonly string[] bannedMethods = new[]
    {
        "Finalizer"
    };

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Log.Info("Initialize wrapper generator");

        context.RegisterPostInitializationOutput(static context =>
        {
            try
            {
                using CodeWriter writer = new CodeWriter();

                foreach (KeyValuePair<string, TypeSource> pair in Generator.TypesToGenerate)
                {
                    Log.Info($"Write {pair.Key}");
                    writer.AppendNullable();
                    writer.AppendNamespace("Hertzole.SourceGen.Wrappers");

                    WriteType(writer, pair);

                    context.AddSource($"{pair.Key}.Wrapper.g.cs", writer);
                }
            }

            catch (Exception e)
            {
                Log.Error(e);
            }
        });

        static void WriteType(CodeWriter writer, KeyValuePair<string, TypeSource> pair)
        {
            SourceTypeSignature signature = SourceTypeSignature.FromSignature(pair.Value.Signature);

            writer.AppendLine(GetSignature(pair.Value));
            using (writer.WithBlock())
            {
                writer.AppendLine("public global::System.Type Type { get; private set; }");
                bool isStatic = pair.Value.Signature.Contains("static");

                // Only non-static types can have instances.
                if (!isStatic)
                {
                    writer.AppendLine("public object Instance { get; private set; }");
                }

                WriteFields(writer, pair.Value);
                WriteProperties(writer, pair.Value, in signature);

                WriteMethodsProperties(writer, pair.Value, pair.Key);

                writer.AppendLine();
                if (!HasConstructor(pair.Value, pair.Key))
                {
                    writer.AppendLine("public " + pair.Key + "(global::System.Type type)");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("Type = type;");
                    }
                }

                writer.Append("public ").Append(pair.Key).Append("(global::System.Reflection.Assembly assembly) : this(assembly.GetType(\"")
                      .Append(Generator.NAMESPACE).Append('.').Append(pair.Key);

                if (signature.IsGeneric)
                {
                    writer.Append('`').Append(signature.GenericArgsCount);
                }

                writer.Append("\", true)!");

                if (signature.IsGeneric)
                {
                    // Close the generic type with the wrapper's own generic arguments so constructors can create an instance.
                    writer.Append(".MakeGenericType(");
                    AppendGenericArgumentTypes(writer, signature.GenericParameters);
                    writer.Append(')');
                }

                writer.AppendLine(") { }");

                if (!isStatic)
                {
                    writer.Append("public ").Append(pair.Key).AppendLine("(object instance)");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("Type = instance.GetType();").AppendLine("Instance = instance;");
                    }
                }

                WriteMethodsImplementations(writer, pair.Value, pair.Key);

                if (pair.Value.Types != null && pair.Value.Types.Count > 0)
                {
                    foreach (KeyValuePair<string, TypeSource> nestedPair in pair.Value.Types)
                    {
                        WriteType(writer, nestedPair);
                    }
                }
            }
        }

        static bool HasConstructor(TypeSource type, string typeName)
        {
            if (type.Methods == null || type.Methods.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < type.Methods.Length; i++)
            {
                if (type.Methods[i].Name == typeName)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static void WriteFields(CodeWriter writer, TypeSource type)
    {
        if (type.Fields == null || type.Fields.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, FieldSource> field in type.Fields)
        {
            SourceFieldSignature signature = SourceFieldSignature.FromSignature(field.Value.Signature);

            writer.Append("// ").AppendLine(signature.ToString());
            writer.Append("private global::System.Reflection.FieldInfo FieldImpl_").AppendLine(signature.Name);
            using (writer.WithBlock())
            {
                writer.AppendLine("get");
                using (writer.WithBlock())
                {
                    writer.AppendLine("if (field == null)");
                    using (writer.WithBlock())
                    {
                        writer.Append("field = Type.GetField(\"").Append(signature.Name).Append("\", ");
                        signature.AppendFlags(writer);
                        writer.AppendLine(");");

                        writer.AppendLine("if (field == null)");
                        using (writer.WithBlock())
                        {
                            writer.Append("throw new global::System.Exception(\"Field not found: ").Append(signature.Name).AppendLine("\");");
                        }
                    }

                    writer.AppendLine("return field;");
                }
            }

            writer.Append("public ").Append(WrapType(signature.Type.ToString())).Append(' ').AppendLine(signature.Name);
            using (writer.WithBlock(true))
            {
                writer.AppendLine("get");
                using (writer.WithBlock())
                {
                    writer.Append("return ");
                    bool needsNew = signature.Type.Contains("Hertzole.SourceGen", StringComparison.Ordinal);
                    if (needsNew)
                    {
                        writer.Append("new ").Append(WrapType(signature.Type.ToString())).Append('(');
                    }
                    else
                    {
                        writer.Append("(").Append(WrapType(signature.Type.ToString())).Append(')');
                    }

                    writer.Append("FieldImpl_").Append(signature.Name).Append(".GetValue(");

                    writer.Append(signature.IsStatic ? "null" : "Instance").Append(")!");
                    if (needsNew)
                    {
                        writer.Append(')');
                    }

                    writer.AppendLine(";");
                }
            }
        }
    }

    private static void WriteProperties(CodeWriter writer, TypeSource type, in SourceTypeSignature typeSignature)
    {
        if (type.Properties == null || type.Properties.Count == 0)
        {
            return;
        }

        foreach (PropertySource prop in type.Properties.Values)
        {
            SourcePropertySignature signature = SourcePropertySignature.FromSignature(prop.Signature);

            writer.AppendLine($"// {signature.ToString()}");
            writer.Append("private global::System.Reflection.PropertyInfo ").Append(signature.Name).AppendLine("_PropertyImpl");
            using (writer.WithBlock())
            {
                writer.AppendLine("get");
                using (writer.WithBlock())
                {
                    writer.AppendLine("if (field == null)");
                    using (writer.WithBlock(true))
                    {
                        writer.Append("field = Type.GetProperty(\"").Append(signature.IsIndexer ? "Item" : signature.Name).Append("\", ");
                        signature.AppendFlags(writer);
                        writer.AppendLine(");");
                        writer.AppendLine("if (field == null)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine($"throw new global::System.MissingMemberException(\"Could not find property {signature.Name.ToString()}\");");
                        }
                    }

                    writer.AppendLine("return field;");
                }
            }

            writer.Append("public ");

            if (typeSignature.IsStruct && typeSignature.IsReadOnly)
            {
                writer.Append("readonly ");
            }

            writer.Append(signature.Type).Append(' ').Append(signature.Name);
            if (signature.IsIndexer)
            {
                writer.Append("[int index]");
            }

            writer.AppendLine();
            using (writer.WithBlock())
            {
                bool autoImplement = !signature.HasImplicitGetter && !signature.HasImplicitSetter;

                if (prop.GetImplementation != null || signature.HasImplicitGetter || autoImplement)
                {
                    writer.Append("get { ");
                    writer.Append("return (").Append(WrapType(signature.Type.ToString())).Append(") ");
                    writer.Append(signature.Name).Append("_PropertyImpl.GetValue(");
                    writer.Append(signature.IsStatic ? "null" : "Instance");
                    if (signature.IsIndexer)
                    {
                        writer.Append(", [index]");
                    }

                    writer.AppendLine("); }");
                }

                if (prop.SetImplementation != null || signature.HasImplicitSetter || autoImplement)
                {
                    writer.Append("set { ");
                    writer.Append(signature.Name).Append("_PropertyImpl.SetValue(");
                    writer.Append(signature.IsStatic ? "null" : "Instance").Append(", value");
                    if (signature.IsIndexer)
                    {
                        writer.Append(", [index]");
                    }

                    writer.AppendLine("); }");
                }
            }
        }
    }

    private static string WrapType(string type)
    {
        if (!type.Contains("Hertzole.SourceGen", StringComparison.Ordinal))
        {
            return type;
        }

        return type.Replace("Hertzole.SourceGen", "Hertzole.SourceGen.Wrappers");
    }

    private static void WriteMethodsProperties(CodeWriter writer, TypeSource type, string typeName)
    {
        if (type.Methods == null || type.Methods.Length == 0)
        {
            return;
        }

        using ArrayBuilder<char> nameBuilder = new ArrayBuilder<char>();
        using ArrayBuilder<SourceParameterInfo> parametersBuilder = new ArrayBuilder<SourceParameterInfo>();

        foreach (MethodSource method in type.Methods)
        {
            SourceMethodSignature signature = SourceMethodSignature.FromSignature(method.Signature);
            parametersBuilder.Clear();
            SourceParameterInfo.FromSignature(method.Signature, parametersBuilder);
            if (IsMethodBanned(method, in signature, in parametersBuilder))
            {
                continue;
            }

            if (signature.HasRefStruct)
            {
                writer.Append("private ");
                AppendDelegate(writer, in signature, in parametersBuilder);

                string actualName = GetMethodName(typeName, method);
                writer.Append(' ').Append(SanitizeMethodName(signature.Name.ToString()));
                if (!string.IsNullOrWhiteSpace(method.ParameterTypesKey))
                {
                    writer.Append(FormatParameterTypesKey(method.ParameterTypesKey));
                }

                writer.AppendLine("_Delegate");
                using (writer.WithBlock())
                {
                    writer.AppendLine("get");
                    using (writer.WithBlock())
                    {
                        writer.AppendLine("if (field == null)");
                        using (writer.WithBlock(true))
                        {
                            writer.Append("global::System.Reflection.MethodInfo? __method = Type.GetMethod(\"").Append(actualName).Append("\", ");
                            signature.AppendFlags(writer);
                            writer.Append(", [");
                            for (int i = 0; i < parametersBuilder.Count; i++)
                            {
                                writer.Append("typeof(");
                                writer.Append(WrapType(parametersBuilder[i].Type));
                                writer.Append(')');

                                if (i < parametersBuilder.Count - 1)
                                {
                                    writer.Append(", ");
                                }
                            }

                            writer.AppendLine("]);");

                            writer.AppendLine("if (__method == null)");
                            using (writer.WithBlock(true))
                            {
                                writer.Append("throw new global::System.MissingMemberException(\"Method '").Append(actualName).Append("' not found in type '")
                                      .Append(typeName).AppendLine("' not found\");");
                            }

                            writer.Append("field = (");
                            AppendDelegate(writer, signature, parametersBuilder);
                            writer.AppendLine(')');
                            writer.Indent++;
                            writer.Append("global::System.Delegate.CreateDelegate(typeof(");
                            AppendDelegate(writer, in signature, in parametersBuilder);
                            writer.AppendLine("),").Append(signature.IsStatic ? "null" : "Instance").AppendLine(", __method);");

                            writer.Indent--;
                        }

                        writer.AppendLine("return field;");
                    }
                }
            }
            else
            {
                writer.Append("private global::System.Reflection.MethodInfo ");
                writer.Append(SanitizeMethodName(method.Name));

                if (signature.IsExplicitImplementation)
                {
                    nameBuilder.Clear();
                    nameBuilder.AddRange(signature.ReturnType);
                    SanitizeExplicitName(nameBuilder);

                    writer.Append('_').Append(nameBuilder);
                }

                if (!string.IsNullOrWhiteSpace(method.ParameterTypesKey))
                {
                    writer.Append(FormatParameterTypesKey(method.ParameterTypesKey));
                }

                string actualName = GetMethodName(typeName, method);

                writer.AppendLine("_MethodInfo");
                using (writer.WithBlock())
                {
                    writer.AppendLine("get");
                    using (writer.WithBlock())
                    {
                        writer.AppendLine("if (field == null)");
                        using (writer.WithBlock(true))
                        {
                            GetCustomTypes(writer, method);
                            writer.Append("field = Type.GetMethod(\"");
                            writer.Append(actualName);
                            writer.Append("\", ");
                            writer.Append(GetBindingFlags(method));
                            writer.Append(", ");
                            writer.Append(GetParameters(method));
                            writer.AppendLine(");");

                            writer.AppendLine("if (field == null)");
                            using (writer.WithBlock())
                            {
                                writer.Append("throw new global::System.Exception(\"Method not found: ");
                                writer.Append(actualName);
                                writer.AppendLine("\");");
                            }
                        }

                        writer.AppendLine("return field;");
                    }
                }
            }
        }

        static void AppendDelegate(CodeWriter writer, in SourceMethodSignature signature, in ArrayBuilder<SourceParameterInfo> parametersBuilder)
        {
            writer.Append("global::System.").Append(signature.IsVoidReturnType ? "Action<" : "Func<");
            bool hasParameters = AppendParameterTypes(writer, in parametersBuilder);

            if (!signature.IsVoidReturnType)
            {
                if (hasParameters)
                {
                    writer.Append(", ");
                }

                writer.Append(WrapType(signature.ReturnType.ToString()));
            }

            writer.Append(">");
        }
    }

    private static bool AppendParameterTypes(CodeWriter writer, in ArrayBuilder<SourceParameterInfo> parametersBuilder)
    {
        if (parametersBuilder.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < parametersBuilder.Count; i++)
        {
            writer.Append(WrapType(parametersBuilder[i].Type));
            if (i < parametersBuilder.Count - 1)
            {
                writer.Append(", ");
            }
        }

        return true;
    }

    private static bool IsMethodBanned(MethodSource method, in SourceMethodSignature signature, in ArrayBuilder<SourceParameterInfo> parameters)
    {
        if (signature.Name.Contains("OnReturn", StringComparison.Ordinal) && method.Signature.Contains("Hertzole.SourceGen.ArrayBuilder"))
        {
            return true;
        }

        if (signature.IsConstructor && signature.HasRefStruct)
        {
            // Can't create instance with ref types
            return true;
        }

        if (signature.HasRefStruct && (signature.HasOutParameter || signature.HasRefParameter))
        {
            //TODO: Support ref structs with ref/out parameters
            return true;
        }

        if (signature.HasRefStruct)
        {
            // Can't have methods with ref structs and runtime generated types.
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].Type.Contains("Hertzole.SourceGen"))
                {
                    return true;
                }
            }
        }

        return Array.IndexOf(bannedMethods, method.Name) != -1;
    }

    private static string SanitizeMethodName(string methodName)
    {
        ReadOnlySpan<char> span = methodName.AsSpan();

        if (span.Equals("==", StringComparison.Ordinal))
        {
            return "op_Equality";
        }

        if (span.Equals("!=", StringComparison.Ordinal))
        {
            return "op_Inequality";
        }

        return methodName;
    }

    private static void SanitizeExplicitName(ArrayBuilder<char> builder)
    {
        if (builder.AsSpan().StartsWith("global::", StringComparison.Ordinal))
        {
            builder.RemoveRange(0, 8);
        }

        builder.Replace('.', '_');
        builder.Replace('<', '_');
        builder.Replace('>', '_');
    }

    private static void WriteMethodsImplementations(CodeWriter writer, TypeSource type, string typeName)
    {
        if (type.Methods == null || type.Methods.Length == 0)
        {
            return;
        }

        using ArrayBuilder<SourceParameterInfo> paramsArray = new ArrayBuilder<SourceParameterInfo>();
        using ArrayBuilder<char> nameBuilder = new ArrayBuilder<char>();

        foreach (MethodSource method in type.Methods)
        {
            paramsArray.Clear();
            SourceParameterInfo.FromSignature(method.Signature, in paramsArray);

            SourceMethodSignature signature = SourceMethodSignature.FromSignature(method.Signature);
            if (IsMethodBanned(method, in signature, in paramsArray))
            {
                writer.Append("// Skipped ");
                writer.AppendLine(method.Signature);
                continue;
            }

            if (signature.IsOperator)
            {
                writer.Append("// Skipped ").AppendLine(method.Signature);
                //TODO: Implement operators
                continue;
            }

            if (!signature.Accessor.IsEmpty)
            {
                writer.Append("public ");
            }

            if (signature.IsOverride)
            {
                writer.Append("override ");
            }

            if (signature.IsOperator)
            {
                writer.Append("static ");
            }

            if (signature.IsImplicit)
            {
                writer.Append("implicit ");
            }

            if (signature.IsExplicit)
            {
                writer.Append("explicit ");
            }

            if (!signature.IsConstructor && !signature.IsExplicit && !signature.IsImplicit)
            {
                writer.Append(WrapType(signature.ReturnType.ToString())).Append(' ');
            }

            if (signature.IsOperator)
            {
                writer.Append("operator ");
            }

            writer.Append(WrapType(signature.Name.ToString()));
            writer.Append('(');
            if (signature.IsConstructor)
            {
                writer.Append("global::System.Type type");
            }

            for (int i = 0; i < paramsArray.Count; i++)
            {
                if (signature.IsConstructor && i == 0)
                {
                    writer.Append(", ");
                }

                if (paramsArray[i].IsOut)
                {
                    writer.Append("out ");
                }

                if (paramsArray[i].IsRef)
                {
                    writer.Append("ref ");
                }

                writer.Append(WrapType(paramsArray[i].Type));
                writer.Append(' ');
                writer.Append(paramsArray[i].Name);

                if (!string.IsNullOrEmpty(paramsArray[i].DefaultValue))
                {
                    writer.Append(" = ");
                    writer.Append(paramsArray[i].DefaultValue);
                }

                if (i < paramsArray.Count - 1)
                {
                    writer.Append(", ");
                }
            }

            writer.AppendLine(')');
            using (writer.WithBlock())
            {
                if (signature.IsConstructor)
                {
                    writer.AppendLine("Type = type;");

                    if (!signature.IsStatic)
                    {
                        writer.Append("Instance = global::System.Activator.CreateInstance(Type");

                        if (method.ParameterCount > 0)
                        {
                            writer.Append(", [");
                            AppendParameterNames(method);
                            writer.Append(']');
                        }

                        writer.AppendLine(
                            ") ?? throw new  global::System.InvalidOperationException($\"Could not create instance for type '{type.FullName}'\");");
                    }
                }
                else
                {
                    const bool to_instance = true;
                    if (signature.HasRefStruct)
                    {
                        if (!signature.IsVoidReturnType)
                        {
                            writer.Append("return ");
                        }

                        writer.Append(SanitizeMethodName(method.Name));

                        if (signature.IsExplicitImplementation)
                        {
                            nameBuilder.Clear();
                            nameBuilder.AddRange(signature.ReturnType);
                            SanitizeExplicitName(nameBuilder);

                            writer.Append('_').Append(nameBuilder);
                        }

                        if (!string.IsNullOrWhiteSpace(method.ParameterTypesKey))
                        {
                            writer.Append(FormatParameterTypesKey(method.ParameterTypesKey));
                        }

                        writer.Append("_Delegate.Invoke(");
                        AppendParameterNames(method);
                        writer.AppendLine(");");
                    }
                    else
                    {
                        writer.Append("object?[] __callerArgs = [");
                        AppendParameterNames(method, to_instance);
                        writer.AppendLine("];");

                        if (!signature.IsVoidReturnType)
                        {
                            writer.Append("object? __callResult = ");
                        }

                        writer.Append(SanitizeMethodName(method.Name));

                        if (signature.IsExplicitImplementation)
                        {
                            nameBuilder.Clear();
                            nameBuilder.AddRange(signature.ReturnType);
                            SanitizeExplicitName(nameBuilder);

                            writer.Append('_').Append(nameBuilder);
                        }

                        if (!string.IsNullOrWhiteSpace(method.ParameterTypesKey))
                        {
                            writer.Append(FormatParameterTypesKey(method.ParameterTypesKey));
                        }

                        writer.Append("_MethodInfo!.Invoke(");
                        if (!signature.IsStatic)
                        {
                            writer.Append("Instance, ");
                        }
                        else
                        {
                            writer.Append("null, ");
                        }

                        writer.Append("__callerArgs)");

                        if (!signature.IsVoidReturnType)
                        {
                            writer.Append('!');
                        }

                        writer.AppendLine(';');

                        bool hasRefParams = method.Signature.Contains("out") || method.Signature.Contains("ref");
                        if (hasRefParams)
                        {
                            AssignRefParams(writer, paramsArray.ToImmutableArray());
                        }

                        if (!signature.IsVoidReturnType)
                        {
                            writer.Append("return ");
                            if (signature.ReturnType.Contains("Hertzole.SourceGen", StringComparison.Ordinal))
                            {
                                writer.Append("new ").Append(WrapType(signature.ReturnType.ToString())).AppendLine("(__callResult!);");
                            }
                            else
                            {
                                writer.Append('(').Append(WrapType(signature.ReturnType.ToString()));
                                writer.AppendLine(")__callResult;");
                            }
                        }
                    }
                }
            }
        }

        return;

        static void AssignRefParams(CodeWriter writer, ImmutableArray<SourceParameterInfo> parameters)
        {
            writer.AppendLine("// ref");
            for (int i = 0; i < parameters.Length; i++)
            {
                ref readonly SourceParameterInfo param = ref parameters.ItemRef(i);

                if (!param.IsOut && !param.IsRef)
                {
                    continue;
                }

                writer.Append(param.Name);
                writer.Append(" = (");
                writer.Append(param.Type);
                writer.Append(")__callerArgs[");
                writer.Append(i);
                writer.AppendLine("]!;");
            }
        }

        void AppendParameterNames(MethodSource method, bool toInstance = false)
        {
            ReadOnlySpan<char> signature = method.Signature.AsSpan(method.Signature.IndexOf('(') + 1);
            int fail = 0;
            int i = 0;
            while (true)
            {
                bool isRef = false;
                if (signature.StartsWith("this "))
                {
                    signature = signature.Slice(5);
                }
                else if (signature.StartsWith("out ") || signature.StartsWith("ref "))
                {
                    isRef = true;
                    signature = signature.Slice(4);
                }

                int end = signature.IndexOf(',');
                if (end == -1)
                {
                    end = signature.IndexOf(')');
                    if (end == -1)
                    {
                        return;
                    }
                }

                if (i != 0)
                {
                    writer.Append(", ");
                }

                int spaceIndex = signature.IndexOf(' ');
                if (spaceIndex == -1)
                {
                    return;
                }

                ReadOnlySpan<char> slice = signature.Slice(0, end).Slice(spaceIndex).Trim();
                int defaultSpace = slice.IndexOf(' ');
                if (slice.IndexOf(' ') != -1)
                {
                    slice = slice.Slice(0, defaultSpace).Trim();
                }

                bool needsInstance = toInstance && signature.Slice(0, end).Contains("Hertzole.SourceGen", StringComparison.Ordinal);

                if (isRef)
                {
                    writer.Append("null");
                }
                else
                {
                    writer.Append(slice);

                    if (needsInstance)
                    {
                        writer.Append(".Instance");
                    }
                }

                signature = signature.Slice(end + 1).Trim();
                i++;

                fail++;

                if (fail == 1000)
                {
                    Log.Error($"Failed to append parameters. Signature: {method.Signature} | {signature.ToString()}");
                    break;
                }
            }
        }
    }

    private static string GetSignature(TypeSource type)
    {
        return type.Signature.Replace("partial ", string.Empty).Replace("static ", string.Empty).Replace("readonly ", string.Empty)
                   .Replace("private ", "public ");
    }

    private static string FormatParameterTypesKey(string parameterTypesKey)
    {
        return parameterTypesKey.Replace("[]", "Array").Replace(", ", "_").Replace('.', '_').Replace('<', '_').Replace('>', '_');
    }

    private static string GetMethodName(string typeName, MethodSource method)
    {
        if (method.Name == typeName)
        {
            return ".ctor";
        }

        return SanitizeMethodName(method.Name);
    }

    private static string GetBindingFlags(MethodSource method)
    {
        using PoolScope<StringBuilder> _ = StringBuilderPool.Get(out StringBuilder sb);

        ReadOnlySpan<char> signature = method.Signature.AsSpan();

        if (signature.Contains("public", StringComparison.Ordinal))
        {
            AppendFlag("Public");
        }
        else
        {
            AppendFlag("NonPublic");
        }

        sb.Append(" | ");
        if (signature.Contains("static", StringComparison.Ordinal))
        {
            AppendFlag("Static");
        }
        else
        {
            AppendFlag("Instance");
        }

        return sb.ToString();

        void AppendFlag(string flag)
        {
            sb.Append("global::System.Reflection.BindingFlags.");
            sb.Append(flag);
        }
    }

    private static string GetParameters(MethodSource method)
    {
        if (method.ParameterCount == 0)
        {
            return "global::System.Array.Empty<global::System.Type>()";
        }

        using PoolScope<StringBuilder> _ = StringBuilderPool.Get(out StringBuilder sb);

        sb.Append('[');

        string[] parameters = method.ParameterTypesKey.Split(',');
        int customType = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Contains("Hertzole.SourceGen", StringComparison.Ordinal))
            {
                sb.Append("__customType");
                sb.Append(customType);
                customType++;
            }
            else
            {
                sb.Append("typeof(");
                sb.Append(parameters[i].AsSpan().Trim());
                sb.Append(")");
            }

            if (i < parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(']');

        return sb.ToString();
    }

    private static void GetCustomTypes(CodeWriter writer, MethodSource source)
    {
        ReadOnlySpan<char> span = source.ParameterTypesKey.AsSpan();

        if (!span.Contains("Hertzole.SourceGen", StringComparison.Ordinal))
        {
            return;
        }

        // Find all custom types that start with Hertzole.SourceGen
        try
        {
            int i = 0;
            while (true)
            {
                int index = span.IndexOf("Hertzole.SourceGen", StringComparison.Ordinal);
                if (index == -1)
                {
                    break;
                }

                int end = span.Slice(index).IndexOf(',');
                if (end == -1)
                {
                    end = span.Length - index;
                }

                ReadOnlySpan<char> fullSlice = span.Slice(index, end);
                ReadOnlySpan<char> methodNameSlice = fullSlice;

                int genericIndex = fullSlice.IndexOf('<');
                bool isGeneric = genericIndex != -1;
                int genericCount = 0;
                if (isGeneric)
                {
                    ReadOnlySpan<char> genericSlice = fullSlice.Slice(genericIndex);
                    writer.AppendLine($"// {genericSlice.ToString()} | {fullSlice.ToString()}");
                    methodNameSlice = fullSlice.Slice(0, genericIndex);
                    // Very ugly hack, only gets all args in one slice. Should get each one individually.

                    do
                    {
                        genericCount++;
                        genericIndex = genericSlice.IndexOf(',');
                        if (genericIndex != -1)
                        {
                            genericSlice = genericSlice.Slice(genericIndex + 1);
                        }
                    } while (genericIndex != -1);
                }

                writer.Append("// ");
                writer.AppendLine(fullSlice);
                writer.Append("global::System.Type __customType");
                writer.Append(i);
                writer.Append(" = Type.Assembly.GetType(\"");
                writer.Append(methodNameSlice.Trim());

                if (isGeneric && genericCount > 0)
                {
                    writer.Append('`').Append(genericCount);
                }

                writer.Append("\", true)");

                if (isGeneric)
                {
                    writer.Append("!.MakeGenericType(");
                    AppendGenericArgs(writer, fullSlice);
                    writer.Append(")");
                }

                writer.AppendLine("!;");

                i++;

                if (span.Length == index + end)
                {
                    Log.Info("Break!");
                    break;
                }

                span = span.Slice(index + end);
            }
        }

        catch (Exception e)
        {
            writer.AppendLine("// Error: " + e.Message);
            writer.AppendLine("// Span: " + span.ToString());
            Log.Error(e);
        }
    }

    private static void AppendGenericArgs(CodeWriter writer, ReadOnlySpan<char> span)
    {
        Log.Info("Append generic args");

        while (true)
        {
            int index = span.IndexOf('<');
            if (index == -1)
            {
                writer.Append(span);
                return;
            }

            span = span.Slice(index + 1);

            int end = span.IndexOf('>');
            if (end == -1)
            {
                throw new Exception("Generic type not closed!");
            }

            writer.Append("typeof(");
            writer.Append(span.Slice(0, end));
            writer.Append(")");

            span = span.Slice(end + 1);
        }
    }

    private static void AppendGenericArgumentTypes(CodeWriter writer, ReadOnlySpan<char> genericParameters)
    {
        while (true)
        {
            writer.Append("typeof(");

            int comma = genericParameters.IndexOf(',');
            if (comma == -1)
            {
                writer.Append(genericParameters.Trim());
                writer.Append(')');
                return;
            }

            writer.Append(genericParameters.Slice(0, comma).Trim());
            writer.Append("), ");
            genericParameters = genericParameters.Slice(comma + 1);
        }
    }
}