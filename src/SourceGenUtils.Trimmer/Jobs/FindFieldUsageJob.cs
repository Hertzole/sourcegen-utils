using System;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

internal readonly struct FindFieldUsageJob : IRefAction<FieldUsage>
{
    private readonly AssemblyDefinition assembly;
    private readonly bool log;

    public FindFieldUsageJob(AssemblyDefinition assembly, bool log = false)
    {
        this.assembly = assembly;
        this.log = log;
    }

    /// <inheritdoc />
    public void Invoke(ref FieldUsage item)
    {
        if (log)
        {
            Console.WriteLine($"FIND FIELD :: {item.Field.GetActualFullName()}");
        }

        Collection<TypeDefinition> types = assembly.MainModule.Types;
        for (int i = 0; i < types.Count; i++)
        {
            if (FindUsageInType(types[i], item.Field))
            {
                item.IsUsed = true;
                return;
            }
        }

        item.IsUsed = false;
    }

    private bool FindUsageInType(TypeDefinition type, FieldDefinition fieldToFind)
    {
        if (FindUsageInMethods(type, fieldToFind))
        {
            return true;
        }

        if (type.HasNestedTypes)
        {
            Collection<TypeDefinition> nestedTypes = type.NestedTypes;
            for (int i = 0; i < nestedTypes.Count; i++)
            {
                if (FindUsageInType(nestedTypes[i], fieldToFind))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool FindUsageInMethods(TypeDefinition type, FieldDefinition fieldToFind)
    {
        if (!type.HasMethods)
        {
            return false;
        }

        Collection<MethodDefinition> methods = type.Methods;
        for (int i = 0; i < methods.Count; i++)
        {
            if (FindUsageInMethod(methods[i], fieldToFind))
            {
                return true;
            }
        }

        return false;
    }

    private bool FindUsageInMethod(MethodDefinition method, FieldDefinition fieldToFind)
    {
        Collection<Instruction> instructions = method.Body.Instructions;
        for (int j = 0; j < instructions.Count; j++)
        {
            Instruction i = instructions[j];

            switch (i.Operand)
            {
                case FieldDefinition fd when fd == fieldToFind:
                case FieldReference fr when fr.LockedResolve() == fieldToFind:
                    if (log)
                    {
                        Console.WriteLine(
                            $"FIND FIELD :: Method | '{fieldToFind.GetActualFullName()}' found in method '{method.GetActualFullName()}' in type '{method.DeclaringType.GetActualFullName()}' | Instruction: {i}");
                    }

                    return true;
            }
        }

        return false;
    }
}