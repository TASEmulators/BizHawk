using System.Diagnostics.CodeAnalysis;

namespace BizHawk.API.ApiHawk
{
	public interface IRegisterAccess
	{
		[DisallowNull]
		ulong? this[string register] { get; set; }
	}
}
