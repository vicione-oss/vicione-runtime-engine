using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyConnector<TValue> : IConnector<TValue>
{
    public IConnectorDesign<TValue> Design => new EmptyConnectorDesign<TValue>();
    IConnectorDesign IConnector.Design => Design;
    public bool MarkAsChangedOnlyIfNotEqual => default;
    public Guid Id => default;
    public IEnumerable<IValidator<TValue>> Validators => [];
    IEnumerable<IValidator> IConnector.Validators => Validators;
    public TValue? Value { get => default; set { } }
    object? IConnector.Value { get => default; set { } }
}
