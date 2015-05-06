// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a collection of objects that are removed in a prioritized order.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    [DebuggerTypeProxy(typeof(PriorityQueueDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class PriorityQueue<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
    {
        private T[] _array;
        private int _size;  // number of elements
        private int _version;
        private Object _syncRoot;
        private readonly IComparer<T> _comparer;

        private const int _minimumGrow = 4;
        private const int _growFactor = 200;  // double each time
        private const int _defaultCapacity = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class 
        /// that uses a default comparer.
        /// </summary>
        public PriorityQueue() 
            : this(0, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class 
        /// that has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="PriorityQueue{T}"/> can contain.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public PriorityQueue(int capacity)
            : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class 
        /// that uses a specified comparer.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="T:System.Collections.Generic.IComparer{T}"/> to use when comparing elements.
        /// -or-
        /// null to use the default <see cref="T:System.Collections.Generic.Comparer{T}"/> for the type of key.
        /// </param>
        public PriorityQueue(IComparer<T> comparer)
            : this(0, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class 
        /// that contains elements copied from the specified collection and uses a default comparer.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see cref="PriorityQueue{T}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public PriorityQueue(IEnumerable<T> collection)
            : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class 
        /// that contains elements copied from the specified collection and uses a specified comparer.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see cref="PriorityQueue{T}"/>.</param>
        /// <param name="comparer">
        /// The <see cref="T:System.Collections.Generic.IComparer{T}"/> to use when comparing elements.
        /// -or-
        /// null to use the default <see cref="T:System.Collections.Generic.Comparer{T}"/> for the type of key.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public PriorityQueue(IEnumerable<T> collection, IComparer<T> comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            _comparer = comparer ?? Comparer<T>.Default;

            var typedCollection = collection as ICollection<T>;
            if (typedCollection != null)
            {
                InitializeFromCollection(typedCollection);
            }
            else 
            {
                InitializeFromCollection(collection);
            }

            Heapify();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class that is empty,
        /// has the specified initial capacity, and uses a specified comparer.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="PriorityQueue{T}"/> can contain.</param>
        /// <param name="comparer">
        /// The <see cref="T:System.Collections.Generic.IComparer{T}"/> to use when comparing elements.
        /// -or-
        /// null to use the default <see cref="T:System.Collections.Generic.Comparer{T}"/> for the type of key.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            }

            _comparer = comparer ?? Comparer<T>.Default;
            _array = (capacity > 0) ? new T[capacity] : Array.Empty<T>();
        }

        /// <summary>
        /// Gets the <see cref="IComparer{T}"/> for the <see cref="PriorityQueue{T}"/>. 
        /// </summary>
        /// <value>
        /// The <see cref="T:System.Collections.Generic.IComparer{T}"/> that is used when
        /// comparing elements in the <see cref="PriorityQueue{T}"/>. 
        /// </value>
        public IComparer<T> Comparer 
        { 
            get
            {
                return _comparer;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="PriorityQueue{T}"/>.</value>
        public int Count 
        { 
            get
            {
                return _size;
            }
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <param name="item">
        /// The object to add to the end of the <see cref="PriorityQueue{T}"/>. 
        /// The value can be null for reference types.
        /// </param>
        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                IncreaseCapacity();
            }

            _array[_size] = item;
            SiftUp(_size);

            _size++;
            _version++;
        }

        /// <summary>
        /// Removes and returns the object with the lowest priority in the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <returns>The object with the lowest priority that is removed from the <see cref="PriorityQueue{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{T}"/> is empty.</exception>
        public T Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyPriorityQueue);
            }

            _size--;

            T removed = _array[0];
            _array[0] = _array[_size];
            _array[_size] = default(T);
            SiftDown(0);
            
            _version++;

            return removed;
        }

        /// <summary>
        /// Returns the object with the lowest priority in the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{T}"/> is empty.</exception>
        public T Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyPriorityQueue);
            }

            return _array[0];
        }

        /// <summary>
        /// Removes all elements from the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (_size > 0)
            {
                ArrayT<T>.Clear(_array, 0, _size);
                _size = 0;
            }

            _version++;
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <param name="item">
        /// The object to add to the end of the <see cref="PriorityQueue{T}"/>. 
        /// The value can be null for reference types.
        /// </param>
        /// <returns>
        /// true if item is found in the <see cref="PriorityQueue{T}"/>;  otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            if ((Object)item == null)
            {
                for (int i = 0; i < _size; i++)
                {
                    if ((Object)_array[i] == null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < _size; i++)
                {
                    if (c.Equals(_array[i], item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the elements of the <see cref="PriorityQueue{T}"/> to an  <see cref="T:System.Array"/>, 
        /// starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the <see cref="PriorityQueue{T}"/>. 
        /// The <see cref="T:System.Array">Array</see> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex"/> is less than zero. -or- 
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of the <paramref name="array"/>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The number of elements in the source <see cref="T:System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="arrayIndex"/> to the end of the destination
        /// <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", SR.ArgumentOutOfRange_Index);
            }

            int arrayLen = array.Length;
            if (arrayLen - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            ArrayT<T>.Copy(_array, 0, array, arrayIndex, _size);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an 
        /// <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the <see cref="PriorityQueue{T}"/>. 
        /// The <see cref="T:System.Array">Array</see> must have zero-based indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/> is multidimensional. -or-
        /// <paramref name="array"/> does not have zero-based indexing. -or-
        /// <paramref name="index"/> is equal to or greater than the length of the <paramref name="array"/> -or- 
        /// The number of elements in the source <see cref="T:System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- 
        /// The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically 
        /// to the type of the destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, "array");
            }

            int arrayLen = array.Length;
            if (index < 0 || index > arrayLen)
            {
                throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
            }

            if (arrayLen - index < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            try
            {
                Array.Copy(_array, 0, array, index, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.Argument_InvalidArrayType);
            }
        }

        /// <summary>
        /// Copies the elements stored in the <see cref="PriorityQueue{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing a snapshot of elements copied from the <see cref="PriorityQueue{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            T[] arr = new T[_size];
            
            if (_size == 0)
            {
                return arr;
            }

            ArrayT<T>.Copy(_array, 0, arr, 0, _size);
            
            return arr;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PriorityQueue{T}"/>
        /// </summary>
        /// <returns>An enumerator for the contents of the <see cref="PriorityQueue{T}"/>.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PriorityQueue{T}"/>
        /// </summary>
        /// <returns>An enumerator for the contents of the <see cref="PriorityQueue{T}"/>.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="PriorityQueue{T}"/>, 
        /// if that number is less than than a threshold value.
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_size < threshold)
            {
                T[] newArray = new T[_size];
                ArrayT<T>.Copy(_array, 0, newArray, 0, _size);
                _array = newArray;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether access to the <see cref="ICollection"/> is 
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="PriorityQueue{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get 
            { 
                return false; 
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the 
        /// <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value>
        /// An object that can be used to synchronize access to the 
        /// <see cref="T:System.Collections.ICollection"/>.
        /// </value>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftDown(int index)
        {
            int current = index;
            T value = _array[index];

            while ((current * 2) + 1 < _size) // TODO: Can this statement be optimized?
            {
                int child = (current * 2) + 1;
                T childValue = _array[child];

                int rightChild = child + 1;
                if (rightChild < _size)
                {
                    T rightChildValue = _array[rightChild];
                    if (_comparer.Compare(childValue, rightChildValue) > 0)
                    {
                        child = rightChild;
                        childValue = rightChildValue;
                    }
                }

                if (_comparer.Compare(value, childValue) <= 0)
                {
                    break;
                }

                _array[current] = childValue;
                current = child;
            }

            _array[current] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftUp(int index)
        {
            int current = index;
            T value = _array[index];

            while (current > 0)
            {
                int parent = (current - 1) / 2;
                T parentValue = _array[parent];

                if (_comparer.Compare(value, parentValue) >= 0)
                {
                    break;
                }

                _array[current] = parentValue;
                current = parent;
            }

            _array[current] = value;
        }

        private void Heapify()
        {
            for (int i = (_size / 2) - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        private void InitializeFromCollection(IEnumerable<T> source)
        {
            _size = 0;
            _array = Array.Empty<T>();

            foreach (var item in source)
            {
                if (_size == _array.Length)
                {
                    IncreaseCapacity();
                }

                _array[_size] = item;
                _size++;
            }
        }

        private void InitializeFromCollection(ICollection<T> source)
        {
            _size = source.Count;
            _array = (_size > 0) ? new T[_size] : Array.Empty<T>();
            source.CopyTo(_array, 0);
        }

        private void IncreaseCapacity()
        {
            if (_size == 0)
            {
                _array = new T[_defaultCapacity];
            }
            else
            {
                int newCapacity = (int)((long)_array.Length * (long)_growFactor / 100);
                if (newCapacity < _array.Length + _minimumGrow)
                {
                    newCapacity = _array.Length + _minimumGrow;
                }
                
                T[] newArray = new T[newCapacity];
                ArrayT<T>.Copy(_array, 0, newArray, 0, _size);
                _array = newArray;
            }
        }

        /// <summary>
        /// Enumerates the elements of a <see cref="PriorityQueue{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly PriorityQueue<T> _queue;
            private int _index;  // -1 = not started, -2 = ended/disposed
            private int _version;
            private T _currentElement;

            internal Enumerator(PriorityQueue<T> queue)
            {
                _queue = queue;
                _version = _queue._version;
                _index = -1;
                _currentElement = default(T);
            }

            /// <summary>
            /// Releases all resources used by the <see cref="PriorityQueue{T}.Enumerator"/>.
            /// </summary>
            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
            }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="PriorityQueue{T}"/>.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if (_index == -2)
                {
                    return false;
                }

                _index++;

                if (_index == _queue._size)
                {
                    _index = -2;
                    _currentElement = default(T);
                    return false;
                }

                _currentElement = _queue._array[_index];
                return true;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public T Current
            {
                get 
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                        }
                    }

                    return _currentElement;
                }
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current
            {
                get 
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                        }
                    }

                    return _currentElement;
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                _index = -1;
                _currentElement = default(T);
            }
        }
    }
}
