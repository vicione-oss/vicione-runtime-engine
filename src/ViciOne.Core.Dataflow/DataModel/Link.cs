using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class Link : ILink
{
    public Guid Id { get; set; }
    public TypeConversion? TypeConversion { get; set; }
    ITypeConversion? ILink.TypeConversion => TypeConversion;
}
