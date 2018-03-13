using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
    [Schema("ZXSpectrum")]
    class ZXSpectrumSchema : IVirtualPadSchema
    {
        public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
        {
            yield return Joystick(1);
            yield return Joystick(2);
            yield return Joystick(3);
            yield return Keyboard();
            //yield return TapeDevice();
        }

        private static PadSchema Joystick(int controller)
        {
            return new PadSchema
            {
                DisplayName = "Joystick " + controller,
                IsConsole = false,
                DefaultSize = new Size(174, 74),
                MaxSize = new Size(174, 74),                
                Buttons = new[]
                {
                    new PadSchema.ButtonSchema
                    {
                        Name = "P" + controller + " Up",
                        DisplayName = "",
                        Icon = Properties.Resources.BlueUp,
                        Location = new Point(23, 15),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "P" + controller + " Down",
                        DisplayName = "",
                        Icon = Properties.Resources.BlueDown,
                        Location = new Point(23, 36),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "P" + controller + " Left",
                        DisplayName = "",
                        Icon = Properties.Resources.Back,
                        Location = new Point(2, 24),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "P" + controller + " Right",
                        DisplayName = "",
                        Icon = Properties.Resources.Forward,
                        Location = new Point(44, 24),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "P" + controller + " Button",
                        DisplayName = "B",
                        Location = new Point(124, 24),
                        Type = PadSchema.PadInputType.Boolean
                    }
                }
            };
        }

        private class ButtonLayout
        {
            public string Name { get; set; }
            public string DisName { get; set; }
            public double WidthFactor { get; set; }
            public int Row { get; set; }
            public bool IsActive = true;
        }

        private static PadSchema Keyboard()
        {
            List<ButtonLayout> bls = new List<ButtonLayout>
            {
                new ButtonLayout { Name = "Key True Video", DisName = "TV", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Inv Video", DisName = "IV", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 1", DisName = "1", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 2", DisName = "2", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 3", DisName = "3", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 4", DisName = "4", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 5", DisName = "5", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 6", DisName = "6", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 7", DisName = "7", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 8", DisName = "8", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 9", DisName = "9", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key 0", DisName = "0", Row = 0, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Break", DisName = "BREAK", Row = 0, WidthFactor = 1.5 },

                new ButtonLayout { Name = "Key Delete", DisName = "DEL", Row = 1, WidthFactor = 1.5 },
                new ButtonLayout { Name = "Key Graph", DisName = "GR", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Q", DisName = "Q", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key W", DisName = "W", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key E", DisName = "E", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key R", DisName = "R", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key T", DisName = "T", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Y", DisName = "Y", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key U", DisName = "U", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key I", DisName = "I", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key O", DisName = "O", Row = 1, WidthFactor = 1 },
                new ButtonLayout { Name = "Key P", DisName = "P", Row = 1, WidthFactor = 1 },

                new ButtonLayout { Name = "Key Extend Mode", DisName = "EM", Row = 2, WidthFactor = 1.5 },
                new ButtonLayout { Name = "Key Edit", DisName = "ED", Row = 2, WidthFactor = 1.25},
                new ButtonLayout { Name = "Key A", DisName = "A", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key S", DisName = "S", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key D", DisName = "D", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key F", DisName = "F", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key G", DisName = "G", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key H", DisName = "H", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key J", DisName = "J", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key K", DisName = "K", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key L", DisName = "L", Row = 2, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Return", DisName = "ENTER", Row = 2, WidthFactor = 1.75 },

                new ButtonLayout { Name = "Key Caps Shift", DisName = "CAPS-S", Row = 3, WidthFactor = 2.25 },
                new ButtonLayout { Name = "Key Caps Lock", DisName = "CL", Row = 3, WidthFactor = 1},
                new ButtonLayout { Name = "Key Z", DisName = "Z", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key X", DisName = "X", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key C", DisName = "C", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key V", DisName = "V", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key B", DisName = "B", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key N", DisName = "N", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key M", DisName = "M", Row = 3, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Period", DisName = ".", Row = 3, WidthFactor = 1},
                new ButtonLayout { Name = "Key Caps Shift", DisName = "CAPS-S", Row = 3, WidthFactor = 2.25 },

                new ButtonLayout { Name = "Key Symbol Shift", DisName = "SS", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Semi-Colon", DisName = ";", Row = 4, WidthFactor = 1},
                new ButtonLayout { Name = "Key Quote", DisName = "\"", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Left Cursor", DisName = "←", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Right Cursor", DisName = "→", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Space", DisName = "SPACE", Row = 4, WidthFactor = 4.5 },
                new ButtonLayout { Name = "Key Up Cursor", DisName = "↑", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Down Cursor", DisName = "↓", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Comma", DisName = ",", Row = 4, WidthFactor = 1 },
                new ButtonLayout { Name = "Key Symbol Shift", DisName = "SS", Row = 4, WidthFactor = 1 },
            };

            PadSchema ps = new PadSchema
            {
                DisplayName = "Keyboard",
                IsConsole = false,
                DefaultSize = new Size(500, 170)
            };

            List<PadSchema.ButtonSchema> btns = new List<PadSchema.ButtonSchema>();

            int rowHeight = 29; //24
            int stdButtonWidth = 29; //24
            int yPos = 18;
            int xPos = 22;
            int currRow = 0;

            foreach (var b in bls)
            {
                if (b.Row > currRow)
                {
                    currRow++;
                    yPos += rowHeight;
                    xPos = 22;
                }

                int txtLength = b.DisName.Length;
                int btnSize = System.Convert.ToInt32((double)stdButtonWidth * b.WidthFactor);
                

                string disp = b.DisName;
                if (txtLength == 1)
                    disp = " " + disp;
                
                switch(b.DisName)
                {
                    case "SPACE": disp = "            " + disp + "            "; break;
                    case "I": disp = " " + disp + " "; break;
                    case "W": disp = b.DisName; break;
                }
                
                    
                if (b.IsActive)
                {
                    PadSchema.ButtonSchema btn = new PadSchema.ButtonSchema();
                    btn.Name = b.Name;
                    btn.DisplayName = disp;
                    btn.Location = new Point(xPos, yPos);
                    btn.Type = PadSchema.PadInputType.Boolean;
                    btns.Add(btn);
                }
                                
                xPos += btnSize;
            }

            ps.Buttons = btns.ToArray();
            return ps;
        }

        private static PadSchema TapeDevice()
        {
            return new PadSchema
            {
                DisplayName = "DATACORDER",
                IsConsole = false,
                DefaultSize = new Size(174, 74),
                MaxSize = new Size(174, 74),
                Buttons = new[]
                {
                    new PadSchema.ButtonSchema
                    {
                        Name = "Play Tape",
                        Icon = Properties.Resources.Play,
                        Location = new Point(23, 22),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "Stop Tape",
                        Icon = Properties.Resources.Stop,
                        Location = new Point(53, 22),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "RTZ Tape",
                        Icon = Properties.Resources.BackMore,
                        Location = new Point(83, 22),
                        Type = PadSchema.PadInputType.Boolean
                    },
                     new PadSchema.ButtonSchema
                    {
                        Name = "Insert Next Tape",
                        DisplayName = "NEXT TAPE",
                        //Icon = Properties.Resources.MoveRight,
                        Location = new Point(23, 52),
                        Type = PadSchema.PadInputType.Boolean
                    },
                    new PadSchema.ButtonSchema
                    {
                        Name = "Insert Previous Tape",
                        DisplayName = "PREV TAPE",
                        //Icon = Properties.Resources.MoveLeft,
                        Location = new Point(100, 52),
                        Type = PadSchema.PadInputType.Boolean
                    },
                                  
                }
            };
        }
    }
}
