
#if NETSTANDARD
namespace DTZero.FactoryGenerator;
#else
namespace DTZero;
#endif

public enum FactoryOperation
{
	None = 0,
	Execute = AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Execute,
	Create = AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Read,
	Fetch = AuthorizeFactoryOperation.Fetch | AuthorizeFactoryOperation.Read,
	Insert = AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Write,
	Update = AuthorizeFactoryOperation.Update | AuthorizeFactoryOperation.Write,
	Delete = AuthorizeFactoryOperation.Delete | AuthorizeFactoryOperation.Write
}

