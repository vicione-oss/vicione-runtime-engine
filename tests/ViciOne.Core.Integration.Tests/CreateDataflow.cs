public abstract class CreateDataflow : LoadingAndSaving
{
    private protected FunctionBlockDesigns FBDesigns { get; } = new FunctionBlockDesigns();

    protected CreateDataflow() : base()
        => FBDesigns = CreateFunctionBlockDesigns.CreateDesigns(PoolingFactory);
}
