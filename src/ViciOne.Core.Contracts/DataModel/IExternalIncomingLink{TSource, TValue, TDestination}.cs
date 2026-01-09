namespace ViciOne.Core.Contracts.DataModel;

public interface IExternalIncomingLink<TSource, TValue, TDestination> : IExternalIncomingLink, ILinkWithDestination<TValue, TDestination>
{
    new TValue? DefaultValue { get; set; }
}
