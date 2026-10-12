using System;
using CommunityToolkit.HighPerformance.Helpers;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace SourceGenUtils.Trimmer;

internal readonly struct FindPropertyUsageJob : IRefAction<PropertyUsage>
{
    private readonly AssemblyDefinition assembly;

    private readonly bool log;

    public FindPropertyUsageJob(AssemblyDefinition assembly, bool log = false)
    {
        this.assembly = assembly;
        this.log = log;
    }

    /// <inheritdoc />
    public void Invoke(ref PropertyUsage item)
    {
        if (log)
        {
            Console.WriteLine($"FIND PROPERTY :: {item.Property.FullName} ({item.Property.DeclaringType.FullName}.{item.Property.Name})");
        }

        Collection<TypeDefinition> types = assembly.MainModule.Types;
        for (int i = 0; i < types.Count; i++)
        {
            if (FindUsageInType(types[i], item.Property))
            {
                item.IsUsed = true;
                return;
            }
        }

        item.IsUsed = false;
    }

    private bool FindUsageInType(TypeDefinition type, PropertyDefinition propertyToFind)
    {
        if (propertyToFind.GetMethod != null && FindMethodUsageJob.FindUsageInMethods(type, propertyToFind.GetMethod, log))
        {
            return true;
        }

        if (propertyToFind.SetMethod != null && FindMethodUsageJob.FindUsageInMethods(type, propertyToFind.SetMethod, log))
        {
            return true;
        }

        return false;
    }
}