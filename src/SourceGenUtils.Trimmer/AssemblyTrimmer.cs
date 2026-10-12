using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

public sealed class AssemblyTrimmer
{
    public bool RemoveGeneratedCodeAttribute { get; set; } = true;
    public bool RemoveCodeCoverageAttribute { get; set; } = true;

    public bool Trim(string assemblyPath, bool log = false)
    {
        if (!File.Exists(assemblyPath))
        {
            return false;
        }

        string tmpPath = Path.GetTempFileName();
        try
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters(ReadingMode.Immediate)))
            {
                List<TypeDefinition> sourceGenTypes = new List<TypeDefinition>();

                RemoveAttributes(assembly);

                bool dirty = true;
                int count = 0;
                while (dirty)
                {
                    dirty = false;

                    if (log)
                    {
                        Console.WriteLine($"=== ITERATION {count} ===");
                    }

                    GetSourceGenTypes(assembly, sourceGenTypes);
                    dirty |= RemoveUnusedMethodUsages(sourceGenTypes, assembly, RemoveGeneratedCodeAttribute, RemoveCodeCoverageAttribute, log);
                    dirty |= RemoveUnusedTypes(sourceGenTypes, assembly, RemoveGeneratedCodeAttribute, RemoveCodeCoverageAttribute, log);
                    count++;

                    if (count >= 512)
                    {
                        return false;
                    }
                }

                assembly.Write(tmpPath);
            }

            File.Delete(assemblyPath);
            File.Move(tmpPath, assemblyPath);

            return true;
        }
        finally
        {
            // Clean up
            if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
            }
        }
    }

    private static void RemoveAttributes(AssemblyDefinition assembly)
    {
        if (!assembly.HasCustomAttributes)
        {
            return;
        }

        RemoveAttribute("Hertzole.SourceGen.EnableRecordSupportAttribute");
        RemoveAttribute("Hertzole.SourceGen.EnableRequiredSupportAttribute");

        void RemoveAttribute(string fullName)
        {
            Collection<CustomAttribute> attributes = assembly.CustomAttributes;
            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                if (attributes[i].AttributeType.FullName == fullName)
                {
                    attributes.RemoveAt(i);
                }
            }
        }
    }

    private static bool RemoveUnusedMethodUsages(IReadOnlyList<TypeDefinition> sourceGenTypes,
        AssemblyDefinition assembly,
        bool removeGeneratedCodeAttribute,
        bool removeCodeCoverageAttribute,
        bool log)
    {
        MethodUsage[] methodUsages = sourceGenTypes.SelectMany(x => x.Methods).Select(x => new MethodUsage(true, x)).ToArray();
        FindMethodUsageJob methodJob = new FindMethodUsageJob(assembly, log);
        ParallelHelper.ForEach<MethodUsage, FindMethodUsageJob>(methodUsages, in methodJob);

        bool dirty = false;

        for (int i = 0; i < methodUsages.Length; i++)
        {
            if (methodUsages[i].IsUsed)
            {
                if (RemoveOptionalAttributes(methodUsages[i].Method, removeGeneratedCodeAttribute, removeCodeCoverageAttribute))
                {
                    if (log)
                    {
                        Console.WriteLine($"REMOVE METHODS :: {methodUsages[i].Method.FullName} is used, but removed optional attributes.");
                    }

                    dirty = true;
                }

                continue;
            }

            if (log)
            {
                Console.WriteLine($"REMOVE METHODS :: {methodUsages[i].Method.DeclaringType.FullName}.{methodUsages[i].Method.Name} is unused.");
            }

            methodUsages[i].Method.DeclaringType.Methods.Remove(methodUsages[i].Method);
            dirty = true;
        }

        return dirty;
    }

    private static bool RemoveUnusedTypes(IReadOnlyList<TypeDefinition> sourceGenTypes,
        AssemblyDefinition assembly,
        bool removeGeneratedCodeAttribute,
        bool removeCodeCoverageAttribute,
        bool log)
    {
        TypeUsage[] usages = sourceGenTypes.Select(static x => new TypeUsage(true, x)).ToArray();
        FindTypeUsageJob typeJob = new FindTypeUsageJob(assembly, log);
        ParallelHelper.ForEach<TypeUsage, FindTypeUsageJob>(usages, in typeJob);

        bool dirty = false;

        for (int i = 0; i < usages.Length; i++)
        {
            if (usages[i].IsUsed)
            {
                if (RemoveOptionalAttributes(usages[i].Type, removeGeneratedCodeAttribute, removeCodeCoverageAttribute))
                {
                    dirty = true;
                    if (log)
                    {
                        Console.WriteLine($"REMOVE TYPES :: {usages[i].Type.FullName} is used, but removed optional attributes.");
                    }
                }

                continue;
            }

            if (usages[i].Type.DeclaringType != null)
            {
                // Nested type
                usages[i].Type.DeclaringType.NestedTypes.Remove(usages[i].Type);
            }
            else
            {
                assembly.MainModule.Types.Remove(usages[i].Type);
            }

            if (log)
            {
                Console.WriteLine($"REMOVE TYPES :: {usages[i].Type.FullName} is unused.");
            }

            dirty = true;
        }

        return dirty;
    }

    private static bool RemoveOptionalAttributes(ICustomAttributeProvider type, bool removeGeneratedCodeAttribute, bool removeCodeCoverageAttribute)
    {
        if (!type.HasCustomAttributes || !removeGeneratedCodeAttribute && !removeCodeCoverageAttribute)
        {
            return false;
        }

        bool dirty = false;

        Collection<CustomAttribute> attributes = type.CustomAttributes;
        for (int i = attributes.Count - 1; i >= 0; i--)
        {
            if (removeGeneratedCodeAttribute && attributes[i].AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute")
            {
                attributes.RemoveAt(i);
                dirty = true;
                continue;
            }

            if (removeCodeCoverageAttribute && attributes[i].AttributeType.FullName == "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute")
            {
                attributes.RemoveAt(i);
                dirty = true;
            }
        }

        return dirty;
    }

    private static void GetSourceGenTypes(AssemblyDefinition assembly, List<TypeDefinition> result)
    {
        result.Clear();

        foreach (TypeDefinition type in assembly.MainModule.Types)
        {
            if (type.Namespace != "Hertzole.SourceGen")
            {
                continue;
            }

            AddType(type);
        }

        return;

        void AddType(TypeDefinition type)
        {
            result.Add(type);

            if (type.HasNestedTypes)
            {
                foreach (TypeDefinition nestedType in type.NestedTypes)
                {
                    AddType(nestedType);
                }
            }
        }
    }
}