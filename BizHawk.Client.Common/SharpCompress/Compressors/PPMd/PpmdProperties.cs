using SharpCompress.Compressors.PPMd.I1;
using SharpCompress.Converters;

namespace SharpCompress.Compressors.PPMd
{
    public class PpmdProperties
    {

        private int _allocatorSize;
        internal Allocator _allocator;

        public PpmdProperties()
            : this(16 << 20, 6)
        {
        }

        public PpmdProperties(int allocatorSize, int modelOrder)
            : this(allocatorSize, modelOrder, ModelRestorationMethod.Restart)
        {
        }

        internal PpmdProperties(int allocatorSize, int modelOrder, ModelRestorationMethod modelRestorationMethod)
        {
            AllocatorSize = allocatorSize;
            ModelOrder = modelOrder;
            RestorationMethod = modelRestorationMethod;
        }
        
        public int ModelOrder { get; }
        public PpmdVersion Version { get; } = PpmdVersion.I1;
        internal ModelRestorationMethod RestorationMethod { get; }

        public PpmdProperties(byte[] properties)
        {
            if (properties.Length == 2)
            {
                ushort props = DataConverter.LittleEndian.GetUInt16(properties, 0);
                AllocatorSize = (((props >> 4) & 0xff) + 1) << 20;
                ModelOrder = (props & 0x0f) + 1;
                RestorationMethod = (ModelRestorationMethod)(props >> 12);
            }
            else if (properties.Length == 5)
            {
                Version = PpmdVersion.H7Z;
                AllocatorSize = DataConverter.LittleEndian.GetInt32(properties, 1);
                ModelOrder = properties[0];
            }
        }

        public int AllocatorSize
        {
            get => _allocatorSize;
            set
            {
                _allocatorSize = value;
                if (Version == PpmdVersion.I1)
                {
                    if (_allocator == null)
                    {
                        _allocator = new Allocator();
                    }
                    _allocator.Start(_allocatorSize);
                }
            }
        }

        public byte[] Properties => DataConverter.LittleEndian.GetBytes(
                                                                        (ushort)
                                                                        ((ModelOrder - 1) + (((AllocatorSize >> 20) - 1) << 4) + ((ushort)RestorationMethod << 12)));
    }
}