using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ILogEntryController : IController
	{
		Bk2LogEntryGenerator LogEntryGenerator { get; }
	}
}
