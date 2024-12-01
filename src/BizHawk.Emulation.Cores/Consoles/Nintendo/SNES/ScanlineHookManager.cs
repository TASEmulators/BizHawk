using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	// TODO: This class is specifically for the SNES Graphics Debugger, but written generally, perhaps it could be moved to a more generic place
	public class ScanlineHookManager
	{
		private readonly List<RegistrationRecord> _records = new List<RegistrationRecord>();

		public void Register(object tag, Action<int> callback)
		{
			Unregister(tag);

			_records.Add(new RegistrationRecord
			{
				Tag = tag,
				Callback = callback
			});

			OnHooksChanged();
		}

		public int HookCount => _records.Count;

		protected virtual void OnHooksChanged()
		{
		}

		public void Unregister(object tag)
		{
			_records.RemoveAll(r => r.Tag == tag);
		}

		public void HandleScanline(int scanline)
		{
			foreach (var rr in _records)
			{
				rr.Callback(scanline);
			}
		}

		private class RegistrationRecord
		{
			public object Tag { get; set; }

			public int Scanline { get; set; } = 0;

			public Action<int> Callback { get; set; }
		}
	}
}
