using System;

namespace SharpCompress
{
    public class Lazy<T>
    {
        private readonly Func<T> _lazyFunc;
        private bool _evaluated;
        private T _value;
        
        public Lazy(Func<T> lazyFunc)
        {
            _lazyFunc = lazyFunc;
        }

        public T Value
        {
            get
            {
                if (!_evaluated)
                {
                    _value = _lazyFunc();
                    _evaluated = true;
                }
                return _value;
            }
        }
    }
}