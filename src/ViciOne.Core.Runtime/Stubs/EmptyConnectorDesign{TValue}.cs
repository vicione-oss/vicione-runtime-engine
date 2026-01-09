using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyConnectorDesign<TValue> : IConnectorDesign<TValue>
{
    public Type ConnectorEditorType => typeof(object);
    public Type ConnectorType => typeof(object);
    public TValue? DefaultValue => default;
    object? IConnectorDesign.DefaultValue => default;
    public string Name => string.Empty;
    public Guid Id => default;
    public IEnumerable<IValidator<TValue>> Validators => [];
    IEnumerable<IValidator> IConnectorDesign.Validators => Validators;
}
