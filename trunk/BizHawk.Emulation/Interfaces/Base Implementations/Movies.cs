using System.IO;

namespace BizHawk
{
    public class InputRecorder : IController
    {
        private IController baseController;
        private BinaryWriter writer;

        public InputRecorder(IController baseController, BinaryWriter writer)
        {
            this.baseController = baseController;
            this.writer = writer;
        }

        public void CloseMovie()
        {
            writer.Close();
        }

        public ControllerDefinition Type
        {
            get { return baseController.Type; }
        }

        public bool this[string name]
        {
            get { return baseController[name]; }
        }

        public bool IsPressed(string name)
        {
            return baseController[name];
        }

        public float GetFloat(string name)
        {
            return baseController.GetFloat(name);
        }

        public void UnpressButton(string name)
        {
            baseController.UnpressButton(name);
        }

        private int frame;
        public int FrameNumber
        {
            get { return frame; }
            set
            {
                if (frame != value)
                {
                    frame = value;
                    RecordFrame();
                } 
                baseController.FrameNumber = value;
            }
        }

        private void RecordFrame()
        {
            int encodedValue = 0;
            for (int i=0; i<Type.BoolButtons.Count; i++)
            {
                if (baseController[Type.BoolButtons[i]])
                {
                    encodedValue |= (1 << i);
                }
            }
            writer.Seek(frame*2, SeekOrigin.Begin);
            writer.Write((ushort)encodedValue);
        }
    }

    public class InputPlayback : IController
    {
        private ControllerDefinition def;
        private int[] input;

        public InputPlayback(ControllerDefinition controllerDefinition, BinaryReader reader)
        {
            def = controllerDefinition;
            int numFrames = (int) (reader.BaseStream.Length/2);
            input = new int[numFrames];
            for (int i=0; i<numFrames; i++)
                input[i] = reader.ReadUInt16();
        }

        public ControllerDefinition Type
        {
            get { return def; }
        }

        public bool this[string name]
        {
            get { return IsPressed(name); }
        }

        public bool IsPressed(string name)
        {
            if (FrameNumber >= input.Length)
                return false;

            for (int i = 0; i < def.BoolButtons.Count; i++)
            {
                if (def.BoolButtons[i] == name)
                {
                    return (input[FrameNumber] & (1 << i)) != 0;
                }
            }
            return false;
        }

        public float GetFloat(string name)
        {
            throw new System.NotImplementedException();
        }

        public void UnpressButton(string name) {}
        public int FrameNumber { get; set; }

        public bool MovieEnded { get { return FrameNumber >= input.Length; } }
    }
}