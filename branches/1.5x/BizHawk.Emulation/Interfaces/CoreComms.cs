using System;
using System.IO;
using System.Text;
namespace BizHawk
{
	public class CoreComm
	{
		public int NES_BackdropColor;
		public bool NES_UnlimitedSprites = false;
		public bool NES_ShowBG = true, NES_ShowOBJ = true;
		public bool PCE_ShowBG1 = true, PCE_ShowOBJ1 = true, PCE_ShowBG2 = true, PCE_ShowOBJ2 = true;
		public bool SMS_ShowBG = true, SMS_ShowOBJ = true;
		public bool GG_ShowClippedRegions;
		public bool GG_HighlightActiveDisplayRegion;

		public string PSX_FirmwaresPath;
		public string SNES_FirmwaresPath;
		public string C64_FirmwaresPath;

		public ICoreFileProvider CoreFileProvider;

		public string SNES_ExePath;
		public string SNES_Profile;
		public bool SNES_UseRingBuffer;
		public bool SNES_AlwaysDoubleSize;

		public bool SNES_ShowBG1_0, SNES_ShowBG2_0, SNES_ShowBG3_0, SNES_ShowBG4_0;
		public bool SNES_ShowBG1_1, SNES_ShowBG2_1, SNES_ShowBG3_1, SNES_ShowBG4_1;
		public bool SNES_ShowOBJ_0, SNES_ShowOBJ_1, SNES_ShowOBJ_2, SNES_ShowOBJ_3;

		public bool Atari2600_ShowBG = true, Atari2600_ShowPlayer1 = true, Atari2600_ShowPlayer2 = true, Atari2600_ShowMissle1 = true, Atari2600_ShowMissle2 = true, Atari2600_ShowBall = true, Atari2600_ShowPF = true;

		/// <summary>
		/// if this is set, then the cpu should dump trace info to CpuTraceStream
		/// </summary>
		public TraceBuffer Tracer = new TraceBuffer();

		/// <summary>
		/// for emu.on_snoop()
		/// </summary>
		public System.Action InputCallback;

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
				buffer.Append(content);
				buffer.Append('\n');
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

		private StringBuilder buffer;
		private bool logging = false;
	}

	public class MemoryCallbackSystem
	{
		public int? ReadAddr = null;
		private System.Action<uint> ReadCallback = null;
		public void SetReadCallback(System.Action<uint> func)
		{
			ReadCallback = func;
		}

		public bool HasRead
		{
			get
			{
				return ReadCallback != null;
			}
		}

		public void TriggerRead(int addr)
		{
			if (ReadCallback != null)
			{
				if (ReadAddr != null)
				{
					if (ReadAddr == addr)
					{
						ReadCallback((uint)addr);
					}
				}
				else
				{
					ReadCallback((uint)addr);
				}
			}
		}

		public int? WriteAddr = null;
		private System.Action<uint> WriteCallback = null;
		public void SetWriteCallback(System.Action<uint> func)
		{
			WriteCallback = func;
		}

		public bool HasWrite
		{
			get
			{
				return WriteCallback != null;
			}
		}

		public void TriggerWrite(int addr)
		{
			if (WriteCallback != null)
			{
				if (WriteAddr != null)
				{
					if (WriteAddr == addr)
					{
						WriteCallback((uint)addr);
					}
				}
				else
				{
					WriteCallback((uint)addr);
				}
			}
		}
	}
}
