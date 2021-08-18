using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactJson
{
    /// <summary>
    /// wraps an enumerable, provides an easy peek at the first row
    /// </summary>
    /// <typeparam name="T">type of datum</typeparam>
    public sealed class PeekingEnumerable<T> : IEnumerable<T>, IEnumerable, IDisposable
    {
        private readonly IEnumerable<T> _wrapped;
        private PeekingEnumerator<T> _ownEnumerator;
        private IDisposable _toDispose;

        /// <summary>
        /// is the wrapped enumerable empty?
        /// </summary>
        public bool Empty { get; }

        /// <summary>
        /// first item of the wrapped enumerable
        /// </summary>
        public T First { get; }

        /// <summary>
        /// number of times enumerators have been issued
        /// primarily for test purposes
        /// </summary>
        public long EnumeratorCount { get; private set; } = 1;

        /// <summary>
        /// construct
        /// </summary>
        /// <param name="wrapped">enumerable to wrap</param>
        public PeekingEnumerable(IEnumerable<T> wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            _ownEnumerator = new PeekingEnumerator<T>(_wrapped.GetEnumerator());
            Empty = _ownEnumerator.Empty;
            First = _ownEnumerator.First;
        }

        /// <summary>
        /// disposal
        /// </summary>
        public void Dispose()
        {
            _ownEnumerator?.Dispose();
            _toDispose?.Dispose();
        }

        /// <summary>
        /// get enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            var own = _ownEnumerator;
            if (!(own is null))
            {
                _toDispose = own;
                _ownEnumerator = null;
                return own;
            }
            else
            {
                _toDispose?.Dispose();
                _toDispose = null;
                EnumeratorCount++;
                return _wrapped.GetEnumerator();
            }
        }

        /// <summary>
        /// get enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
