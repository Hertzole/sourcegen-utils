using System;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

internal readonly struct FindMethodUsageJob : IRefAction<MethodUsage>
{
    private readonly AssemblyDefinition assembly;
    private readonly TypeReference? attributeType;

    private readonly bool log;

    public FindMethodUsageJob(AssemblyDefinition assembly, bool log = false)
    {
        this.assembly = assembly;
        this.log = log;

        this.assembly.MainModule.TryGetTypeReference("System.Attribute", out attributeType);
    }

    /// <inheritdoc />
    public void Invoke(ref MethodUsage item)
    {
        if (log)
        {
            Console.WriteLine($"FIND METHOD :: {item.Method.FullName} ({item.Method.DeclaringType.FullName}.{item.Method.Name})");
        }

        if (item.Method.Name == ".cctor")
        {
            item.IsUsed = true;
            return;
        }

        // Accessors are referenced by Property/Event rows, not IL. Removing them
        // leaves those rows with dangling tokens, which breaks reflection over kept types.
        if (IsPropertyOrEventAccessor(item.Method))
        {
            item.IsUsed = true;
            return;
        }

        if (IsInterfaceImplementation(item.Method))
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
                return;
            }
        }

        item.IsUsed = false;
    }

    private static bool IsPropertyOrEventAccessor(MethodDefinition method)
    {
        TypeDefinition type = method.DeclaringType;

        if (type.HasProperties)
        {
            Collection<PropertyDefinition> properties = type.Properties;
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].GetMethod == method || properties[i].SetMethod == method)
                {
                    return true;
                }
            }
        }

        if (type.HasEvents)
        {
            Collection<EventDefinition> events = type.Events;
            for (int i = 0; i < events.Count; i++)
            {
                EventDefinition evt = events[i];
                if (evt.AddMethod == method || evt.RemoveMethod == method)
                {
                    return true;
                }

                if (evt.HasOtherMethods)
                {
                    Collection<MethodDefinition> otherMethods = evt.OtherMethods;
                    for (int j = 0; j < otherMethods.Count; j++)
                    {
                        if (otherMethods[j] == method)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool IsInterfaceImplementation(MethodDefinition method)
    {
        for (int i = 0; i < method.Overrides.Count; i++)
        {
            MethodReference overridden = method.Overrides[i];
            if (overridden.DeclaringType != null && overridden.DeclaringType.LockedResolve() is { IsInterface: true })
            {
                if (log)
                {
                    Console.WriteLine(
                        $"FIND METHOD :: Implemented | '{method.FullName}' found as an implementation of '{overridden.DeclaringType.FullName}' in type '{method.DeclaringType.FullName}'");
                }

                return true;
            }
        }

        if (!method.IsNewSlot || !method.IsVirtual)
        {
            return false;
        }

        if (!method.DeclaringType.HasInterfaces)
        {
            return false;
        }

        Collection<InterfaceImplementation> interfaces = method.DeclaringType.Interfaces;
        for (int i = 0; i < interfaces.Count; i++)
        {
            TypeDefinition resolved = interfaces[i].InterfaceType.LockedResolve();
            if (!resolved.HasMethods)
            {
                continue;
            }

            for (int j = 0; j < resolved.Methods.Count; j++)
            {
                MethodDefinition interfaceMethod = resolved.Methods[j];

                if (interfaceMethod.Name == method.Name && interfaceMethod.Parameters.Count == method.Parameters.Count)
                {
                    if (log)
                    {
                        Console.WriteLine(
                            $"FIND METHOD :: Implemented | '{method.FullName}' found as an implementation of '{resolved.FullName}' in type '{method.DeclaringType.FullName}'");
                    }

                    return true;
                }
            }
        }

        return false;
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
                if (log)
                {
                    Console.WriteLine(
                        $"FOUND METHOD :: Assembly | '{methodToFind.FullName}' found on assembly attribute '{attributes[i].AttributeType.FullName}'");
                }

                return true;
            }
        }

        return false;
    }

    private bool FindUsageInMethods(TypeDefinition type, MethodDefinition methodToFind)
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

    private bool FindUsageInMethod(MethodDefinition targetMethod, MethodDefinition methodToFind)
    {
        Collection<Instruction> il = targetMethod.Body.Instructions;
        for (int j = 0; j < il.Count; j++)
        {
            Instruction i = il[j];

            switch (i.Operand)
            {
                case MethodReference mr when mr == methodToFind || mr == methodToFind.GetOriginalBaseMethod():
                case MethodDefinition md when md == methodToFind || md == methodToFind.GetOriginalBaseMethod():
                case MethodReference mr2 when mr2.LockedResolve() == methodToFind || mr2.LockedResolve() == methodToFind.GetOriginalBaseMethod():
                    LogFound(log, i);
                    return true;
                case MethodReference mr when mr.DeclaringType.IsGenericInstance && mr.LockedResolve() == methodToFind:
                case MethodDefinition md when md.DeclaringType.IsGenericInstance && md.LockedResolve() == methodToFind:
                    LogFound(log, i);
                    return true;
            }
        }

        return false;

        void LogFound(bool shouldLog, Instruction instruction)
        {
            if (shouldLog)
            {
                Console.WriteLine(
                    $"FIND METHOD :: Method | '{methodToFind.FullName}' found in method '{targetMethod.FullName}' in type '{targetMethod.DeclaringType.FullName}' | Instruction: {instruction}");
            }
        }
    }
}