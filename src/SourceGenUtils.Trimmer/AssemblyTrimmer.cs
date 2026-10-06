using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

public sealed class AssemblyTrimmer
{
    private static readonly object resolveLock = new object();

    public bool RemoveGeneratedCodeAttribute { get; set; } = true;
    public bool RemoveCodeCoverageAttribute { get; set; } = true;

    private static TypeDefinition LockedResolve(TypeReference reference)
    {
        lock (resolveLock)
        {
            return reference.Resolve();
        }
    }

    public bool Trim(string assemblyPath)
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

                bool dirtyMethods = true;
                bool dirtyTypes = true;
                int count = 0;
                while (dirtyMethods || dirtyTypes)
                {
                    Console.WriteLine($"=== ITERATION {count} ===");
                    GetSourceGenTypes(assembly, sourceGenTypes);
                    dirtyMethods = RemoveUnusedMethodUsages(sourceGenTypes, assembly, RemoveGeneratedCodeAttribute, RemoveCodeCoverageAttribute);
                    dirtyTypes = RemoveUnusedTypes(sourceGenTypes, assembly, RemoveGeneratedCodeAttribute, RemoveCodeCoverageAttribute);
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
        bool removeCodeCoverageAttribute)
    {
        MethodUsage[] methodUsages = sourceGenTypes.SelectMany(x => x.Methods).Select(x => new MethodUsage(true, x)).ToArray();
        FindMethodUsageJob methodJob = new FindMethodUsageJob(assembly);
        ParallelHelper.ForEach<MethodUsage, FindMethodUsageJob>(methodUsages, in methodJob);

        bool dirty = false;

        for (int i = 0; i < methodUsages.Length; i++)
        {
            if (methodUsages[i].IsUsed)
            {
                if (RemoveOptionalAttributes(methodUsages[i].Method, removeGeneratedCodeAttribute, removeCodeCoverageAttribute))
                {
                    dirty = true;
                }

                continue;
            }

            methodUsages[i].Method.DeclaringType.Methods.Remove(methodUsages[i].Method);
            dirty = true;
        }

        return dirty;
    }

    private static bool RemoveUnusedTypes(IReadOnlyList<TypeDefinition> sourceGenTypes,
        AssemblyDefinition assembly,
        bool removeGeneratedCodeAttribute,
        bool removeCodeCoverageAttribute)
    {
        TypeUsage[] usages = sourceGenTypes.Select(static x => new TypeUsage(true, x)).ToArray();
        FindTypeUsageJob typeJob = new FindTypeUsageJob(assembly);
        ParallelHelper.ForEach<TypeUsage, FindTypeUsageJob>(usages, in typeJob);

        bool dirty = false;

        for (int i = 0; i < usages.Length; i++)
        {
            if (usages[i].IsUsed)
            {
                if (RemoveOptionalAttributes(usages[i].Type, removeGeneratedCodeAttribute, removeCodeCoverageAttribute))
                {
                    dirty = true;
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

    private record struct MethodUsage(bool IsUsed, MethodDefinition Method);

    private record struct TypeUsage(bool IsUsed, TypeDefinition Type);

    private readonly struct FindMethodUsageJob : IRefAction<MethodUsage>
    {
        private readonly AssemblyDefinition assembly;
        private readonly TypeReference? attributeType;

        public FindMethodUsageJob(AssemblyDefinition assembly)
        {
            this.assembly = assembly;

            this.assembly.MainModule.TryGetTypeReference("System.Attribute", out attributeType);
        }

        /// <inheritdoc />
        public void Invoke(ref MethodUsage item)
        {
            if (item.Method.Name == ".cctor")
            {
                item.IsUsed = true;
                return;
            }

            Collection<TypeDefinition> types = assembly.MainModule.Types;
            for (int i = 0; i < types.Count; i++)
            {
                if (FindUsageInType(types[i], item.Method))
                {
                    item.IsUsed = true;
                    Console.WriteLine($"METHOD :: {item.Method.FullName} | {types[i].FullName}");
                    return;
                }
            }

            item.IsUsed = false;
        }

        private bool FindUsageInType(TypeDefinition type, MethodDefinition methodToFind)
        {
            if (attributeType != null && type.BaseType == attributeType)
            {
                if (FindUsageOnAssembly(methodToFind))
                {
                    return true;
                }
            }

            if (FindUsageInMethods(type, methodToFind))
            {
                return true;
            }

            if (type.HasNestedTypes)
            {
                Collection<TypeDefinition> nestedTypes = type.NestedTypes;
                for (int i = 0; i < nestedTypes.Count; i++)
                {
                    if (FindUsageInType(nestedTypes[i], methodToFind))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool FindUsageOnAssembly(MethodDefinition methodToFind)
        {
            if (!assembly.HasCustomAttributes)
            {
                return false;
            }

            Collection<CustomAttribute> attributes = assembly.CustomAttributes;
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].Constructor == methodToFind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FindUsageInMethods(TypeDefinition type, MethodDefinition methodToFind)
        {
            if (!type.HasMethods)
            {
                return false;
            }

            Collection<MethodDefinition> methods = type.Methods;
            for (int i = 0; i < methods.Count; i++)
            {
                if (FindUsageInMethod(methods[i], methodToFind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FindUsageInMethod(MethodDefinition targetMethod, MethodDefinition methodToFind)
        {
            Collection<Instruction> il = targetMethod.Body.Instructions;
            for (int j = 0; j < il.Count; j++)
            {
                Instruction i = il[j];

                switch (i.Operand)
                {
                    case MethodReference mr when mr == methodToFind:
                    case MethodDefinition md when md == methodToFind:
                        return true;
                    case MethodReference mr when mr.DeclaringType.IsGenericInstance && LockedResolve(mr.DeclaringType) == methodToFind.DeclaringType:
                    case MethodDefinition md when md.DeclaringType.IsGenericInstance && LockedResolve(md.DeclaringType) == methodToFind.DeclaringType:
                        return true;
                }
            }

            return false;
        }
    }

    private readonly struct FindTypeUsageJob : IRefAction<TypeUsage>
    {
        private readonly AssemblyDefinition assembly;
        private readonly TypeReference? attributeType;

        public FindTypeUsageJob(AssemblyDefinition assembly)
        {
            this.assembly = assembly;

            this.assembly.MainModule.TryGetTypeReference("System.Attribute", out attributeType);
        }

        /// <inheritdoc />
        public void Invoke(ref TypeUsage item)
        {
            Collection<TypeDefinition> types = assembly.MainModule.Types;
            for (int i = 0; i < types.Count; i++)
            {
                if (FindUsageInType(types[i], item.Type))
                {
                    Console.WriteLine($"TYPE :: {item.Type.FullName} | {types[i].FullName}");
                    item.IsUsed = true;
                    return;
                }
            }

            item.IsUsed = false;
        }

        private bool FindUsageInType(TypeDefinition type, TypeDefinition typeToFind)
        {
            if (type == typeToFind)
            {
                // Types shouldn't use themselves.
                return false;
            }

            if (attributeType != null && type.BaseType == attributeType)
            {
                if (FindUsageOnAssembly(typeToFind))
                {
                    return true;
                }
            }

            if (FindUsageInMethods(type, typeToFind))
            {
                return true;
            }

            if (type.HasNestedTypes)
            {
                Collection<TypeDefinition> nestedTypes = type.NestedTypes;
                for (int i = 0; i < nestedTypes.Count; i++)
                {
                    if (FindUsageInType(nestedTypes[i], typeToFind))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool FindUsageOnAssembly(TypeDefinition typeToFind)
        {
            if (!assembly.HasCustomAttributes)
            {
                return false;
            }

            Collection<CustomAttribute> attributes = assembly.CustomAttributes;
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].AttributeType == typeToFind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FindUsageInMethods(TypeDefinition type, TypeDefinition typeToFind)
        {
            if (!type.HasMethods)
            {
                return false;
            }

            Collection<MethodDefinition> methods = type.Methods;

            for (int i = 0; i < methods.Count; i++)
            {
                if (FindUsageInMethod(methods[i], typeToFind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FindUsageInMethod(MethodDefinition method, TypeDefinition typeToFind)
        {
            Collection<VariableDefinition> variables = method.Body.Variables;
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].VariableType == typeToFind)
                {
                    return true;
                }
            }

            Collection<Instruction> il = method.Body.Instructions;
            for (int j = 0; j < il.Count; j++)
            {
                Instruction i = il[j];

                if (i.OpCode == OpCodes.Stsfld)
                {
                    Console.WriteLine($"SET STATIC: {i.Operand} | {i.Operand.GetType()}");
                }

                switch (i.Operand)
                {
                    case MethodReference mr when mr.DeclaringType == typeToFind:
                    case MethodDefinition md when md.DeclaringType == typeToFind:
                    case TypeDefinition td when td == typeToFind:
                    case TypeReference tr when tr == typeToFind:
                    case FieldDefinition fd when fd.FieldType == typeToFind:
                        return true;
                    case MethodReference mr when mr.DeclaringType.IsGenericInstance && LockedResolve(mr.DeclaringType) == typeToFind:
                    case TypeReference tr when tr.IsGenericInstance && LockedResolve(tr) == typeToFind:
                    case FieldDefinition fd when fd.FieldType.IsGenericInstance && LockedResolve(fd.FieldType) == typeToFind:
                        return true;
                }
            }

            return false;
        }
    }
}