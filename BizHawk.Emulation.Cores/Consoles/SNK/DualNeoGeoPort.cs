using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sound;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[Core("Dual NeoPop", "Thomas Klausner, Mednafen Team, natt", true, false, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class DualNeoGeoPort : IEmulator
	{
		private NeoGeoPort _left;
		private NeoGeoPort _right;
		private readonly BasicServiceProvider _serviceProvider;
		private bool _disposed = false;
		private readonly DualSyncSound _soundProvider;
		private readonly SideBySideVideo _videoProvider;
		private readonly LinkInterop _leftEnd;
		private readonly LinkInterop _rightEnd;
		private readonly LinkCable _linkCable;

		[CoreConstructor("DNGP")]
		public DualNeoGeoPort(CoreComm comm, byte[] rom, bool deterministic)
		{
			CoreComm = comm;
			_left = new NeoGeoPort(comm, rom, new NeoGeoPort.SyncSettings { Language = LibNeoGeoPort.Language.English }, deterministic, PeRunner.CanonicalStart);
			_right = new NeoGeoPort(comm, rom, new NeoGeoPort.SyncSettings { Language = LibNeoGeoPort.Language.English }, deterministic, PeRunner.AlternateStart);
			_linkCable = new LinkCable();
			_leftEnd = new LinkInterop(_left, _linkCable.LeftIn, _linkCable.LeftOut);
			_rightEnd = new LinkInterop(_right, _linkCable.RightIn, _linkCable.RightOut);


			_serviceProvider = new BasicServiceProvider(this);
			_soundProvider = new DualSyncSound(_left, _right);
			_serviceProvider.Register<ISoundProvider>(_soundProvider);
			_videoProvider = new SideBySideVideo(_left, _right);
			_serviceProvider.Register<IVideoProvider>(_videoProvider);
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			var t1 = Task.Run(() =>
			{
				_left.FrameAdvance(new PrefixController(controller, "P1 "), render, rendersound);
				_leftEnd.SignalEndOfFrame();
			});
			var t2 = Task.Run(() =>
			{
				_right.FrameAdvance(new PrefixController(controller, "P2 "), render, rendersound);
				_rightEnd.SignalEndOfFrame();
			});
			var t3 = Task.Run(() =>
			{
				_linkCable.RunFrame();
			});
			Task.WaitAll(t1, t2, t3);
			Frame++;
			_soundProvider.Fetch();
			_videoProvider.Fetch();

			return true;
		}

		#region link cable

		private class LinkCable
		{
			public readonly BlockingCollection<LinkRequest> LeftIn = new BlockingCollection<LinkRequest>();
			public readonly BlockingCollection<LinkResult> LeftOut = new BlockingCollection<LinkResult>();
			public readonly BlockingCollection<LinkRequest> RightIn = new BlockingCollection<LinkRequest>();
			public readonly BlockingCollection<LinkResult> RightOut = new BlockingCollection<LinkResult>();

			private readonly Queue<byte> _leftData = new Queue<byte>();
			private readonly Queue<byte> _rightData = new Queue<byte>();

			public void RunFrame()
			{
				LinkRequest l = LeftIn.Take();
				LinkRequest r = RightIn.Take();
				while (true)
				{
					switch (l.RequestType)
					{
						case LinkRequest.RequestTypes.EndOfFrame:
							if (r.RequestType == LinkRequest.RequestTypes.EndOfFrame)
							{
								Console.WriteLine("\nEnd of Frame {0} {1}", _leftData.Count, _rightData.Count);
								return;
							}
							break;
						case LinkRequest.RequestTypes.Write:
							Console.Write("LW ");
							_leftData.Enqueue(l.Data);
							l = LeftIn.Take();
							continue;
						case LinkRequest.RequestTypes.Read:
						case LinkRequest.RequestTypes.Poll:
							if (_rightData.Count > 0)
							{
								if (l.RequestType == LinkRequest.RequestTypes.Read)
									Console.Write("LR ");
								LeftOut.Add(new LinkResult
								{
									Data = l.RequestType == LinkRequest.RequestTypes.Read ? _rightData.Dequeue() : _rightData.Peek(),
									Return = true
								});
								l = LeftIn.Take();
								continue;
							}
							else if (r.RequestType != LinkRequest.RequestTypes.Write)
							{
								if (l.RequestType == LinkRequest.RequestTypes.Read)
									Console.Write("L! ");
								LeftOut.Add(new LinkResult
								{
									Data = l.Data,
									Return = false
								});
								l = LeftIn.Take();
								continue;
							}
							else
							{
								break;
							}
					}
					switch (r.RequestType)
					{
						case LinkRequest.RequestTypes.Write:
							Console.Write("RW ");
							_rightData.Enqueue(r.Data);
							r = RightIn.Take();
							continue;
						case LinkRequest.RequestTypes.Read:
						case LinkRequest.RequestTypes.Poll:
							if (_leftData.Count > 0)
							{
								if (r.RequestType == LinkRequest.RequestTypes.Read)
									Console.Write("RR ");
								RightOut.Add(new LinkResult
								{
									Data = r.RequestType == LinkRequest.RequestTypes.Read ? _leftData.Dequeue() : _leftData.Peek(),
									Return = true
								});
								r = RightIn.Take();
								continue;
							}
							else if (l.RequestType != LinkRequest.RequestTypes.Write)
							{
								if (r.RequestType == LinkRequest.RequestTypes.Read)
									Console.Write("R! ");
								RightOut.Add(new LinkResult
								{
									Data = r.Data,
									Return = false
								});
								r = RightIn.Take();
								continue;
							}
							else
							{
								break;
							}
					}
				}
			}
		}

		public struct LinkRequest
		{
			public enum RequestTypes : byte
			{
				Read,
				Poll,
				Write,
				EndOfFrame
			}
			public RequestTypes RequestType;
			public byte Data;
		}
		public struct LinkResult
		{
			public byte Data;
			public bool Return;
		}

		private unsafe class LinkInterop
		{
			private readonly BlockingCollection<LinkRequest> _push;
			private readonly BlockingCollection<LinkResult> _pull;
			private NeoGeoPort _core;
			private readonly IntPtr _readcb;
			private readonly IntPtr _pollcb;
			private readonly IntPtr _writecb;
			private readonly IImportResolver _exporter;

			public LinkInterop(NeoGeoPort core, BlockingCollection<LinkRequest> push, BlockingCollection<LinkResult> pull)
			{
				_core = core;
				_push = push;
				_pull = pull;
				_exporter = BizExvoker.GetExvoker(this, CallingConventionAdapters.Waterbox);
				_readcb = _exporter.GetProcAddrOrThrow("CommsReadCallback");
				_pollcb = _exporter.GetProcAddrOrThrow("CommsPollCallback");
				_writecb = _exporter.GetProcAddrOrThrow("CommsWriteCallback");
				ConnectPointers();
			}

			private void ConnectPointers()
			{
				_core._neopop.SetCommsCallbacks(_readcb, _pollcb, _writecb);
			}

			private bool CommsPollNoBuffer()
			{
				_push.Add(new LinkRequest
				{
					RequestType = LinkRequest.RequestTypes.Poll
				});
				return _pull.Take().Return;
			}

			[BizExport(CallingConvention.Cdecl)]
			public bool CommsReadCallback(byte* buffer)
			{
				if (buffer == null)
					return CommsPollNoBuffer();
				_push.Add(new LinkRequest
				{
					RequestType = LinkRequest.RequestTypes.Read,
					Data = *buffer
				});
				var r = _pull.Take();
				*buffer = r.Data;
				return r.Return;
			}
			[BizExport(CallingConvention.Cdecl)]
			public bool CommsPollCallback(byte* buffer)
			{
				if (buffer == null)
					return CommsPollNoBuffer();
				_push.Add(new LinkRequest
				{
					RequestType = LinkRequest.RequestTypes.Poll,
					Data = *buffer
				});
				var r = _pull.Take();
				*buffer = r.Data;
				return r.Return;
			}
			[BizExport(CallingConvention.Cdecl)]
			public void CommsWriteCallback(byte data)
			{
				_push.Add(new LinkRequest
				{
					RequestType = LinkRequest.RequestTypes.Write,
					Data = data
				});
			}

			public void SignalEndOfFrame()
			{
				_push.Add(new LinkRequest
				{
					RequestType = LinkRequest.RequestTypes.EndOfFrame
				});
			}

			public void PostLoadState()
			{
				ConnectPointers();
			}
		}

		#endregion

		private class PrefixController : IController
		{
			public PrefixController(IController controller, string prefix)
			{
				_controller = controller;
				_prefix = prefix;
			}

			private readonly IController _controller;
			private readonly string _prefix;

			public ControllerDefinition Definition => null;

			public float GetFloat(string name)
			{
				return _controller.GetFloat(_prefix + name);
			}

			public bool IsPressed(string button)
			{
				return _controller.IsPressed(_prefix + button);
			}
		}

		public ControllerDefinition ControllerDefinition => DualNeoGeoPortController;

		private static readonly ControllerDefinition DualNeoGeoPortController = new ControllerDefinition
		{
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 Option", "P1 Power",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 Option", "P2 Power"
			},
			Name = "Dual NeoGeo Portable Controller"
		};

		public void ResetCounters()
		{
			Frame = 0;
		}

		public int Frame { get; private set; }

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public CoreComm CoreComm { get; }

		public bool DeterministicEmulation => _left.DeterministicEmulation && _right.DeterministicEmulation;

		public string SystemId => "DNGP";

		public void Dispose()
		{
			if (!_disposed)
			{
				_left.Dispose();
				_right.Dispose();
				_left = null;
				_right = null;
				_disposed = true;
			}
		}
	}
}
