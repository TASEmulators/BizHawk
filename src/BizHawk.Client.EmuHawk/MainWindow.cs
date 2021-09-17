using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainWindow : Form
    {
        public bool debugMode = false;
        public bool eventFoundPreventsTimer = false;
        public List<string> onEvent = new List<string>();
        public int numberOfRAMWritesOnEvent = 5;
        public int ramWriteMin = -1;
        public int ramWriteMax = -1;
        public string ramWriteDomain = "DEFAULT";
        public bool controlsReaderDebugMode = false;
        public int controlsReaderSecondsBetweenSwitches = 5;

        public string activeGamePath = "";
        public GameDetails activeGameDetails;
        public EventDefinition activeEventDef;

        public Random randomGenerator;

        public MainWindow()
        {
            InitializeComponent();

            randomGenerator = new Random();

            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    if (control.Tag.ToString() == "eventSelect")
                    {
                        control.Hide();
                    }
                }
            }

            ClearGameView();

            ParseEventsFolder();
        }



        public void ParseEventsFolder()
        {
            string[] fileList = Directory.GetFiles("_shuffletriggers");

            foreach(string fileName in fileList)
            {
                if (fileName.EndsWith(".txt"))
                {
                    string fileNameWithoutPath = fileName.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    string gameId = fileNameWithoutPath.Substring(0, fileNameWithoutPath.Length - 4);
                    this.GamesWithEventsListBox.Items.Add(gameId);
                }
            }
        }

        private void EventTriggerDefListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void GamesWithEventsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string gameToShow = this.GamesWithEventsListBox.SelectedItem.ToString();

            activeGamePath = "_shuffletriggers\\" + gameToShow + ".txt";

            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    if (control.Tag.ToString() == "eventSelect")
                    {
                        control.Show();
                    }
                }
            }

            ClearGameView();
            ShowActiveGameDetails();
		}       

        public void ClearGameView()
        {
            this.EventsThisGameListBox.Items.Clear();

            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    if (control.Tag.ToString() == "eventEdit")
                    {
                        control.Hide();
                    }
                }
            }
        }

        public void ShowControlsWithTag(string tag)
        {
            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    if (control.Tag.ToString() == tag)
                    {
                        control.Show();
                    }
                }
            }
        }

        public void HideControlsWithTag(string tag)
        {
            foreach (Control control in this.Controls)
            {
                if (control.Tag != null)
                {
                    if (control.Tag.ToString() == tag)
                    {
                        control.Hide();
                    }
                }
            }
        }

        public void ShowActiveGameDetails()
        {
            StreamReader gameReader = new StreamReader(activeGamePath);
            string gameSetupDetails = gameReader.ReadToEnd();
            gameReader.Close();

            //this.Text = "ShowActiveGameDetails: " + activeGamePath + ", " + gameSetupDetails;


            activeGameDetails = GameDetails.FromString(gameSetupDetails);
            activeGameDetails.filePath = activeGamePath;

            this.EventsThisGameListBox.Items.Clear();
            foreach (EventDefinition eventDefinition in activeGameDetails.events)
            {
                string name = eventDefinition.name;
                if (!eventDefinition.enabled)
                {
                    name += " (off)";
                }
                this.EventsThisGameListBox.Items.Add(name);
            }

			PopulateActiveGameLifeSettings();

		}

		public LifeCountDefinition GetActiveLifeCountDefinition()
		{
			LifeCountDefinition lifeCountDefinition = new LifeCountDefinition();

			if (activeGamePath != null)
			{
				string[] splitPath = activeGamePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
				string gameKey = splitPath[splitPath.Length - 1];
				if (gameKey.EndsWith(".txt"))
				{
					gameKey = gameKey.Substring(0, gameKey.Length - ".txt".Length);
				}

				if (RomLoader.gameTriggers.ContainsKey(gameKey))
				{
					Debug.WriteLine("Found game triggers for " + gameKey);
					if (RomLoader.gameTriggers[gameKey].lifeCountDefinition != null)
					{
						lifeCountDefinition = RomLoader.gameTriggers[gameKey].lifeCountDefinition;
						Debug.WriteLine("Found lifeCountDefinition triggers for " + gameKey + "\n" + lifeCountDefinition.GetStringValue());
					}
					else
					{
						Debug.WriteLine("Did not find lifeCountDefinition triggers for " + gameKey);
						RomLoader.gameTriggers[gameKey].lifeCountDefinition = lifeCountDefinition;
					}
				}
				else
				{
					Debug.WriteLine("Did not find game triggers for " + gameKey);

					GameTriggerDefinition gameTriggerDefinition = new GameTriggerDefinition();
					gameTriggerDefinition.lifeCountDefinition = lifeCountDefinition;
					RomLoader.gameTriggers.Add(gameKey, gameTriggerDefinition);
				}
			} else
			{
				Debug.WriteLine("activeGamePath is null");

			}

			return lifeCountDefinition;
		}

		public void PopulateActiveGameLifeSettings()
		{
			Debug.WriteLine("!! PopulateActiveGameLifeSettings");

			LifeCountDefinition lifeCountDefinition = GetActiveLifeCountDefinition();

			livesFrameCount.Text = lifeCountDefinition.period.ToString();
			for (int i = 0; i < 4; i++)
			{
				foreach (Control control in this.Controls)
				{
					if (control.Name == "livesBytes" + i.ToString())
					{
						if (lifeCountDefinition.bytes.Count > i)
						{
							control.Text = lifeCountDefinition.bytes[i].ToString("X4");
						}
						else
						{
							control.Text = "";
						}
						Debug.WriteLine("   set " + control.Name + " to " + control.Text);
					}

					if (control.Name == "livesValue" + i.ToString())
					{
						if (lifeCountDefinition.values.Count > i)
						{
							control.Text = lifeCountDefinition.values[i].ToString("X2");
						} else
						{
							control.Text = "";
						}
						Debug.WriteLine("   set " + control.Name + " to " + control.Text);
					}

					if (control.Name == "livesDomain" + i.ToString())
					{
						if (lifeCountDefinition.domains.Count > i)
						{
							control.Text = lifeCountDefinition.domains[i];
						}
						else
						{
							control.Text = "";
						}
						Debug.WriteLine("   set " + control.Name + " to " + control.Text);
					}
				}
			}
		}

		public void SaveLifeSettings()
		{
			LifeCountDefinition lifeCountDefinition = GetActiveLifeCountDefinition();
			int.TryParse(livesFrameCount.Text, out lifeCountDefinition.period);

			for (int i = 0; i < 4; i++)
			{
				foreach (Control control in this.Controls)
				{
					if (control.Text.Length == 0)
					{
						continue;
					}

					if (control.Name == "livesBytes" + i.ToString())
					{
						while (lifeCountDefinition.bytes.Count <= i)
						{
							lifeCountDefinition.bytes.Add(0);
						}
						int value = lifeCountDefinition.bytes[i];
						if (int.TryParse(control.Text, System.Globalization.NumberStyles.HexNumber, null, out value))
						{
							lifeCountDefinition.bytes[i] = value;
						}
					}

					if (control.Name == "livesValue" + i.ToString())
					{
						while (lifeCountDefinition.values.Count <= i)
						{
							lifeCountDefinition.values.Add(0);
						}
						int value = lifeCountDefinition.values[i];
						if (int.TryParse(control.Text, System.Globalization.NumberStyles.HexNumber, null, out value))
						{
							lifeCountDefinition.values[i] = value % 0x100;
						}
					}

					if (control.Name == "livesDomain" + i.ToString())
					{
						while (lifeCountDefinition.domains.Count <= i)
						{
							lifeCountDefinition.domains.Add("DEFAULT");
						}
						lifeCountDefinition.domains[i] = control.Text;
					}
				}
			}

			string[] splitPath = activeGamePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
			string gameKey = splitPath[splitPath.Length - 1];
			if (gameKey.EndsWith(".txt"))
			{
				gameKey = gameKey.Substring(0, gameKey.Length - ".txt".Length);
			}
			string path = ".//_lifesettings//" + gameKey + ".txt";

			Debug.WriteLine("WRITING LIFE SETTINGS: " + path + "\n" + lifeCountDefinition.GetStringValue());
			File.WriteAllText(path, lifeCountDefinition.GetStringValue());
		}

		public void RefreshEventList()
        {

        }

        private void EventsThisGameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.EventsThisGameListBox.SelectedItem != null)
            {
                string eventId = this.EventsThisGameListBox.SelectedItem.ToString();
                if (eventId.EndsWith(" (off)"))
                {
                    eventId = eventId.Substring(0, eventId.Length - 6);
                }
                PopulateGameDetails(eventId);
            }
        }

        public void PopulateGameDetails(string selectedEventId)
        {
            //this.Text = "PopulateGameDetails " + selectedEventId;

            foreach (EventDefinition eventDefinition in activeGameDetails.events)
            {
                if (eventDefinition.name == selectedEventId)
                {
                    activeEventDef = eventDefinition;
                    ShowControlsWithTag("eventEdit");
                    ShowEventDetails(eventDefinition);
                    return;
                }
            }

            //this.Text = "DID NOT FIND " + selectedEventId;
        }

        public void ShowEventDetails(EventDefinition def)
        {
            this.EventNameTextBox.Text = def.name;
            this.EventEnabledCheckBox.Checked = def.enabled;
            this.RAMValuesToCheckTextBox.Text = def.bytesDef;
            this.EventRAMBaseComboBox.Text = def.baseDef;

            this.RAMMinChangeNumericInput.Value = def.minChange;
            this.RAMMaxChangeNumericInput.Value = def.maxChange;

            this.EventDelayNumericInput.Value = def.delay;

			this.blockZeroCheckBox.Checked = def.blockFromZero;

			this.EventSteppedChangeEventBufferNumericInput.Value = def.steppedChangeEventBuffer;

			this.EventDomainComboBox.Text = def.domain;

            PopulateExample(def);

            //this.EventCalculationExample.Text = randomGenerator.Next(1000, 9999).ToString();
        }

        public void PopulateExample(EventDefinition def)
        {
            string exampleText = "";
            string[] bytes = def.bytesDef.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (bytes.Length == 0)
            {
                this.EventCalculationExample.Text = "Input bytes to be read from";
                return;
            }

            int targetValue = 1;
            string hexTarget = "";
            string endingString = "";
            List<string> calculationComponents = new List<string>();

            if (def.baseDef == "256")
            {
                string runningString = "";
                if (bytes.Length == 1)
                {
                    runningString = "FF";
                } else
                {
                    for (int i = 0; i < bytes.Length && i < 4; i++)
                    {
                        runningString += (i + 1).ToString().PadLeft(2, '0');
                    }
                }

                int whichByte = 0;
                for (int i = runningString.Length - 2; i >= 0; i -= 2)
                {
                    string line = "0x" + bytes[whichByte] + " = " + "0x" + runningString.Substring(i, 2);
                    calculationComponents.Add(line);
                    whichByte++;
                }

                hexTarget = " (0x" + runningString + " in hex) ";
                targetValue = int.Parse(runningString, System.Globalization.NumberStyles.HexNumber, null);
            }
            if (def.baseDef == "100")
            {
                string runningString = "";
            
                for (int i = 0; i < bytes.Length * 2; i++)
                {
                    runningString += ((i + 1) % 10).ToString();
                }

                int whichByte = 0;
                for (int i = runningString.Length - 2; i >= 0; i -= 2)
                {
                    string line = "0x" + bytes[whichByte] + " = " + "0x" + runningString.Substring(i, 2);
                    calculationComponents.Add(line);
                    whichByte++;
                }

                targetValue = int.Parse(runningString);
                if (bytes.Length <= 2)
                {
                    endingString = "\n(Each byte 0xNN represents two decimal digits,\nfor N between 0 and 9)";
                }
            }
            if (def.baseDef == "10")
            {
                string runningString = "";
                for (int i = 0; i < bytes.Length; i++)
                {
                    runningString += ((i + 1) % 10).ToString();
                }

                int whichByte = 0;
                for (int i = runningString.Length - 1; i >= 0; i--)
                {
                    string line = "0x" + bytes[whichByte] + " = " + "0x0" + runningString.Substring(i, 1);
                    calculationComponents.Add(line);
                    whichByte++;
                }

                targetValue = int.Parse(runningString);
                if (bytes.Length <= 2)
                {
                    endingString = "\n(Each byte represents a digit from 0 to 9)";
                }
            }

            exampleText += "The value " + targetValue.ToString() + hexTarget + " is represented as:\n" + string.Join("\n", calculationComponents) + endingString;

            this.EventCalculationExample.Text = exampleText;
        }


        public void UpdateCurrentGame()
        {
            if (activeEventDef != null)
            {
                activeEventDef.name = this.EventNameTextBox.Text;

                activeEventDef.bytesDef = this.RAMValuesToCheckTextBox.Text;
                activeEventDef.baseDef = this.EventRAMBaseComboBox.Text;
                if (!this.EventRAMBaseComboBox.Items.Contains(this.EventRAMBaseComboBox.Text))
                {
                    this.EventRAMBaseComboBox.BackColor = Color.Red;
                }
                else
                {
                    this.EventRAMBaseComboBox.BackColor = Color.White;
                }

                activeEventDef.enabled = this.EventEnabledCheckBox.Checked;
                activeEventDef.minChange = (int)this.RAMMinChangeNumericInput.Value;
                activeEventDef.maxChange = (int)this.RAMMaxChangeNumericInput.Value;
                activeEventDef.delay = (int)this.EventDelayNumericInput.Value;

				activeEventDef.blockFromZero = this.blockZeroCheckBox.Checked;

				activeEventDef.steppedChangeEventBuffer = (int)this.EventSteppedChangeEventBufferNumericInput.Value;

                activeEventDef.domain = this.EventDomainComboBox.Text;
                if (!this.EventDomainComboBox.Items.Contains(this.EventDomainComboBox.Text))
                {
                    this.EventDomainComboBox.BackColor = Color.Red;
                }
                else
                {
                    this.EventDomainComboBox.BackColor = Color.White;
                }
            }

            if (this.activeGameDetails != null)
            {
                this.activeGameDetails.SaveToDisk();
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void EventRAMBaseComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (activeEventDef != null)
            {
                PopulateExample(activeEventDef);
            }
        }

        private void EventNameTextBox_TextChanged(object sender, EventArgs e)
        {
        }

        private void EventEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void RAMValuesToCheckTextBox_TextChanged(object sender, EventArgs e)
        {
            if (activeEventDef != null)
            {
                PopulateExample(activeEventDef);
            }
        }

        private void RAMMinChangeNumericInput_ValueChanged(object sender, EventArgs e)
        {
        }

        private void EventDelayNumericInput_ValueChanged(object sender, EventArgs e)
        {
        }

        private void EventSteppedChangeEventBufferNumericInput_ValueChanged(object sender, EventArgs e)
        {
        }

        private void ControlReaderEffectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void EventDomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void SaveEventChangesButton_Click(object sender, EventArgs e)
        {
            UpdateCurrentGame();

            int selectedIndex = this.EventsThisGameListBox.SelectedIndex;

            ShowActiveGameDetails();

            this.EventsThisGameListBox.SelectedIndex = selectedIndex;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string gameId = RomLoader.GetActiveGameFileHandle();

            bool hasFound = false;

            List<object> items = new List<object>();
            foreach (object item in this.GamesWithEventsListBox.Items)
            {
                items.Add(item);
            }

            foreach (object item in items)
            {
                if (item.ToString() == gameId)
                {
                    this.GamesWithEventsListBox.SelectedItem = item;
                    hasFound = true;
                }
            }

            if (!hasFound && gameId != "NONE")
            {
                StreamWriter gameWriter = new StreamWriter(".\\_shuffletriggers\\" + gameId + ".txt");
                gameWriter.Write("event0>");
                gameWriter.Close();

                this.GamesWithEventsListBox.Items.Add(gameId);
                this.GamesWithEventsListBox.SelectedIndex = this.GamesWithEventsListBox.Items.Count - 1;
            }
        }

        private void AddNewEventButton_Click(object sender, EventArgs e)
        {
            if (activeGameDetails != null)
            {
                int eventCount = this.EventsThisGameListBox.Items.Count;

                EventDefinition newEvent = EventDefinition.FromString("event" + eventCount.ToString());
                activeGameDetails.events.Add(newEvent);
                activeGameDetails.SaveToDisk();

                this.ShowActiveGameDetails();

                this.EventsThisGameListBox.SelectedIndex = this.EventsThisGameListBox.Items.Count - 1;
            }
        }

        private void ResetGameToDefaultButton_Click(object sender, EventArgs e)
        {
            if (this.activeGameDetails != null)
            {
                string defaultPath = activeGameDetails.filePath.Replace("Events\\", "Events\\Defaults\\");
                if (!File.Exists(defaultPath))
                {
                    return;
                }

                StreamReader gameReader = new StreamReader(defaultPath);
                string gameDetails = gameReader.ReadLine();
                gameReader.Close();

                GameDetails newDetails = GameDetails.FromString(gameDetails);

                this.Text = defaultPath;

                newDetails.filePath = activeGameDetails.filePath;
                newDetails.SaveToDisk();

                this.activeGameDetails = newDetails;

                // force a reselect of this game
                this.HideControlsWithTag("eventSelect");
                this.HideControlsWithTag("eventEdit");
            }
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void RemoveEventButton_Click(object sender, EventArgs e)
        {
            if (this.activeGameDetails != null)
            {
                if (this.EventsThisGameListBox.SelectedItem != null)
                {
                    this.activeGameDetails.RemoveEventAtIndex(this.EventsThisGameListBox.SelectedIndex);
                   
                }
                this.ShowActiveGameDetails();
                this.HideControlsWithTag("eventEdit");
            }
        }

		private void saveLivesSettingsButton_Click(object sender, EventArgs e)
		{
			SaveLifeSettings();
			RomLoader.InitialiseShuffler(true);
			PopulateActiveGameLifeSettings();
		}

		private void horizontalLine1_Click(object sender, EventArgs e)
		{

		}

		private void blockZeroCheckBox_CheckedChanged(object sender, EventArgs e)
		{

		}
	}
}

public class GameDetails {
    public string filePath;
    public List<EventDefinition> events = new List<EventDefinition>();

    public static GameDetails FromString(string sourceString)
    {
        GameDetails game = new GameDetails();

        string[] lines = sourceString.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
			if (line == "---")
			{
				break;
			}
            game.events.Add(EventDefinition.FromString(line));
        }

        return game;
    }

    public void RemoveEventAtIndex(int index)
    {
        if (index >= 0 && index < events.Count)
        {
            events.RemoveAt(index);
            SaveToDisk();
        }
    }

    public void SaveToDisk()
    {
        List<string> linesToWrite = new List<string>();

        foreach(EventDefinition eventDefinition in events)
        {
            linesToWrite.Add(eventDefinition.GetSaveDataString());
        }

        StreamWriter settingsWriter = new StreamWriter(filePath);
        settingsWriter.Write(string.Join("\n", linesToWrite));
        settingsWriter.Close();

		RomLoader.InitialiseShuffler(true);
	}
}

public class EventDefinition
{
    public string name = "";
    public string bytesDef = "";
    public string baseDef = "256";
    public int minChange = 0;
    public int maxChange = int.MaxValue;
    public int delay = 0;
    public string domain = "DEFAULT";
    public string controlOutput = "PRESS";
    public bool enabled = true;
    public bool isTracker = false;
    public int trackerDivisor = 1;
	public bool blockFromZero = false;
	public int steppedChangeEventBuffer = 0;
    public static EventDefinition FromString(string sourceString)
    {
        EventDefinition eventDefinition = new EventDefinition();

        string[] arrowComponents = sourceString.Split(new string[] { ">" }, StringSplitOptions.None);
        eventDefinition.name = arrowComponents[0];

        if (arrowComponents.Length > 1)
        {
            string[] elements = arrowComponents[1].Split(new string[] { "/" }, StringSplitOptions.None);
            foreach(string element in elements)
            {
                // decode bytesDef etc
                string[] components = element.Split(new string[] { ":" }, StringSplitOptions.None);
                if (components.Length > 1)
                {
                    switch (components[0])
                    {
                        case "bytes":
                            eventDefinition.bytesDef = components[1];
                            break;
                        case "base":
                            eventDefinition.baseDef = components[1];
                            break;
                        case "minChange":
                            int.TryParse(components[1], out eventDefinition.minChange);
                            break;
                        case "maxChange":
                            int.TryParse(components[1], out eventDefinition.maxChange);
                            break;
                        case "delay":
                            int.TryParse(components[1], out eventDefinition.delay);
                            break;
                        case "domain":
                            eventDefinition.domain = components[1];
                            break;
                        case "controlOutput":
                            eventDefinition.controlOutput = components[1];
                            break;
                        case "enabled":
                            eventDefinition.enabled = components[1].ToUpper() == "TRUE";
                            break;
                        case "isTracker":
                            eventDefinition.isTracker = components[1].ToUpper() == "TRUE";
                            break;
                        case "trackerDivisor":
                            int.TryParse(components[1], out eventDefinition.trackerDivisor);
                            break;
						case "blockFromZero":
							eventDefinition.blockFromZero = components[1].ToUpper() == "TRUE";
							break;
						case "steppedChangeEventBuffer":
							int.TryParse(components[1], out eventDefinition.steppedChangeEventBuffer);
							break;
					}
                }
            }
        }

        return eventDefinition;
    }

    public string GetSaveDataString()
    {
        List<string> fields = new List<string>();

        SanitiseBytes();
        fields.Add("bytes:" + bytesDef);
        fields.Add("base:" + baseDef);
        fields.Add("minChange:" + minChange.ToString());
        fields.Add("maxChange:" + maxChange.ToString());
        fields.Add("delay:" + delay.ToString());
        fields.Add("domain:" + domain);
        fields.Add("controlOutput:" + controlOutput);
        fields.Add("enabled:" + enabled.ToString());
        fields.Add("isTracker:" + isTracker.ToString());
        fields.Add("trackerDivisor:" + trackerDivisor.ToString());
		fields.Add("blockFromZero:" + blockFromZero.ToString());
		fields.Add("steppedChangeEventBuffer:" + steppedChangeEventBuffer.ToString());

		return name + ">" + string.Join("/", fields);

    }

    public void SanitiseBytes()
    {
        string newBytes = "";
        string[] allowedCharacters = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "," };


        for (int i = 0; i < bytesDef.Length; i++)
        {
            string character = bytesDef.ToUpper().Substring(i, 1);
            if (allowedCharacters.Contains(character))
            {
                newBytes += character;
            }
        }

        bytesDef = newBytes;
    }
}