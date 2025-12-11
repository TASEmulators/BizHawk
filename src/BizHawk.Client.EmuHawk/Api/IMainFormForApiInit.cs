using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForApiInit : IMainFormForApi, IDialogParent
	{
		IMovieSession MovieSession { get; }

		ToolManager Tools { get; }
	}
}
