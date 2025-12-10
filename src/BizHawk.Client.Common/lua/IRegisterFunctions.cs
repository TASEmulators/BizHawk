namespace BizHawk.Client.Common
{
	public interface IRegisterFunctions
	{
		LuaLibraryBase.NLFAddCallback CreateAndRegisterNamedFunction { get; set; }
	}
}
