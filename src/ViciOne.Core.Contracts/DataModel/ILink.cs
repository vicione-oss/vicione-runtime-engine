namespace ViciOne.Core.Contracts.DataModel;

public interface ILink : IIdProvider
{
    ITypeConversion? TypeConversion { get; }
}
