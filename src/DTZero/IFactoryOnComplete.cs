namespace DTZero;

public interface IFactoryOnComplete
{
	void FactoryComplete(FactoryOperation factoryOperation);
}

public interface IFactoryOnCompleteAsync
{
	Task FactoryCompleteAsync(FactoryOperation factoryOperation);
}
