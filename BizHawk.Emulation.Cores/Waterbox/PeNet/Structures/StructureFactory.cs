using System;

namespace PeNet.Structures
{
    internal class StructureFactory<T>
        where T : AbstractStructure
    {
        private T _instance;
        private bool _instanceAlreadyParsed;
        private byte[] _buff;
        private uint _offset;

        public Exception ParseException { get; private set; }

        public StructureFactory(byte[] buff, uint offset)
        {
            _buff = buff;
            _offset = offset;
        }

        private T CreateInstance<T>(byte[] buff, uint offset)
            where T : AbstractStructure
        {
            T instance = null;

            try
            {
                instance = Activator.CreateInstance(typeof(T), buff, offset) as T;
            }
            catch (Exception exception)
            {
                ParseException = exception;
            }

            return instance;
        }

        public T GetInstance()
        {
            // The structure was already parsed. Return the result.
            if (_instanceAlreadyParsed)
                return _instance;

            // The structure wasn't parsed before. Create and save a new
            // instance and return it.
            _instanceAlreadyParsed = true;
            _instance = CreateInstance<T>(_buff, _offset);

            return _instance;
        }
    }
}