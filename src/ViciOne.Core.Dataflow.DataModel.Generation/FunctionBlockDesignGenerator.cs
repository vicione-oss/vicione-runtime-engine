using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;
using ViciOne.Core.Dataflow.Pooling;

namespace ViciOne.Core.Dataflow.DataModel.Generation;

public static class FunctionBlockDesignGenerator
{
    private static readonly MethodInfo s_createInputEditorMethod = typeof(FunctionBlockDesignGenerator).GetMethod(nameof(CreateInputEditor), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo s_createOutputEditorMethod = typeof(FunctionBlockDesignGenerator).GetMethod(nameof(CreateOutputEditor), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo s_createPersistentVariableEditorMethod = typeof(FunctionBlockDesignGenerator).GetMethod(nameof(CreatePersistentVariableEditor), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo s_createSettingEditorMethod = typeof(FunctionBlockDesignGenerator).GetMethod(nameof(CreateSettingEditor), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void AddInput(FunctionBlockDesignEditor blockEditor, ConnectorDesignInfo connectorInfo,
        PoolingFactory poolingFactory)
    {
        MethodInfo genericMethod = s_createInputEditorMethod.MakeGenericMethod(connectorInfo.Type);
        var inputEditor = (IConnectorDesignEditor)genericMethod.Invoke(null,
        [
            connectorInfo.Id,
            connectorInfo.Name,
            connectorInfo.DefaultValue is not null,
            connectorInfo.DefaultValue,
            connectorInfo.DefaultCrossSourcePoolingStrategy.HasValue,
            connectorInfo.DefaultCrossSourcePoolingStrategy,
            connectorInfo.DefaultCrossSourceAggregatingPooling is not null,
            connectorInfo.DefaultCrossSourceAggregatingPooling,
            connectorInfo.DefaultPerSourcePoolingStrategy.HasValue,
            connectorInfo.DefaultPerSourcePoolingStrategy,
            connectorInfo.DefaultPerSourceAggregatingPooling is not null,
            connectorInfo.DefaultPerSourceAggregatingPooling,
            connectorInfo.Validators,
            poolingFactory,
        ])!;

        blockEditor.AddProcessDataInput(inputEditor);
    }

    private static void AddInputs(FunctionBlockDesignEditor blockEditor, FunctionBlockDesignInfo blockInfo,
        PoolingFactory poolingFactory)
    {
        foreach (ConnectorDesignInfo input in blockInfo.Inputs)
            AddInput(blockEditor, input, poolingFactory);
    }

    private static void AddOutput(FunctionBlockDesignEditor blockEditor, ConnectorDesignInfo connectorInfo)
    {
        MethodInfo genericMethod = s_createOutputEditorMethod.MakeGenericMethod(connectorInfo.Type);
        var outputEditor = (IConnectorDesignEditor)genericMethod.Invoke(null,
        [
            connectorInfo.Id,
            connectorInfo.Name,
            connectorInfo.DefaultValue is not null,
            connectorInfo.DefaultValue,
            connectorInfo.Validators,
        ])!;

        blockEditor.AddProcessDataOutput(outputEditor);
    }

    private static void AddOutputs(FunctionBlockDesignEditor blockEditor, FunctionBlockDesignInfo blockInfo)
    {
        foreach (ConnectorDesignInfo output in blockInfo.Outputs)
            AddOutput(blockEditor, output);
    }

    private static void AddPersistentVariable(FunctionBlockDesignEditor blockEditor, PersistentVariableDesignInfo variableInfo)
    {
        MethodInfo genericMethod = s_createPersistentVariableEditorMethod.MakeGenericMethod(variableInfo.Type);
        var variableEditor = (IPersistentVariableDesignEditor)genericMethod.Invoke(null,
        [
            variableInfo.Name,
            variableInfo.Id,
            variableInfo.DefaultValue is not null,
            variableInfo.DefaultValue,
        ])!;

        blockEditor.AddPersistentVariable(variableEditor);
    }

    private static void AddPersistentVariables(FunctionBlockDesignEditor blockEditor, FunctionBlockDesignInfo blockInfo)
    {
        foreach (PersistentVariableDesignInfo variable in blockInfo.PersistentVariables)
            AddPersistentVariable(blockEditor, variable);
    }

    private static void AddSetting(FunctionBlockDesignEditor blockEditor, SettingDesignInfo settingInfo, PoolingFactory poolingFactory)
    {
        MethodInfo genericMethod = s_createSettingEditorMethod.MakeGenericMethod(settingInfo.Type);
        var settingEditor = (ISettingDesignEditor)genericMethod.Invoke(null,
        [
            settingInfo.Name,
            settingInfo.Id,
            settingInfo.InputId,
            settingInfo.OutputId,
            settingInfo.DefaultValue is not null,
            settingInfo.DefaultValue,
            settingInfo.Validators,
            poolingFactory,
        ])!;

        blockEditor.AddSetting(settingEditor);
    }

    private static void AddSettings(FunctionBlockDesignEditor blockEditor, FunctionBlockDesignInfo blockInfo, PoolingFactory poolingFactory)
    {
        foreach (SettingDesignInfo setting in blockInfo.Settings)
            AddSetting(blockEditor, setting, poolingFactory);
    }

    private static ConnectorDesignInputEditor<TValue> CreateInputEditor<TValue>(Guid id, string name, bool setDefaultValue, TValue? defaultValue,
        bool setDefaultCrossSourcePoolingStrategy, PoolingStrategy? defaultCrossSourcePoolingStrategy, bool setDefaultCrossSourceAggregatingPooling, IAggregatingPooling<TValue>? defaultCrossSourceAggregatingPooling,
        bool setDefaultPerSourcePoolingStrategy, PoolingStrategy? defaultPerSourcePoolingStrategy, bool setDefaultPerSourceAggregatingPooling, IAggregatingPooling<TValue>? defaultPerSourceAggregatingPooling,
        object[] validators, PoolingFactory poolingFactory)
    {
        var connectorEditor = new ConnectorDesignInputEditor<TValue>(name, poolingFactory);
        connectorEditor.Design.Id = id;
        if (setDefaultValue)
            connectorEditor.SetDefaultValue(defaultValue);
        if (setDefaultCrossSourcePoolingStrategy)
            connectorEditor.SetDefaultCrossSourcePoolingStrategy(defaultCrossSourcePoolingStrategy!.Value, defaultCrossSourceAggregatingPooling);
        if (setDefaultCrossSourceAggregatingPooling)
            connectorEditor.SetDefaultCrossSourceAggregatingPooling(defaultCrossSourceAggregatingPooling!);
        if (setDefaultPerSourcePoolingStrategy)
            connectorEditor.SetDefaultPerSourcePoolingStrategy(defaultPerSourcePoolingStrategy!.Value, defaultPerSourceAggregatingPooling);
        if (setDefaultPerSourceAggregatingPooling)
            connectorEditor.SetDefaultPerSourceAggregatingPooling(defaultPerSourceAggregatingPooling!);
        foreach (IValidator<TValue> validator in validators.Cast<IValidator<TValue>>())
            connectorEditor.AddValidator(validator);
        return connectorEditor;
    }

    private static ConnectorDesignOutputEditor<TValue> CreateOutputEditor<TValue>(Guid id, string name, bool setDefaultValue, TValue? defaultValue,
        object[] validators)
    {
        var connectorEditor = new ConnectorDesignOutputEditor<TValue>(name);
        connectorEditor.Design.Id = id;
        if (setDefaultValue)
            connectorEditor.SetDefaultValue(defaultValue);
        foreach (IValidator<TValue> validator in validators.Cast<IValidator<TValue>>())
            connectorEditor.AddValidator(validator);
        return connectorEditor;
    }

    private static PersistentVariableDesignEditor<TValue> CreatePersistentVariableEditor<TValue>(string name, Guid id, bool setDefaultValue,
        TValue? defaultValue)
    {
        var persistentVariableEditor = new PersistentVariableDesignEditor<TValue>(name);
        persistentVariableEditor.PersistentVariable.Id = id;
        if (setDefaultValue)
            persistentVariableEditor.SetDefaultValue(defaultValue);
        return persistentVariableEditor;
    }

    private static SettingDesignEditor<TValue> CreateSettingEditor<TValue>(string name, Guid id, Guid inputId, Guid outputId, bool setDefaultValue, TValue? defaultValue,
        object[] validators, PoolingFactory poolingFactory)
    {
        var settingEditor = new SettingDesignEditor<TValue>(name, poolingFactory);
        settingEditor.Setting.Id = id;
        settingEditor.Setting.Input.Id = inputId;
        settingEditor.Setting.Output.Id = outputId;
        if (setDefaultValue)
            settingEditor.SetDefaultValue(defaultValue);
        foreach (IValidator<TValue> validator in validators.Cast<IValidator<TValue>>())
            settingEditor.AddValidator(validator);
        return settingEditor;
    }

    public static FunctionBlockDesign GenerateFunctionBlockDesign(FunctionBlockDesignInfo blockInfo, PoolingFactory poolingFactory)
    {
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = blockInfo.Id;
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs.First(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName)).Id = blockInfo.FunctionBlockEnabledInputId;
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs.First(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName)).Id = blockInfo.FunctionBlockEnabledOutputId;
        designEditor.SetNamespace(blockInfo.Namespace);
        designEditor.SetName(blockInfo.Name);
        designEditor.SetRuntimeDriverTypeName(blockInfo.RuntimeDriverTypeName);
        designEditor.SetRuntimeTypeName(blockInfo.RuntimeTypeName);
        if (blockInfo.DefaultCycleFrequency.HasValue)
            designEditor.SetDefaultCycleFrequency(blockInfo.DefaultCycleFrequency.Value);
        if (blockInfo.ValidRunModes is not null)
            SetValidRunModes(designEditor, blockInfo.ValidRunModes);
        if (blockInfo.DefaultRunMode.HasValue)
            designEditor.SetDefaultRunMode(blockInfo.DefaultRunMode.Value);

        AddSettings(designEditor, blockInfo, poolingFactory);
        AddPersistentVariables(designEditor, blockInfo);
        AddInputs(designEditor, blockInfo, poolingFactory);
        AddOutputs(designEditor, blockInfo);

        return designEditor.Design;
    }

    public static IEnumerable<FunctionBlockDesign> GenerateFunctionBlockDesigns(IEnumerable<FunctionBlockDesignInfo> designInfos, PoolingFactory poolingFactory)
    {
        foreach (FunctionBlockDesignInfo info in designInfos)
            yield return GenerateFunctionBlockDesign(info, poolingFactory);
    }

    private static void SetValidRunModes(FunctionBlockDesignEditor designEditor, RunMode[] validRunModes)
    {
        bool defaultRunModeIsInNewValidRunModes = validRunModes.Contains(designEditor.Design.DefaultRunMode);
        RunMode[] runModesToAdd = [.. validRunModes.Except(designEditor.Design.ValidRunModes)];
        RunMode[] runModesToRemove = [.. designEditor.Design.ValidRunModes.Except(validRunModes)];
        foreach (RunMode runMode in runModesToAdd)
        {
            designEditor.AddValidRunMode(runMode);
            if (defaultRunModeIsInNewValidRunModes == false)
            {
                designEditor.SetDefaultRunMode(runMode);
                defaultRunModeIsInNewValidRunModes = true;
            }
        }
        foreach (RunMode runMode in runModesToRemove)
            designEditor.RemoveValidRunMode(runMode);
    }
}
