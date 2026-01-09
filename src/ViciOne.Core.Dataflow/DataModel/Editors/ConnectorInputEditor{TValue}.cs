using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ConnectorInputEditor<TValue>(ConnectorInput<TValue> connector) : IConnectorEditor
{
    public ConnectorInput<TValue> Connector { get; } = connector;
    Connector IConnectorEditor.Connector => Connector;

    public ConnectorInputEditor(ConnectorDesignInput<TValue> design) : this(CreateModel(design)) { }

    public virtual void AddValidator(IValidator<TValue> validator)
    {
        ValidateValidatorNotAdded(validator);
        Connector.Validators.Add(validator);
    }

    private static IAggregatingPooling<TValue>? ClonePoolingIfNecessary(IAggregatingPooling<TValue>? pooling)
        => pooling is not null ? FastCloner.FastCloner.DeepClone(pooling) : null;

    private static ConnectorInput<TValue> CreateModel(ConnectorDesignInput<TValue> design)
    {
        var connector = (ConnectorInput<TValue>)Activator.CreateInstance(design.ConnectorType)!;
        connector.Design = design;
        connector.Id = Guid.NewGuid();
        connector.CrossSourcePoolingStrategy = design.DefaultCrossSourcePoolingStrategy;
        connector.CrossSourceAggregatingPooling = ClonePoolingIfNecessary(design.DefaultCrossSourceAggregatingPooling);
        connector.MarkAsChangedOnlyIfNotEqual = ConnectorDefaults.MarkAsChangedOnlyIfNotEqual;
        connector.PerSourcePoolingStrategy = design.DefaultPerSourcePoolingStrategy;
        connector.PerSourceAggregatingPooling = ClonePoolingIfNecessary(design.DefaultPerSourceAggregatingPooling);
        // TODO: muss der Standardwert auch geklont werden?
        connector.Value = design.DefaultValue;
        return connector;
    }

    public virtual bool RemoveValidator(IValidator<TValue> validator)
        => Connector.Validators.Remove(validator);

    protected virtual void ResetAggregatingPoolings()
    {
        SetCrossSourceAggregatingPooling(ClonePoolingIfNecessary(Connector.Design.DefaultCrossSourceAggregatingPooling));
        SetPerSourceAggregatingPooling(ClonePoolingIfNecessary(Connector.Design.DefaultPerSourceAggregatingPooling));
    }

    public virtual void ResetToDefaults()
    {
        ResetAggregatingPoolings();
        SetCrossSourcePoolingStrategy(Connector.Design.DefaultCrossSourcePoolingStrategy, Connector.CrossSourceAggregatingPooling);
        SetPerSourcePoolingStrategy(Connector.Design.DefaultPerSourcePoolingStrategy, Connector.PerSourceAggregatingPooling);
        // TODO: muss der Standardwert auch geklont werden?
        SetValue(Connector.Design.DefaultValue);
    }

    public virtual void SetCrossSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => Connector.CrossSourceAggregatingPooling = ValidateCrossSourceAggregatingPooling(pooling);

    public virtual void SetCrossSourcePoolingStrategy(PoolingStrategy strategy, IAggregatingPooling<TValue>? aggregatingPooling = null)
    {
        Connector.CrossSourcePoolingStrategy = ValidatePoolingStrategy(strategy);
        SetCrossSourceAggregatingPooling(aggregatingPooling);
    }

    public virtual void SetPerSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => Connector.PerSourceAggregatingPooling = ValidatePerSourceAggregatingPooling(pooling);

    public virtual void SetPerSourcePoolingStrategy(PoolingStrategy strategy, IAggregatingPooling<TValue>? aggregatingPooling = null)
    {
        Connector.PerSourcePoolingStrategy = ValidatePoolingStrategy(strategy);
        SetPerSourceAggregatingPooling(aggregatingPooling);
    }

    public virtual void SetMarkAsChangedOnlyIfNotEqual(bool markAsChangedOnlyIfNotEqual)
        => Connector.MarkAsChangedOnlyIfNotEqual = markAsChangedOnlyIfNotEqual;

    public virtual void SetValue(TValue? value)
        => Connector.Value = ValidateValue(value);

    protected virtual IAggregatingPooling<TValue>? ValidateAggregatingPooling(IAggregatingPooling<TValue>? pooling, PoolingStrategy poolingStrategy)
        => poolingStrategy == PoolingStrategy.Aggregate && pooling is null
            ? throw new InvalidOperationException($"If connector is set to pooling strategy '{PoolingStrategy.Aggregate}', the aggregating pooling cannot be set to null.")
            : pooling;

    protected virtual IAggregatingPooling<TValue>? ValidateCrossSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => ValidateAggregatingPooling(pooling, Connector.CrossSourcePoolingStrategy);

    protected virtual IAggregatingPooling<TValue>? ValidatePerSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => ValidateAggregatingPooling(pooling, Connector.PerSourcePoolingStrategy);

    protected virtual PoolingStrategy ValidatePoolingStrategy(PoolingStrategy strategy)
        => strategy == PoolingStrategy.Concat && typeof(TValue).IsArray == false
            ? throw new ArgumentException($"To use '{nameof(PoolingStrategy)}.{PoolingStrategy.Concat}' the connector´s value type must be an array type.")
            : strategy;

    protected virtual void ValidateValidatorNotAdded(IValidator<TValue> validator)
    {
        if (Connector.Validators.Contains(validator))
            throw new InvalidOperationException("This validator was already added to this connector.");
    }

    protected virtual TValue? ValidateValue(TValue? value)
    {
        // zweistufige Validierung
        foreach (IValidator<TValue> validator in Connector.Design.Validators)
            value = validator.Validate(value, true);
        foreach (IValidator<TValue> validator in Connector.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
