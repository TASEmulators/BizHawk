using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		// TODO: Change this into a regular list.
		private List<bool> _lagLog = new List<bool>();
		private List<bool> _wasLag = new List<bool>();

		public bool? this[int frame]
		{
			get
			{
				if (frame < _lagLog.Count)
				{
					if (frame < 0)
					{
						return null;
					}

					return _lagLog[frame];
				}

				if (frame == Global.Emulator.Frame && frame == _lagLog.Count)
				{
					////LagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame; // Note: Side effects!
					return Global.Emulator.AsInputPollable().IsLagFrame;
				}

				return null;
			}

			set
			{
				if (!value.HasValue)
				{
					_lagLog.RemoveAt(frame);
					return;
				}

				if (frame < 0)
				{
					return; // Nothing to do
				}

				if (frame > _lagLog.Count)
				{
					System.Diagnostics.Debug.Print("Lag Log error. f" + frame + ", log: " + _lagLog.Count);
					return; // Can this break anything?
				}

				bool wasValue;
				if (frame < _lagLog.Count)
				{
					wasValue = _lagLog[frame];
				}
				else if (frame == _wasLag.Count)
				{
					wasValue = value.Value;
				}
				else
				{
					wasValue = _wasLag[frame];
				}

				if (frame == _wasLag.Count)
				{
					_wasLag.Add(wasValue);
				}
				else
				{
					_wasLag[frame] = wasValue;
				}

				if (frame != 0)
				{
					_wasLag[frame - 1] = _lagLog[frame - 1];
				}

				if (frame >= _lagLog.Count)
				{
					_lagLog.Add(value.Value);
				}
				else
				{
					_lagLog[frame] = value.Value;
				}
			}
		}

		public void Clear()
		{
			_lagLog.Clear();
		}

		public bool RemoveFrom(int frame)
		{
			if (_lagLog.Count > frame && frame >= 0)
			{
				_lagLog.RemoveRange(frame + 1, _lagLog.Count - frame - 1);
				return true;
			}

			return false;
		}

		public void RemoveHistoryAt(int frame)
		{
			_wasLag.RemoveAt(frame);
		}

		public void InsertHistoryAt(int frame, bool isLag)
		{
			// LagLog was invalidated when the frame was inserted
			if (frame <= _lagLog.Count)
			{
				_lagLog.Insert(frame, isLag);
			}

			_wasLag.Insert(frame, isLag);
		}

		public void Save(BinaryWriter bw)
		{
			bw.Write((byte)1); // New saving format.
			bw.Write(_lagLog.Count);
			bw.Write(_wasLag.Count);
			for (int i = 0; i < _lagLog.Count; i++)
			{
				bw.Write(_lagLog[i]);
				bw.Write(_wasLag[i]);
			}

			for (int i = _lagLog.Count; i < _wasLag.Count; i++)
			{
				bw.Write(_wasLag[i]);
			}
		}

		public void Load(BinaryReader br)
		{
			_lagLog.Clear();
			_wasLag.Clear();
			////if (br.BaseStream.Length > 0)
			////{ BaseStream.Length does not return the expected value.
			int formatVersion = br.ReadByte();
			if (formatVersion == 0)
			{
				int length = (br.ReadByte() << 8) | formatVersion; // The first byte should be a part of length.
				length = (br.ReadInt16() << 16) | length;
				for (int i = 0; i < length; i++)
				{
					br.ReadInt32();
					_lagLog.Add(br.ReadBoolean());
					_wasLag.Add(_lagLog.Last());
				}
			}
			else if (formatVersion == 1)
			{
				int length = br.ReadInt32();
				int lenWas = br.ReadInt32();
				for (int i = 0; i < length; i++)
				{
					_lagLog.Add(br.ReadBoolean());
					_wasLag.Add(br.ReadBoolean());
				}

				for (int i = length; i < lenWas; i++)
				{
					_wasLag.Add(br.ReadBoolean());
				}
			}
			////}
		}

		public bool? History(int frame)
		{
			if (frame < _wasLag.Count)
			{
				if (frame < 0)
				{
					return null;
				}

				return _wasLag[frame];
			}

			return null;
		}

		public int LastValidFrame
		{
			get
			{
				if (_lagLog.Count == 0)
				{
					return 0;
				}

				return _lagLog.Count - 1;
			}
		}

		public TasLagLog Clone()
		{
			return new TasLagLog
			{
				_lagLog = _lagLog.ToList(),
				_wasLag = _wasLag.ToList()
			};
		}

		public void FromLagLog(TasLagLog log)
		{
			_lagLog = log._lagLog.ToList();
			_wasLag = log._wasLag.ToList();
		}

		public void StartFromFrame(int index)
		{
			_lagLog.RemoveRange(0, index);
			_wasLag.RemoveRange(0, index);
		}
	}
}
