using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfish.Virtu.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jellyfish.Virtu
{
	public sealed class Machine : IDisposable
	{
		/// <summary>
		/// for deserialization only!!
		/// </summary>
		public Machine() { }

		public Machine(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();

			Cpu = new Cpu(this);
			Memory = new Memory(this, appleIIe);
			Keyboard = new Keyboard(this);
			GamePort = new GamePort(this);
			Cassette = new Cassette(this);
			Speaker = new Speaker(this);
			Video = new Video(this);
			NoSlotClock = new NoSlotClock(this);

			var emptySlot = new PeripheralCard(this);
			Slot1 = emptySlot;
			Slot2 = emptySlot;
			Slot3 = emptySlot;
			Slot4 = emptySlot;
			Slot5 = emptySlot;
			Slot6 = new DiskIIController(this, diskIIRom);
			Slot7 = emptySlot;

			Slots = new List<PeripheralCard> { null, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };
			Components = new List<MachineComponent> { Cpu, Memory, Keyboard, GamePort, Cassette, Speaker, Video, NoSlotClock, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };

			BootDiskII = Slots.OfType<DiskIIController>().Last();
		}

		public void Dispose()
		{
		}

		public void Reset()
		{
			foreach (var component in Components)
			{
				DebugService.WriteMessage("Resetting machine '{0}'", component.GetType().Name);
				component.Reset();
				//DebugService.WriteMessage("Reset machine '{0}'", component.GetType().Name);
			}
		}



		private void Initialize()
		{
			foreach (var component in Components)
			{
				DebugService.WriteMessage("Initializing machine '{0}'", component.GetType().Name);
				component.Initialize();
				//DebugService.WriteMessage("Initialized machine '{0}'", component.GetType().Name);
			}
		}

		private void Uninitialize()
		{
			foreach (var component in Components)
			{
				DebugService.WriteMessage("Uninitializing machine '{0}'", component.GetType().Name);
				component.Uninitialize();
				//DebugService.WriteMessage("Uninitialized machine '{0}'", component.GetType().Name);
			}
		}

		public void BizInitialize()
		{
			Initialize();
			Reset();
		}

		public void BizFrameAdvance(IEnumerable<string> buttons)
		{
			Lagged = true;
			DriveLight = false;

			Keyboard.SetKeys(buttons);

			//frame begins at vsync.. beginning of vblank
			while (Video.IsVBlank)
			{
				/*
				var sb = new System.Text.StringBuilder();
				sb.AppendFormat("{0} ", Cpu);
				for (int i = 0; i < 256; i++)
					sb.AppendFormat("{0:x2} ", Memory.Read(i));
				tw.WriteLine(sb.ToString());*/
				Events.HandleEvents(Cpu.Execute());
			}
			//now, while not vblank, we're in a frame
			while (!Video.IsVBlank)
			{
				/*
				var sb = new System.Text.StringBuilder();
				sb.AppendFormat("{0} ", Cpu);
				for (int i = 0; i < 256; i++)
					sb.AppendFormat("{0:x2} ", Memory.Read(i));
				tw.WriteLine(sb.ToString()); */

				Events.HandleEvents(Cpu.Execute());
			}
		}

		public void BizShutdown()
		{
			Uninitialize();
		}

		private static JsonSerializer CreateSerializer()
		{
			// TODO: converters could be cached for speedup

			var ser = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Auto,
				PreserveReferencesHandling = PreserveReferencesHandling.All, // leaving out Array is a very important problem, and means that we can't rely on a directly shared array to work.
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			};

			ser.Converters.Add(new TypeTypeConverter(new[]
			{
				// all expected Types to convert are either in this assembly or mscorlib
				typeof(Machine).Assembly,
				typeof(object).Assembly
			}));
			
			ser.Converters.Add(new DelegateConverter());
			ser.Converters.Add(new ArrayConverter());

			var cr = new DefaultContractResolver();
			cr.DefaultMembersSearchFlags |= System.Reflection.BindingFlags.NonPublic;
			ser.ContractResolver = cr;

			return ser;
		}

		public void Serialize(JsonWriter w)
		{
			CreateSerializer().Serialize(w, this);
		}

		public static Machine Deserialize(JsonReader r)
		{
			return CreateSerializer().Deserialize<Machine>(r);
		}

		public const string Version = "0.9.4.0";

		public MachineEvents Events { get; private set; }

		public Cpu Cpu { get; private set; }
		public Memory Memory { get; private set; }
		public Keyboard Keyboard { get; private set; }
		public GamePort GamePort { get; private set; }
		public Cassette Cassette { get; private set; }
		public Speaker Speaker { get; private set; }
		public Video Video { get; private set; }
		public NoSlotClock NoSlotClock { get; private set; }

		public PeripheralCard Slot1 { get; private set; }
		public PeripheralCard Slot2 { get; private set; }
		public PeripheralCard Slot3 { get; private set; }
		public PeripheralCard Slot4 { get; private set; }
		public PeripheralCard Slot5 { get; private set; }
		public PeripheralCard Slot6 { get; private set; }
		public PeripheralCard Slot7 { get; private set; }

		public IList<PeripheralCard> Slots { get; private set; }
		public IList<MachineComponent> Components { get; private set; }

		public DiskIIController BootDiskII { get; private set; }

		public bool Lagged { get; set; }
		public bool DriveLight { get; set; }

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", Cpu.RA },
				{ "X", Cpu.RX },
				{ "Y", Cpu.RY },
				{ "S", Cpu.RS },
				{ "PC", Cpu.RPC },
				{ "Flag C", Cpu.FlagC ? 1 : 0 },
				{ "Flag Z", Cpu.FlagZ ? 1 : 0 },
				{ "Flag I", Cpu.FlagI ? 1 : 0 },
				{ "Flag D", Cpu.FlagD ? 1 : 0 },
				{ "Flag B", Cpu.FlagB ? 1 : 0 },
				{ "Flag V", Cpu.FlagV ? 1 : 0 },
				{ "Flag N", Cpu.FlagN ? 1 : 0 },
				{ "Flag T", Cpu.FlagT ? 1 : 0 }
			};
		}
	}
}
