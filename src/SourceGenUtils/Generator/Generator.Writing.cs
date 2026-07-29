using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    internal static void AppendTrivia(CodeWriter writer, TriviaSource? trivia, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (trivia == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(trivia.Summary))
        {
            writer.AppendLine("/// <summary>");
            writer.Append("/// ");
            writer.AppendLine(trivia.Summary!);
            writer.AppendLine("/// </summary>");
        }

        if (!string.IsNullOrWhiteSpace(trivia.Remarks))
        {
            writer.AppendLine("/// <remarks>");
            writer.Append("/// ");
            writer.AppendLine(trivia.Remarks!);
            writer.AppendLine("/// </remarks>");
        }

        if (trivia.Parameters != null && trivia.Parameters.Count > 0)
        {
            foreach (KeyValuePair<string, string> valuePair in trivia.Parameters)
            {
                writer.Append("/// <param name=\"");
                writer.Append(valuePair.Key);
                writer.Append("\">");
                writer.Append(valuePair.Value);
                writer.AppendLine("</param>");
            }
        }

        if (!string.IsNullOrWhiteSpace(trivia.Returns))
        {
            writer.Append("/// <returns>");
            writer.Append(trivia.Returns!);
            writer.AppendLine("</returns>");
        }
    }

    internal static void AppendShellType(CodeWriter writer, TypeSource type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool typeHasConditionalSymbol = !string.IsNullOrWhiteSpace(type.ConditionalPreprocessorSymbol);

        if (typeHasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(type.ConditionalPreprocessorSymbol!);
        }

        AppendTrivia(writer, type.Trivia, cancellationToken);
        writer.AppendEmbeddedAttribute();
        writer.AppendLine(type.Signature);

        using (writer.WithBlock())
        {
            if (type.Methods != null && type.Methods.Length > 0)
            {
                for (int i = 0; i < type.Methods.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (type.Methods[i].SkipPartial)
                    {
                        continue;
                    }

                    bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(type.Methods[i].ConditionalPreprocessorSymbol);

                    if (hasConditionalSymbol)
                    {
                        writer.AppendConditionalSymbol(type.Methods[i].ConditionalPreprocessorSymbol!);
                    }

                    AppendTrivia(writer, type.Methods[i].Trivia, cancellationToken);
                    writer.Append(type.Methods[i].Signature);
                    writer.AppendLine(";");

                    if (hasConditionalSymbol)
                    {
                        writer.AppendPreprocessorSymbol("#endif");
                    }

                    if (i < type.Methods.Length - 1)
                    {
                        writer.AppendLine();
                    }
                }
            }

            if (type.Types != null && type.Types.Count > 0)
            {
                foreach (KeyValuePair<string, TypeSource> typeKvp in type.Types)
                {
                    writer.AppendLine();

                    AppendShellType(writer, typeKvp.Value, cancellationToken);
                }
            }
        }

        if (typeHasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    internal static void AppendType(TypeSource typeSource, string typeName, CodeWriter writer, in ImplementationContext implementationContext)
    {
        MethodSource[]? methods = typeSource.Methods;

        bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(typeSource.ConditionalPreprocessorSymbol);

        if (hasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(typeSource.ConditionalPreprocessorSymbol!);
        }

        writer.AppendGeneratedCodeAttribute(generatorName, generatorVersion);
        writer.AppendExcludeFromCodeCoverageAttribute();

        WriteAttributes(typeSource, writer, in implementationContext);

        writer.AppendLine(typeSource.Signature);
        writer.AppendLine("{");
        writer.Indent++;

        bool needsSpace = false;

        if (typeSource.Fields != null)
        {
            bool writtenAnyFields = false;

            foreach (FieldSource field in typeSource.Fields.Values)
            {
                if (WriteFieldOrProperty(field, writer, in implementationContext))
                {
                    writtenAnyFields = true;
                }
            }

            if (writtenAnyFields)
            {
                needsSpace = true;
            }
        }

        if (typeSource.Properties != null)
        {
            if (needsSpace)
            {
                writer.AppendLine();
                needsSpace = false;
            }

            bool writtenAnyProperties = false;

            foreach (PropertySource prop in typeSource.Properties.Values)
            {
                if (WriteFieldOrProperty(prop, writer, in implementationContext))
                {
                    writtenAnyProperties = true;
                }
            }

            if (writtenAnyProperties)
            {
                needsSpace = true;
            }
        }

        if (methods != null && methods.Length > 0)
        {
            if (needsSpace)
            {
                writer.AppendLine();
                needsSpace = false;
            }

            bool firstMethod = true;

            foreach (MethodSource method in methods)
            {
                implementationContext.CancellationToken.ThrowIfCancellationRequested();

                if (!firstMethod)
                {
                    writer.AppendLine();
                }

                firstMethod = false;

                string fullName = $"{typeName}.{method.Name}({method.ParameterTypesKey})";

                AppendMethod(writer, method, fullName, in implementationContext);

                needsSpace = true;
            }
        }

        if (typeSource.Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> typeKvp in typeSource.Types)
            {
                if (needsSpace)
                {
                    writer.AppendLine();
                }

                implementationContext.CancellationToken.ThrowIfCancellationRequested();
                AppendType(typeKvp.Value, $"{typeName}.{typeKvp.Key}", writer, in implementationContext);
                needsSpace = true;
            }
        }

        writer.Indent--;
        writer.AppendLine("}");

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    internal static void AppendMethod(CodeWriter writer, MethodSource method, string fullName, in ImplementationContext context)
    {
        bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(method.ConditionalPreprocessorSymbol);

        if (hasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(method.ConditionalPreprocessorSymbol);
        }

        WriteAttributes(method, writer, in context);
        writer.AppendLine(ClearSignatureFromDefaults(method.Signature));

        using (writer.WithBlock())
        {
            if (method.AlwaysWrite || context.HasCalledMethod(fullName) && AreAllDependenciesMet(method.RequiredDependencies, in context))
            {
                method.Implementation.Invoke(writer, in context);
            }
            else
            {
                writer.AppendConditionalSymbol("DEBUG");
                writer.Append("throw new global::System.NotImplementedException(\"");
                writer.Append(fullName);
                writer.AppendLine(" has not been properly emitted. This is a bug!\");");

                if (!string.IsNullOrWhiteSpace(method.EmptyStub))
                {
                    writer.AppendPreprocessorSymbol("else");
                    writer.AppendLine(method.EmptyStub);
                }

                writer.AppendPreprocessorSymbol("endif");
            }
        }

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    private static string ClearSignatureFromDefaults(string signature)
    {
        ReadOnlySpan<char> span = signature.AsSpan();

        int equalsIndex = span.IndexOf("= ");

        if (equalsIndex == -1)
        {
            return signature;
        }

        //TODO: Pool
        //PERF: Use spans?
        StringBuilder sb = new StringBuilder(signature);
        sb.Replace(" = null", string.Empty).Replace(" = default", string.Empty);
        return sb.ToString();
    }

    internal static bool WriteFieldOrProperty(BaseSource source, CodeWriter writer, in ImplementationContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (AreAnyDependenciesMet(source.Dependencies, in context) &&
            AreAllDependenciesMet(source.RequiredDependencies, in context))
        {
            bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(source.ConditionalPreprocessorSymbol);

            if (hasConditionalSymbol)
            {
                writer.AppendConditionalSymbol(source.ConditionalPreprocessorSymbol);
            }

            AppendTrivia(writer, source.Trivia, context.CancellationToken);
            WriteAttributes(source, writer, in context);

            if (source is PropertySource property)
            {
                writer.Append(source.Signature);

                if (property.GetImplementation == null && property.SetImplementation == null)
                {
                    writer.AppendLine(" { get; set; }");
                }
                else
                {
                    writer.AppendLine();

                    using (writer.WithBlock())
                    {
                        if (property.GetImplementation != null)
                        {
                            WriteAttributes(property.GetAttributes, writer, in context);
                            writer.AppendLine("get");

                            using (writer.WithBlock())
                            {
                                property.GetImplementation.Invoke(writer, in context);
                            }
                        }

                        if (property.SetImplementation != null)
                        {
                            WriteAttributes(property.SetAttributes, writer, in context);
                            writer.AppendLine("set");

                            using (writer.WithBlock())
                            {
                                property.SetImplementation.Invoke(writer, in context);
                            }
                        }
                    }
                }
            }
            else
            {
                writer.AppendLine(source.Signature);
            }

            if (hasConditionalSymbol)
            {
                writer.AppendPreprocessorSymbol("#endif");
            }

            return true;
        }

        return false;
    }

    private static void WriteAttributes(IHasAttributes source, CodeWriter writer, in ImplementationContext context)
    {
        WriteAttributes(source.Attributes, writer, in context);
    }

    private static void WriteAttributes(string[]? attributes, CodeWriter writer, in ImplementationContext context)
    {
        if (attributes == null || attributes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < attributes.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            writer.Append('[');
            writer.Append(attributes[i]);
            writer.AppendLine("]");
        }
    }
}