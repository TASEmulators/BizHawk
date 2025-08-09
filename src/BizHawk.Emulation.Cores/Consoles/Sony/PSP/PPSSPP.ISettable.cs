using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : ISettable<object, object>
	{
		object ISettable<object, object>.GetSettings() => throw new NotImplementedException();
		object ISettable<object, object>.GetSyncSettings() => throw new NotImplementedException();
		PutSettingsDirtyBits ISettable<object, object>.PutSettings(object o) => throw new NotImplementedException();
		PutSettingsDirtyBits ISettable<object, object>.PutSyncSettings(object o) => throw new NotImplementedException();
	}
}
