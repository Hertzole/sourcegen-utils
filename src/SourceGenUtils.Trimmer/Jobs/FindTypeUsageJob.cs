using System;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

internal readonly struct FindTypeUsageJob : IRefAction<TypeUsage>
{
    private readonly AssemblyDefinition assembly;
    private readonly TypeReference? attributeType;

    private readonly bool log;

    public FindTypeUsageJob(AssemblyDefinition assembly, bool log = false)
    {
        this.assembly = assembly;
        this.log = log;

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
                if (log)
                {
                    Console.WriteLine(
                        $"FOUND TYPE :: Assembly | '{typeToFind.FullName}' found on assembly attribute '{attributes[i].AttributeType.FullName}'");
                }

                return true;
            }
        }

        return false;
    }

    private bool FindUsageInMethods(TypeDefinition type, TypeDefinition typeToFind)
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

    private bool FindUsageInMethod(MethodDefinition method, TypeDefinition typeToFind)
    {
        if (method.ReturnType == typeToFind || method.ReturnType.LockedResolve() == typeToFind)
        {
            if (log)
            {
                Console.WriteLine(
                    $"FIND TYPE :: Method | '{typeToFind.FullName}' found in method return type '{method.FullName}' in type '{method.DeclaringType.FullName}'");
            }

            return true;
        }

        Collection<VariableDefinition> variables = method.Body.Variables;
        for (int i = 0; i < variables.Count; i++)
        {
            if (variables[i].VariableType == typeToFind)
            {
                if (log)
                {
                    Console.WriteLine(
                        $"FIND TYPE :: Method | '{typeToFind.FullName}' found in method '{method.FullName}' variable in type '{method.DeclaringType.FullName}'");
                }

                return true;
            }
        }

        Collection<Instruction> il = method.Body.Instructions;
        for (int j = 0; j < il.Count; j++)
        {
            Instruction i = il[j];

            switch (i.Operand)
            {
                case MethodReference mr when mr.DeclaringType == typeToFind:
                case MethodDefinition md when md.DeclaringType == typeToFind:
                case TypeDefinition td when td == typeToFind:
                case TypeReference tr when tr == typeToFind:
                case FieldDefinition fd when fd.FieldType == typeToFind:
                    LogFoundInReference(log);
                    return true;
                case MethodReference mr when mr.DeclaringType.IsGenericInstance && mr.DeclaringType.LockedResolve() == typeToFind:
                case TypeReference tr when tr.IsGenericInstance && tr.LockedResolve() == typeToFind:
                case FieldDefinition fd when fd.FieldType.IsGenericInstance && fd.FieldType.LockedResolve() == typeToFind:
                    LogFoundInReference(log);
                    return true;
            }
        }

        return false;

        void LogFoundInReference(bool shouldLog)
        {
            if (shouldLog)
            {
                Console.WriteLine(
                    $"FIND TYPE :: Method | '{typeToFind.FullName}' found in method '{method.FullName}' in type '{method.DeclaringType.FullName}'");
            }
        }
    }
}