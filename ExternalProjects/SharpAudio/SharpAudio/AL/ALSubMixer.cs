namespace SharpAudio.AL
{
    internal sealed class ALSubmixer : Submixer
    {
        public ALSubmixer(ALEngine engine)
        {
        }

        public override float Volume
        {
            get { return _volume; }
            set { _volume = value; }
        }

        public override void Dispose()
        {
        }
    }
}
