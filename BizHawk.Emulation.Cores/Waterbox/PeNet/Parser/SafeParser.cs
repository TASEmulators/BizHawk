/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using System;

namespace PeNet.Parser
{
    internal abstract class SafeParser<T>
        where T : class 
    {
        protected readonly byte[] _buff;
        protected readonly uint _offset;
        private bool _alreadyParsed;

        private T _target;

        internal SafeParser(byte[] buff, uint offset)
        {
            _buff = buff;
            _offset = offset;
        }

        private bool SanityCheckFailed()
        {
            return _offset > _buff?.Length;
        }

        public Exception ParserException { get; protected set; }

        protected abstract T ParseTarget();

        public T GetParserTarget()
        {
            if (_alreadyParsed)
                return _target;

            _alreadyParsed = true;

            if (SanityCheckFailed())
                return null;

            try
            {
                _target = ParseTarget();
            }
            catch (Exception exception)
            {
                ParserException = exception;
            }

            return _target;
        }
    }
}