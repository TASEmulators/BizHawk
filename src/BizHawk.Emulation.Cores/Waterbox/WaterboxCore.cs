using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class WaterboxCore : IEmulator, IVideoProvider, ISoundProvider, IStatable,
		IInputPollable, ISaveRam
	{
		private LibWaterboxCore _core;
		protected WaterboxHost _exe;
		protected LibWaterboxCore.MemoryArea[] _memoryAreas;
		private LibWaterboxCore.EmptyCallback _inputCallback;
		protected CoreComm CoreComm { get; }

		public class Configuration
		{
			public int MaxWidth;
			public int MaxHeight;
			public int DefaultWidth;
			public int DefaultHeight;
			public int DefaultFpsNumerator;
			public int DefaultFpsDenominator;
			public int MaxSamples;
			public string SystemId;
		}

		protected WaterboxCore(CoreComm comm, Configuration c)
		{
			BufferWidth = c.DefaultWidth;
			BufferHeight = c.DefaultHeight;
			_videoBuffer = new int[c.MaxWidth * c.MaxHeight];
			_soundBuffer = new short[c.MaxSamples * 2];
			VsyncNumerator = c.DefaultFpsNumerator;
			VsyncDenominator = c.DefaultFpsDenominator;
			_serviceProvider = new BasicServiceProvider(this);
			SystemId = c.SystemId;
			CoreComm = comm;
			_inputCallback = InputCallbacks.Call;
		}

		protected T PreInit<T>(WaterboxOptions options)
			where T : LibWaterboxCore
		{
			options.Path ??= CoreComm.CoreFileProvider.DllPath();
			_exe = new WaterboxHost(options);
			using (_exe.EnterExit())
			{
				var ret = BizInvoker.GetInvoker<T>(_exe, _exe, CallingConventionAdapters.Waterbox);
				_core = ret;
				return ret;
			}
		}

		protected void PostInit()
		{
			using (_exe.EnterExit())
			{
				var areas = new LibWaterboxCore.MemoryArea[256];
				_core.GetMemoryAreas(areas);
				_memoryAreas = areas.Where(a => a.Data != IntPtr.Zero && a.Size != 0)
					.ToArray();
				_saveramAreas = _memoryAreas.Where(a => (a.Flags & LibWaterboxCore.MemoryDomainFlags.Saverammable) != 0)
					.ToArray();
				_saveramSize = (int)_saveramAreas.Sum(a => a.Size);

				var memoryDomains = _memoryAreas.Select(a => new LibWaterboxCore.WaterboxMemoryDomain(a, _exe));
				var primaryIndex = _memoryAreas
					.Select((a, i) => new { a, i })
					.Single(a => (a.a.Flags & LibWaterboxCore.MemoryDomainFlags.Primary) != 0).i;
				var mdl = new MemoryDomainList(memoryDomains.Cast<MemoryDomain>().ToList());
				mdl.MainMemory = mdl[primaryIndex];
				_serviceProvider.Register<IMemoryDomains>(mdl);

				var sr = _core as ICustomSaveram;
				if (sr != null)
					_serviceProvider.Register<ISaveRam>(new CustomSaverammer(sr)); // override the default implementation

				_exe.Seal();
			}
		}

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
		private long _clockTime;
		private int _clockRemainder;

		protected void InitializeRtc(DateTime start)
		{
			_clockTime = (long)(start - Epoch).TotalSeconds;
		}

		protected long GetRtcTime(bool realTime)
		{
			if (realTime && DeterministicEmulation)
				throw new InvalidOperationException();
			return realTime ? (long)(DateTime.Now - Epoch).TotalSeconds : _clockTime;
		}

		private void AdvanceRtc()
		{
			_clockRemainder += VsyncDenominator;
			if (_clockRemainder >= VsyncNumerator)
			{
				_clockRemainder -= VsyncNumerator;
				_clockTime++;
			}
		}

		private LibWaterboxCore.MemoryArea[] _saveramAreas;
		private int _saveramSize;

		public unsafe bool SaveRamModified
		{
			get
			{
				if (_saveramSize == 0)
					return false;
				using (_exe.EnterExit())
				{
					foreach (var area in _saveramAreas)
					{
						int* p = (int*)area.Data;
						int* pend = p + area.Size / sizeof(int);
						int cmp = (area.Flags & LibWaterboxCore.MemoryDomainFlags.OneFilled) != 0 ? -1 : 0;

						while (p < pend)
						{
							if (*p++ != cmp)
								return true;
						}
					}
				}
				return false;
			}
		}

		public byte[] CloneSaveRam()
		{
			if (_saveramSize == 0)
				return null;
			using (_exe.EnterExit())
			{
				var ret = new byte[_saveramSize];
				var offs = 0;
				foreach (var area in _saveramAreas)
				{
					Marshal.Copy(area.Data, ret, offs, (int)area.Size);
					offs += (int)area.Size;
				}
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			using (_exe.EnterExit())
			{
				if (data.Length != _saveramSize)
					throw new InvalidOperationException("Saveram size mismatch");
				using (_exe.EnterExit())
				{
					var offs = 0;
					foreach (var area in _saveramAreas)
					{
						Marshal.Copy(data, offs, area.Data, (int)area.Size);
						offs += (int)area.Size;
					}
				}
			}
		}

		protected abstract LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound);
		protected virtual void FrameAdvancePost()
		{ }

		public unsafe bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			using (_exe.EnterExit())
			{
				_core.SetInputCallback(InputCallbacks.Count > 0 ? _inputCallback : null);

				fixed (int* vp = _videoBuffer)
				fixed (short* sp = _soundBuffer)
				{
					var frame = FrameAdvancePrep(controller, render, rendersound);
					frame.VideoBuffer = (IntPtr)vp;
					frame.SoundBuffer = (IntPtr)sp;

					_core.FrameAdvance(frame);

					Frame++;
					if (IsLagFrame = frame.Lagged != 0)
						LagCount++;
					AdvanceRtc();

					BufferWidth = frame.Width;
					BufferHeight = frame.Height;
					_numSamples = frame.Samples;

					FrameAdvancePost();
				}
			}

			return true;
		}

		private bool _disposed = false;

		public virtual void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_disposed = true;
			}
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		protected readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;
		public virtual string SystemId { get; }
		public bool DeterministicEmulation { get; protected set; } = true;
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
		public virtual ControllerDefinition ControllerDefinition { get; protected set; } = NullController.Instance.Definition;

		public void LoadStateBinary(BinaryReader reader)
		{
			_exe.LoadStateBinary(reader);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			BufferWidth = reader.ReadInt32();
			BufferHeight = reader.ReadInt32();
			_clockTime = reader.ReadInt64();
			_clockRemainder = reader.ReadInt32();
			// reset pointers here!
			_core.SetInputCallback(null);
			//_exe.PrintDebuggingInfo();
			LoadStateBinaryInternal(reader);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_exe.SaveStateBinary(writer);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
			writer.Write(BufferWidth);
			writer.Write(BufferHeight);
			writer.Write(_clockTime);
			writer.Write(_clockRemainder);
			SaveStateBinaryInternal(writer);
		}

		public byte[] SaveStateBinary()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return ms.ToArray();
		}

		/// <summary>
		/// called after the base core saves state.  the core must save any other
		/// variables that it needs to.
		/// the default implementation does nothing
		/// </summary>
		protected virtual void SaveStateBinaryInternal(BinaryWriter writer)
		{

		}

		/// <summary>
		/// called after the base core loads state.  the core must load any other variables
		/// that were in SaveStateBinaryInternal and reset any native pointers.
		/// the default implementation does nothing
		/// </summary>
		protected virtual void LoadStateBinaryInternal(BinaryReader reader)
		{

		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuffer;
			nsamp = _numSamples;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
		}

		protected readonly short[] _soundBuffer;
		protected int _numSamples;
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public virtual int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		protected int[] _videoBuffer;
		public virtual int VirtualWidth => BufferWidth;
		public virtual int VirtualHeight => BufferHeight;
		public int BufferWidth { get; protected set; }
		public int BufferHeight { get; protected set; }
		public virtual int VsyncNumerator { get; protected set; }
		public virtual int VsyncDenominator { get; protected set; }
		public int BackgroundColor => unchecked((int)0xff000000);
	}
}
