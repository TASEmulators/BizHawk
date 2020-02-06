using System;
using System.Linq;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class MovieZone
	{
		private readonly IEmulator _emulator;
		private readonly ToolManager _tools;
		private readonly string[] _log;
		private readonly Bk2ControllerAdapter _targetController;
		private string _inputKey;
		private Bk2ControllerAdapter _controller;

		public MovieZone(IMovie movie, IEmulator emulator, ToolManager tools, int start, int length, string key = "")
		{
			_emulator = emulator;
			_tools = tools;
			var lg = movie.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			_targetController = new Bk2ControllerAdapter { Definition = _emulator.ControllerDefinition };
			_targetController.LatchFromSource(_targetController); // Reference and create all buttons

			if (key == "")
			{
				key = lg.GenerateLogKey();
			}

			key = key.Replace("LogKey:", "").Replace("#", "");
			key = key.Substring(0, key.Length - 1);

			_inputKey = key;
			Length = length;
			_log = new string[length];

			// Get a IController that only contains buttons in key.
			string[] keys = key.Split('|');
			var d = new ControllerDefinition();
			foreach (var k in keys)
			{
				if (_emulator.ControllerDefinition.BoolButtons.Contains(k))
				{
					d.BoolButtons.Add(k);
				}
				else
				{
					d.FloatControls.Add(k);
					int rangeIndex = _emulator.ControllerDefinition.FloatControls.IndexOf(k);
					d.FloatRanges.Add(_emulator.ControllerDefinition.FloatRanges[rangeIndex]);
				}
			}

			_controller = new Bk2ControllerAdapter { Definition = d };
			var logGenerator = new Bk2LogEntryGenerator("");
			logGenerator.SetSource(_controller);
			logGenerator.GenerateLogEntry(); // Reference and create all buttons.

			string movieKey = logGenerator.GenerateLogKey().Replace("LogKey:", "").Replace("#", "");
			movieKey = movieKey.Substring(0, movieKey.Length - 1);
			if (key == movieKey)
			{
				for (int i = 0; i < length; i++)
				{
					_log[i] = movie.GetInputLogEntry(i + start);
				}
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					_controller.LatchFromSource(movie.GetInputState(i + start));
					_log[i] = logGenerator.GenerateLogEntry();
				}
			}
		}

		public string Name { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }

		public bool Replace { get; set; } = true;
		public bool Overlay { get; set; }

		public string InputKey
		{
			get => _inputKey;
			set { _inputKey = value; ReSetLog(); }
		}

		private void ReSetLog()
		{
			// Get a IController that only contains buttons in key.
			string[] keys = _inputKey.Split('|');
			var d = new ControllerDefinition();
			foreach (var key in keys)
			{
				if (_emulator.ControllerDefinition.BoolButtons.Contains(key))
				{
					d.BoolButtons.Add(key);
				}
				else
				{
					d.FloatControls.Add(key);
				}
			}

			var newController = new Bk2ControllerAdapter { Definition = d };
			var logGenerator = new Bk2LogEntryGenerator("");

			logGenerator.SetSource(newController);
			logGenerator.GenerateLogEntry(); // Reference and create all buttons.

			// Reset all buttons in targetController (it may still have buttons that aren't being set here set true)
			var tC = new Bk2LogEntryGenerator("");
			tC.SetSource(_targetController);
			_targetController.SetControllersAsMnemonic(tC.EmptyEntry);
			for (int i = 0; i < Length; i++)
			{
				_controller.SetControllersAsMnemonic(_log[i]);
				LatchFromSourceButtons(_targetController, _controller);
				newController.LatchFromSource(_targetController);
				_log[i] = logGenerator.GenerateLogEntry();
			}

			_controller = newController;
		}

		public void PlaceZone(IMovie movie)
		{
			if (movie is TasMovie tasMovie)
			{
				tasMovie.ChangeLog.BeginNewBatch($"Place Macro at {Start}");
			}

			if (Start > movie.InputLogLength)
			{ 
				// Cannot place a frame here. Find a nice way around this.
				return;
			}

			if (!Replace && movie is TasMovie tasMovie2)
			{ // Can't be done with a regular movie.
				tasMovie2.InsertEmptyFrame(Start, Length);
			}

			if (Overlay)
			{
				for (int i = 0; i < Length; i++)
				{ // Overlay the frames.
					_controller.SetControllersAsMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					ORLatchFromSource(_targetController, movie.GetInputState(i + Start));
					movie.PokeFrame(i + Start, _targetController);
				}
			}
			else
			{
				for (int i = 0; i < Length; i++)
				{ // Copy over the frame.
					_controller.SetControllersAsMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					movie.PokeFrame(i + Start, _targetController);
				}
			}

			if (movie is TasMovie tasMovie3) // Assume TAStudio is open?
			{
				tasMovie3.ChangeLog.EndBatch();
				if (_emulator.Frame > Start)
				{
					// TODO: Go to start of macro? Ask TAStudio to do that?

					// TasMovie.InvalidateAfter(Start) [this is private]
					// Load last state, Emulate to Start

					// Or do this, if TAStudio has to be open.
					if (_tools.IsLoaded<TAStudio>())
					{
						_tools.TAStudio.GoToFrame(Start);
					}

					_tools.UpdateBefore();
					_tools.UpdateAfter();
				}
				else if (_tools.IsLoaded<TAStudio>())
				{
					_tools.TAStudio.UpdateValues();
				}
			}

			if (movie.InputLogLength >= _emulator.Frame)
			{
				movie.SwitchToPlay();
				Global.Config.MovieEndAction = MovieEndAction.Record;
			}
		}

		public void Save(string fileName)
		{
			// Save the controller definition/LogKey
			// Save the controller name and player count. (Only for the user.)
			// Save whether or not the macro should use overlay input, and/or replace
			string[] header = new string[4];
			header[0] = InputKey;
			header[1] = _emulator.ControllerDefinition.Name;
			header[2] = _emulator.ControllerDefinition.PlayerCount.ToString();
			header[3] = $"{Overlay},{Replace}";

			File.WriteAllLines(fileName, header);
			File.AppendAllLines(fileName, _log);
		}

		public MovieZone(string fileName, IEmulator emulator = null, ToolManager tools = null)
		{
			if (!File.Exists(fileName))
			{
				return;
			}

			_emulator = emulator;
			_tools = tools;

			string[] readText = File.ReadAllLines(fileName);

			// If the LogKey contains buttons/controls not accepted by the emulator,
			//	tell the user and display the macro's controller name and player count
			_inputKey = readText[0];
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			string key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");
			key = key.Substring(0, key.Length - 1);
			string[] emuKeys = key.Split('|');
			string[] macroKeys = _inputKey.Split('|');
			foreach (var macro in macroKeys)
			{
				if (!emuKeys.Contains(macro))
				{
					System.Windows.Forms.MessageBox.Show($"The selected macro is not compatible with the current emulator core.\nMacro controller: {readText[1]}\nMacro player count: {readText[2]}", "Error");
					return;
				}
			}

			// Settings
			string[] settings = readText[3].Split(',');
			Overlay = Convert.ToBoolean(settings[0]);
			Replace = Convert.ToBoolean(settings[1]);

			_log = new string[readText.Length - 4];
			readText.ToList().CopyTo(4, _log, 0, _log.Length);
			Length = _log.Length;
			Start = 0;

			Name = Path.GetFileNameWithoutExtension(fileName);

			// Adapters
			_targetController = new Bk2ControllerAdapter { Definition = _emulator.ControllerDefinition };
			_targetController.LatchFromSource(_targetController); // Reference and create all buttons
			string[] keys = _inputKey.Split('|');
			var d = new ControllerDefinition();
			foreach (var k in keys)
			{
				if (_emulator.ControllerDefinition.BoolButtons.Contains(k))
				{
					d.BoolButtons.Add(k);
				}
				else
				{
					d.FloatControls.Add(k);
				}
			}

			_controller = new Bk2ControllerAdapter { Definition = d };
		}

		#region Custom Latch

		private void LatchFromSourceButtons(Bk2ControllerAdapter latching, IController source)
		{
			foreach (string button in source.Definition.BoolButtons)
			{
				latching[button] = source.IsPressed(button);
			}

			foreach (string name in source.Definition.FloatControls)
			{
				latching.SetFloat(name, source.GetFloat(name));
			}
		}

		private void ORLatchFromSource(Bk2ControllerAdapter latching, IController source)
		{
			foreach (string button in latching.Definition.BoolButtons)
			{
				latching[button] |= source.IsPressed(button);
			}

			foreach (string name in latching.Definition.FloatControls)
			{
				float sFloat = source.GetFloat(name);
				int indexRange = source.Definition.FloatControls.IndexOf(name);
				if (sFloat == source.Definition.FloatRanges[indexRange].Mid)
				{
					latching.SetFloat(name, sFloat);
				}
			}
		}

		#endregion
	}
}
