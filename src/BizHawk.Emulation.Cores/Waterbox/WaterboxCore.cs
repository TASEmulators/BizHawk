using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

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

		protected T PreInit<T>(WaterboxOptions options, IEnumerable<Delegate> allExtraDelegates = null)
			where T : LibWaterboxCore
		{
			options.Path ??= CoreComm.CoreFileProvider.DllPath();
			_exe = new WaterboxHost(options);
			var delegates = new Delegate[] { _inputCallback }.AsEnumerable();
			if (allExtraDelegates != null)
				delegates = delegates.Concat(allExtraDelegates);
			using (_exe.EnterExit())
			{
				var ret = BizInvoker.GetInvoker<T>(_exe, _exe, CallingConventionAdapters.MakeWaterbox(delegates, _exe));
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

				var memoryDomains = _memoryAreas.Select(a => WaterboxMemoryDomain.Create(a, _exe)).ToList();
				var primaryDomain = memoryDomains
					.Where(md => md.Definition.Flags.HasFlag(LibWaterboxCore.MemoryDomainFlags.Primary))
					.Single();

				var mdl = new MemoryDomainList(
					memoryDomains.Cast<MemoryDomain>()
						.Concat(new[] { _exe.GetPagesDomain() })
						.ToList()
				);
				mdl.MainMemory = primaryDomain;
				_serviceProvider.Register<IMemoryDomains>(mdl);

				_saveramAreas = memoryDomains
					.Where(md => md.Definition.Flags.HasFlag(LibWaterboxCore.MemoryDomainFlags.Saverammable))
					.ToArray();
				_saveramSize = (int)_saveramAreas.Sum(a => a.Size);

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

		private WaterboxMemoryDomain[] _saveramAreas;
		private int _saveramSize;

		public unsafe bool SaveRamModified
		{
			get
			{
				if (_saveramSize == 0)
					return false;
				var buff = new byte[4096];
				using (_exe.EnterExit())
				{
					fixed(byte* bp = buff)
					{
						foreach (var area in _saveramAreas)
						{
							var stream = new MemoryDomainStream(area);
							int cmp = (area.Definition.Flags & LibWaterboxCore.MemoryDomainFlags.OneFilled) != 0 ? -1 : 0;
							while (true)
							{
								int nread = stream.Read(buff, 0, 4096);
								if (nread == 0)
									break;
								
								int* p = (int*)bp;
								int* pend = p + nread / sizeof(int);
								while (p < pend)
								{
									if (*p++ != cmp)
										return true;
								}
							}
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
				var dest = new MemoryStream(ret, true);
				foreach (var area in _saveramAreas)
				{
					new MemoryDomainStream(area).CopyTo(dest);
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
					var source = new MemoryStream(data, false);
					foreach (var area in _saveramAreas)
					{
						WaterboxUtils.CopySome(source, new MemoryDomainStream(area), area.Size);
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

					if (render)
					{
						BufferWidth = frame.Width;
						BufferHeight = frame.Height;
					}
					if (rendersound)
					{
						_numSamples = frame.Samples;
					}
					else
					{
						_numSamples = 0;
					}

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
			using (_exe.EnterExit())
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
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			using (_exe.EnterExit())
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

		protected short[] _soundBuffer;
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
