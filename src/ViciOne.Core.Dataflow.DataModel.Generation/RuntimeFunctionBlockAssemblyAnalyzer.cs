using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;
using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Core.Dataflow.DataModel.Generation;

public static class RuntimeFunctionBlockAssemblyAnalyzer
{
    private static IEnumerable<Type> GetAvailableTypes(IEnumerable<Assembly> assemblies)
        => assemblies
            // collect all types
            .SelectMany(s => s.DefinedTypes)
            // narrow down to the usable ones
            .Where(x => x.IsPublic && x.IsClass && x.IsAbstract == false)
            // pick only the types that are decorated with the attribute we are looking for
            .Where(p => Attribute.IsDefined(p, typeof(FunctionBlockDesignAttribute)));

    private static IAggregatingPooling? GetDefaultAggregatingPooling(MemberInfo info, PoolingAttributeTarget target)
    {
        DefaultAggregatingPoolingAttribute? attribute = info.GetCustomAttributes<DefaultAggregatingPoolingAttribute>().FirstOrDefault(x => x.Target == target);
        return attribute?.Pooling;
    }

    private static uint? GetDefaultCycleFrequency(MemberInfo info)
    {
        DefaultCycleFrequencyAttribute? attribute = info.GetCustomAttribute<DefaultCycleFrequencyAttribute>();
        return attribute?.CycleFrequency;
    }

    private static PoolingStrategy? GetDefaultPoolingStrategy(MemberInfo info, PoolingAttributeTarget target)
    {
        DefaultPoolingStrategyAttribute? attribute = info.GetCustomAttributes<DefaultPoolingStrategyAttribute>().FirstOrDefault(x => x.Target == target);
        return attribute?.Strategy;
    }

    private static RunMode? GetDefaultRunMode(MemberInfo info)
    {
        DefaultRunModeAttribute? attribute = info.GetCustomAttribute<DefaultRunModeAttribute>();
        return attribute?.RunMode;
    }

    private static object? GetDefaultValue(MemberInfo info)
    {
        DefaultValueAttribute? attribute = info.GetCustomAttribute<DefaultValueAttribute>();
        return attribute?.Value;
    }

    public static FunctionBlockDesignInfo GetFunctionBlockDesign(Type type)
    {
        var blockInfo = new FunctionBlockDesignInfo();
        (string? @namespace, string? name, string? runtimeTypeName) = GetNamespaceNameAndRuntimeTypeName(type);
        blockInfo.Namespace = @namespace;
        blockInfo.Name = name;
        blockInfo.RuntimeTypeName = runtimeTypeName;
        blockInfo.DefaultCycleFrequency = GetDefaultCycleFrequency(type);
        blockInfo.ValidRunModes = GetValidRunModes(type);
        blockInfo.DefaultRunMode = GetDefaultRunMode(type);
        blockInfo.RuntimeDriverTypeName = GetRuntimeDriverTypeName(type);
        (Guid block, Guid functionBlockbEnabledInput, Guid functionBlockEnabledOutput) = GetFunctionBlockDesignIds(type);
        blockInfo.Id = block;
        blockInfo.FunctionBlockEnabledInputId = functionBlockbEnabledInput;
        blockInfo.FunctionBlockEnabledOutputId = functionBlockEnabledOutput;

        Type[] nestedTypes = type.GetNestedTypes();
        Type? inputsType = nestedTypes.FirstOrDefault(x => Attribute.IsDefined(x, typeof(InputConnectorDesignsAttribute)));
        Type? outputsType = nestedTypes.FirstOrDefault(x => Attribute.IsDefined(x, typeof(OutputConnectorDesignsAttribute)));
        Type? settingsType = nestedTypes.FirstOrDefault(x => Attribute.IsDefined(x, typeof(SettingDesignsAttribute)));
        Type? persistentVariablesType = nestedTypes.FirstOrDefault(x => Attribute.IsDefined(x, typeof(PersistentVariableDesignsAttribute)));

        if (inputsType is not null)
        {
            IEnumerable<PropertyInfo> inputs = inputsType.GetProperties().Where(x => Attribute.IsDefined(x, typeof(InputConnectorDesignAttribute)));
            foreach (PropertyInfo input in inputs)
            {
                PoolingStrategy? defaultCrossSourcePoolingStrategy = GetDefaultPoolingStrategy(input, PoolingAttributeTarget.CrossSource);
                IAggregatingPooling? defaultCrossSourceAggregatingPooling = GetDefaultAggregatingPooling(input, PoolingAttributeTarget.CrossSource);
                PoolingStrategy? defaultPerSourcePoolingStrategy = GetDefaultPoolingStrategy(input, PoolingAttributeTarget.PerSource);
                IAggregatingPooling? defaultPerSourceAggregatingPooling = GetDefaultAggregatingPooling(input, PoolingAttributeTarget.PerSource);
                object? defaultValue = GetDefaultValue(input);
                IValidator[] validators = GetValidators(input);
                Type determinedType = DataTypeCompatibilityValidator.DetermineValueType(input.PropertyType);
                Guid id = GetInputConnectorDesignId(input);
                blockInfo.Inputs.Add(new ConnectorDesignInfo()
                {
                    DefaultCrossSourceAggregatingPooling = defaultCrossSourceAggregatingPooling,
                    DefaultCrossSourcePoolingStrategy = defaultCrossSourcePoolingStrategy,
                    DefaultPerSourceAggregatingPooling = defaultPerSourceAggregatingPooling,
                    DefaultPerSourcePoolingStrategy = defaultPerSourcePoolingStrategy,
                    DefaultValue = defaultValue,
                    Name = input.Name,
                    Type = determinedType,
                    Id = id,
                    Validators = validators,
                });
            }
        }

        if (outputsType is not null)
        {
            IEnumerable<PropertyInfo> outputs = outputsType.GetProperties().Where(x => Attribute.IsDefined(x, typeof(OutputConnectorDesignAttribute)));
            foreach (PropertyInfo output in outputs)
            {
                object? defaultValue = GetDefaultValue(output);
                IValidator[] validators = GetValidators(output);
                Type determinedType = DataTypeCompatibilityValidator.DetermineValueType(output.PropertyType);
                Guid id = GetOutputConnectorDesignId(output);
                blockInfo.Outputs.Add(new ConnectorDesignInfo()
                {
                    DefaultValue = defaultValue,
                    Name = output.Name,
                    Type = determinedType,
                    Id = id,
                    Validators = validators,
                });
            }
        }

        if (settingsType is not null)
        {
            IEnumerable<PropertyInfo> settings = settingsType.GetProperties().Where(x => Attribute.IsDefined(x, typeof(SettingDesignAttribute)));
            foreach (PropertyInfo setting in settings)
            {
                object? defaultValue = GetDefaultValue(setting);
                IValidator[] validators = GetValidators(setting);
                Type determinedType = DataTypeCompatibilityValidator.DetermineValueType(setting.PropertyType);
                (Guid settingId, Guid inputId, Guid outputId) = GetSettingDesignIds(setting);
                blockInfo.Settings.Add(new SettingDesignInfo()
                {
                    DefaultValue = defaultValue,
                    Name = setting.Name,
                    Type = determinedType,
                    Validators = validators,
                    Id = settingId,
                    InputId = inputId,
                    OutputId = outputId,
                });
            }
        }

        if (persistentVariablesType is not null)
        {
            IEnumerable<PropertyInfo> persistentVariables = persistentVariablesType.GetProperties().Where(x => Attribute.IsDefined(x, typeof(PersistentVariableDesignAttribute)));
            foreach (PropertyInfo persistentVariable in persistentVariables)
            {
                object? defaultValue = GetDefaultValue(persistentVariable);
                Type determinedType = DataTypeCompatibilityValidator.DetermineValueType(persistentVariable.PropertyType);
                Guid id = GetPersistentVariableDesignId(persistentVariable);
                blockInfo.PersistentVariables.Add(new PersistentVariableDesignInfo()
                {
                    DefaultValue = defaultValue,
                    Name = persistentVariable.Name,
                    Type = determinedType,
                    Id = id,
                });
            }
        }

        return blockInfo;
    }

    public static IEnumerable<FunctionBlockDesignInfo> GetFunctionBlockDesigns(IEnumerable<Assembly> assemblies)
    {
        IEnumerable<Type> types = GetAvailableTypes(assemblies);
        foreach (Type type in types)
            yield return GetFunctionBlockDesign(type);
    }

    private static (Guid Block, Guid EnabledInput, Guid EnabledOutput) GetFunctionBlockDesignIds(MemberInfo info)
    {
        FunctionBlockDesignAttribute attribute = info.GetCustomAttribute<FunctionBlockDesignAttribute>()!;
        return
            (
                attribute.Id,
                attribute.FunctionBlockEnabledInputId,
                attribute.FunctionBlockEnabledOutputId
            );
    }

    private static Guid GetInputConnectorDesignId(MemberInfo info)
        => info.GetCustomAttribute<InputConnectorDesignAttribute>()!.Id;

    private static Guid GetOutputConnectorDesignId(MemberInfo info)
        => info.GetCustomAttribute<OutputConnectorDesignAttribute>()!.Id;

    private static Guid GetPersistentVariableDesignId(MemberInfo info)
        => info.GetCustomAttribute<PersistentVariableDesignAttribute>()!.Id;

    private static string? GetRuntimeDriverTypeName(MemberInfo info)
    {
        DriverAttribute? attribute = info.GetCustomAttribute<DriverAttribute>();
        return attribute?.TypeName;
    }

    private static (string Namespace, string Name, string? RuntimeTypeName) GetNamespaceNameAndRuntimeTypeName(Type type)
    {
        FunctionBlockDesignAttribute attribute = type.GetCustomAttribute<FunctionBlockDesignAttribute>()!;
        string? @namespace = attribute.Namespace;
        string? name = attribute.Name;
        string? runtimeTypeName = @namespace is null && name is null
            ? null
            : type.FullName;

        @namespace ??= type.Namespace;
        name ??= type.Name;

        return (@namespace!, name!, runtimeTypeName);
    }

    private static (Guid Setting, Guid Input, Guid Output) GetSettingDesignIds(MemberInfo info)
    {
        SettingDesignAttribute attribute = info.GetCustomAttribute<SettingDesignAttribute>()!;
        return
            (
                attribute.Id,
                attribute.InputId,
                attribute.OutputId
            );
    }

    private static IValidator[] GetValidators(MemberInfo info)
    {
        IEnumerable<ValidatorAttribute> attributes = info.GetCustomAttributes<ValidatorAttribute>();
        return [.. attributes.Select(x => x.Validator)];
    }

    private static RunMode[]? GetValidRunModes(MemberInfo info)
    {
        ValidRunModesAttribute? attribute = info.GetCustomAttribute<ValidRunModesAttribute>();
        return attribute?.ValidRunModes;
    }
}
