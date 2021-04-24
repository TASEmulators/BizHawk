using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IQuickBmpFile
	{
		void Copy(IVideoProvider src, IVideoProvider dst);

		bool Load(IVideoProvider v, Stream s);

		bool LoadAuto(Stream s, out IVideoProvider vp);

		void Save(IVideoProvider v, Stream s, int w, int h);
	}
}
