using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class CoreComm
	{
		public ICoreFileProvider CoreFileProvider;

		/// <summary>
		/// if this is set, then the cpu should dump trace info to CpuTraceStream
		/// </summary>
		public TraceBuffer Tracer = new TraceBuffer();

		/// <summary>
		/// for emu.on_snoop()
		/// </summary>
		public InputCallbackSystem InputCallback = new InputCallbackSystem();

		public MemoryCallbackSystem MemoryCallbackSystem = new MemoryCallbackSystem();

		public double VsyncRate
		{
			get
			{
				return VsyncNum / (double)VsyncDen;
			}
		}
		public int VsyncNum = 60;
		public int VsyncDen = 1;

		//a core should set these if you wish to provide rom status information yourself. otherwise it will be calculated by the frontend in a way you may not like, using RomGame-related concepts.
		public string RomStatusAnnotation;
		public string RomStatusDetails;

		public int ScreenLogicalOffsetX, ScreenLogicalOffsetY;

		public bool CpuTraceAvailable = false;

		public string TraceHeader = "Instructions";

		// size hint to a/v out resizer.  this probably belongs in VideoProvider?  but it's somewhat different than VirtualWidth...
		public int NominalWidth = 640;
		public int NominalHeight = 480;

		public bool DriveLED = false;
		public bool UsesDriveLed = false;

		/// <summary>
		/// show a message.  reasonably annoying (dialog box), shouldn't be used most of the time
		/// </summary>
		public Action<string> ShowMessage { get; private set; }

		/// <summary>
		/// show a message.  less annoying (OSD message). Should be used for ignorable helpful messages
		/// </summary>
		public Action<string> Notify { get; private set; }

		public CoreComm(Action<string> ShowMessage, Action<string> NotifyMessage)
		{
			this.ShowMessage = ShowMessage;
			this.Notify = NotifyMessage;
		}

		public Func<object> RequestGLContext;
		public Action<object> ActivateGLContext;
		public Action DeactivateGLContext; //this shouldnt be necessary.. frontend should be changing context before it does anything.. but for now..

		public Func<bool> DispSnowyNullEmulator;
	}

	public class TraceBuffer
	{
		public string TakeContents()
		{
			string s = buffer.ToString();
			buffer.Clear();
			return s;
		}

		public string Contents
		{
			get
			{
				return buffer.ToString();
			}
		}

		public void Put(string content)
		{
			if (logging)
			{
				buffer.AppendLine(content);
			}
		}

		public TraceBuffer()
		{
			buffer = new StringBuilder();
		}

		public bool Enabled
		{
			get
			{
				return logging;
			}

			set
			{
				logging = value;
			}
		}

		private readonly StringBuilder buffer;
		private bool logging;
	}

	public class InputCallbackSystem : List<Action>
	{
		public void Call()
		{
			foreach (var action in this)
			{
				action();
			}
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			foreach (var action in actions)
			{
				this.Remove(action);
			}
		}
	}

	public class MemoryCallbackSystem
	{
		private readonly List<Action> _reads = new List<Action>();
		private readonly List<uint?> _readAddrs = new List<uint?>();
		
		private readonly List<Action> _writes = new List<Action>();
		private readonly List<uint?> _writeAddrs = new List<uint?>();

		private readonly List<Action> _executes = new List<Action>();
		private readonly List<uint> _execAddrs = new List<uint>();

		public void AddRead(Action function, uint? addr)
		{
			_reads.Add(function);
			_readAddrs.Add(addr);
		}

		public void AddWrite(Action function, uint? addr)
		{
			_writes.Add(function);
			_writeAddrs.Add(addr);
		}

		public void AddExecute(Action function, uint addr)
		{
			_executes.Add(function);
			_execAddrs.Add(addr);
		}

		public void CallRead(uint addr)
		{
			for (int i = 0; i < _reads.Count; i++)
			{
				if (!_readAddrs[i].HasValue || _readAddrs[i].Value == addr)
				{
					_reads[i]();
				}
			}
		}

		public void CallWrite(uint addr)
		{
			for (int i = 0; i < _writes.Count; i++)
			{
				if (!_writeAddrs[i].HasValue || _writeAddrs[i] == addr)
				{
					_writes[i]();
				}
			}
		}

		public void CallExecute(uint addr)
		{
			for (int i = 0; i < _executes.Count; i++)
			{
				if (_execAddrs[i] == addr)
				{
					_executes[i]();
				}
			}
		}

		public bool HasReads { get { return _reads.Any(); } }
		public bool HasWrites { get { return _writes.Any(); } }
		public bool HasExecutes { get { return _executes.Any(); } }

		public void Remove(Action action)
		{
			for (int i = 0; i < _reads.Count; i++)
			{
				if (_reads[i] == action)
				{
					_reads.Remove(_reads[i]);
					_readAddrs.Remove(_readAddrs[i]);
				}
			}

			for (int i = 0; i < _writes.Count; i++)
			{
				if (_writes[i] == action)
				{
					_writes.Remove(_writes[i]);
					_writeAddrs.Remove(_writeAddrs[i]);
				}
			}

			for (int i = 0; i < _executes.Count; i++)
			{
				if (_executes[i] == action)
				{
					_executes.Remove(_executes[i]);
					_execAddrs.Remove(_execAddrs[i]);
				}
			}
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			foreach (var action in actions)
			{
				Remove(action);
			}
		}

		public void Clear()
		{
			_reads.Clear();
			_readAddrs.Clear();
			_writes.Clear();
			_writes.Clear();
			_executes.Clear();
			_execAddrs.Clear();
		}
	}
}
