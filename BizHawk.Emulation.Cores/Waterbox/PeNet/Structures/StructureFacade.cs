namespace PeNet.Structures
{
    internal class StructureFacade
    {
        private byte[] _buff;
        private StructureFactory<IMAGE_DOS_HEADER> _imageDosHeaderFactory;
        private StructureFactory<IMAGE_NT_HEADERS> _imageNtHeadersFactory;
        

        public StructureFacade(byte[] buff)
        {
            _buff = buff;
            _imageDosHeaderFactory = new StructureFactory<IMAGE_DOS_HEADER>(buff, 0);
        }

        public IMAGE_DOS_HEADER GetImageDosHeader()
        {
            return _imageDosHeaderFactory.GetInstance();
        }
    }
}