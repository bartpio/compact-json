using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactJson
{
    /// <summary>
    /// wraps an enumerator, provides an easy peek at the first row
    /// </summary>
    /// <typeparam name="T">type of datum</typeparam>
    public sealed class PeekingEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private readonly IEnumerator<T> _wrapped;
        private readonly T _peeked;
        private bool _peekstate = true;
        private readonly bool _nonempty;

        /// <summary>
        /// construct
        /// </summary>
        /// <param name="wrapped">enumerator to wrap</param>
        public PeekingEnumerator(IEnumerator<T> wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));

            // now let's peek
            _nonempty = _wrapped.MoveNext();
            if (_nonempty)
            {
                _peeked = _wrapped.Current;
            }
        }

        /// <summary>
        /// is the wrapped enumerator empty?
        /// </summary>
        public bool Empty => !_nonempty;

        /// <summary>
        /// first item provided by the wrapped enumerator
        /// </summary>
        public T First => _peeked;

        /// <summary>
        /// current item
        /// </summary>
        public T Current => _peekstate ? default : _wrapped.Current;

        /// <summary>
        /// current item
        /// </summary>
        object IEnumerator.Current => _peekstate ? null : (object)_wrapped.Current;

        /// <summary>
        /// disposal
        /// </summary>
        public void Dispose() => _wrapped.Dispose();

        /// <summary>
        /// move on to the next item
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_peekstate)
            {
                _peekstate = false;
                return _nonempty;
            }
            else
            {
                return _wrapped.MoveNext();
            }
        }

        /// <summary>
        /// reset enumerator
        /// </summary>
        public void Reset()
        {
            _wrapped.Reset();
            _peekstate = false;
        }
    }
}
