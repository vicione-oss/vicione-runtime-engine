using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.Pooling;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ConnectorDesignInputEditor<TValue>(ConnectorDesignInput<TValue> design) : IConnectorDesignEditor
{
    public ConnectorDesignInput<TValue> Design { get; } = design;
    ConnectorDesign IConnectorDesignEditor.Design => Design;
    public FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }

    public ConnectorDesignInputEditor(string name, PoolingFactory poolingFactory) : this(CreateModel(name, poolingFactory)) { }

    public virtual void AddValidator(IValidator<TValue> validator)
    {
        ValidateValidatorNotAdded(validator);
        Design.Validators.Add(validator);
    }

    private static ConnectorDesignInput<TValue> CreateModel(string name, PoolingFactory poolingFactory)
    {
        var design = new ConnectorDesignInput<TValue>
        {
            Id = Guid.NewGuid(),
            Name = ValidateNameBase(name),
        };
        GetAndSetDefaultPoolings(design, poolingFactory);
        return design;
    }

    private static void GetAndSetDefaultPoolings(ConnectorDesignInput<TValue> design, PoolingFactory poolingFactory)
    {
        IAggregatingPooling<TValue>? aggregatingPooling = poolingFactory.GetNewDefaultPooling<TValue>();
        if (aggregatingPooling is null)
        {
            design.DefaultCrossSourcePoolingStrategy = PoolingStrategy.Queue;
            design.DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue;
        }
        else
        {
            design.DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate;
            design.DefaultPerSourcePoolingStrategy = PoolingStrategy.Aggregate;
            design.DefaultCrossSourceAggregatingPooling = aggregatingPooling;
            // HACK: the same instance must not be reused, so a new one is requested here
            design.DefaultPerSourceAggregatingPooling = poolingFactory.GetNewDefaultPooling<TValue>();
        }
    }

    protected virtual bool IsSystemReservedName(string name)
        => IsSystemReservedNameBase(name);

    protected static bool IsSystemReservedNameBase(string name)
        => name.Equals(SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase);

    public virtual bool RemoveValidator(IValidator<TValue> validator)
        => Design.Validators.Remove(validator);

    public virtual void SetDefaultCrossSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => Design.DefaultCrossSourceAggregatingPooling = ValidateCrossSourceAggregatingPooling(pooling);

    public virtual void SetDefaultCrossSourcePoolingStrategy(PoolingStrategy strategy, IAggregatingPooling<TValue>? aggregatingPooling = null)
    {
        Design.DefaultCrossSourcePoolingStrategy = ValidatePoolingStrategy(strategy);
        SetDefaultCrossSourceAggregatingPooling(aggregatingPooling);
    }

    public virtual void SetDefaultPerSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => Design.DefaultPerSourceAggregatingPooling = ValidatePerSourceAggregatingPooling(pooling);

    public virtual void SetDefaultPerSourcePoolingStrategy(PoolingStrategy strategy, IAggregatingPooling<TValue>? aggregatingPooling = null)
    {
        Design.DefaultPerSourcePoolingStrategy = ValidatePoolingStrategy(strategy);
        SetDefaultPerSourceAggregatingPooling(aggregatingPooling);
    }

    public virtual void SetDefaultValue(TValue? value)
        => Design.DefaultValue = ValidateValue(value);

    public virtual void SetName(string name)
        => Design.Name = ValidateName(name);

    protected virtual IAggregatingPooling<TValue>? ValidateAggregatingPooling(IAggregatingPooling<TValue>? pooling, PoolingStrategy defaultPoolingStrategy)
        => defaultPoolingStrategy == PoolingStrategy.Aggregate && pooling is null
            ? throw new InvalidOperationException($"If connector is set to pooling strategy '{PoolingStrategy.Aggregate}', the aggregation pooling cannot be set to null.")
            : pooling;

    protected virtual IAggregatingPooling<TValue>? ValidateCrossSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => ValidateAggregatingPooling(pooling, Design.DefaultCrossSourcePoolingStrategy);

    protected virtual IAggregatingPooling<TValue>? ValidatePerSourceAggregatingPooling(IAggregatingPooling<TValue>? pooling)
        => ValidateAggregatingPooling(pooling, Design.DefaultPerSourcePoolingStrategy);

    protected virtual string ValidateName(string name)
    {
        name = ValidateNameBase(name);

        if (name.Equals(Design.Name, StringComparison.Ordinal) == false)
            ValidateNameNotExisting(name);

        return name;
    }

    protected static string ValidateNameBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));

        name = name.Trim();
        if (NameValidator.IsValidNamespaceOrClassName(name) == false)
            throw ExceptionFactory.CreateNoValidNamespaceOrClassNameException(name);

        if (IsSystemReservedNameBase(name))
            throw new ArgumentException("The specified name is a reserved system connector name.", nameof(name));

        return name;
    }

    protected virtual void ValidateNameNotExisting(string name)
        => FunctionBlockDesignEditor?.ValidateConnectorDesignNameNotExisting(Design, name);

    protected virtual PoolingStrategy ValidatePoolingStrategy(PoolingStrategy strategy)
        => strategy == PoolingStrategy.Concat && typeof(TValue).IsArray == false
            ? throw new ArgumentException($"To use '{nameof(PoolingStrategy)}.{PoolingStrategy.Concat}' the connector´s value type must be an array type.")
            : strategy;

    protected virtual void ValidateValidatorNotAdded(IValidator<TValue> validator)
    {
        if (Design.Validators.Contains(validator))
            throw new InvalidOperationException("This validator was already added to this design.");
    }

    protected virtual TValue? ValidateValue(TValue? value)
    {
        foreach (IValidator<TValue> validator in Design.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
