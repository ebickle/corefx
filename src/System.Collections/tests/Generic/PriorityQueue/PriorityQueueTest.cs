// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace PriorityQueueTests
{
    public class PriorityQueueTest
    {
        // constructor(collection, comparer)
        // Count property
        // Enqueue
        // Dequeue
        // Contains
        // CopyTo
        // ICollection.CopyTo
        // ToArray
        // GetEnumerator
        // IEnumerable<T>.GetEnumerator
        // IEnumerable.GetEnumerator
        // TrimExcess
        // SyncRoot
        // (SiftDown)
        // (SiftUp)
        // (Heapify)
        // Enumerator

        [Fact]
        public void TestBasicScenarios()
        {
            var intQueue = new PriorityQueue<int>();
            intQueue.Enqueue(5);
            intQueue.Enqueue(10);
            intQueue.Enqueue(0);
            intQueue.Enqueue(-1);
            intQueue.Enqueue(15);
            intQueue.Enqueue(0);
            Assert.Equal(-1, intQueue.Dequeue());
            Assert.Equal(0, intQueue.Dequeue());
            Assert.Equal(0, intQueue.Dequeue());
            Assert.Equal(5, intQueue.Dequeue());
            Assert.Equal(10, intQueue.Dequeue());
            Assert.Equal(15, intQueue.Dequeue());
            Assert.Throws<InvalidOperationException>(() => intQueue.Dequeue());

            var stringQueue = new PriorityQueue<string>();
            stringQueue.Enqueue("Aaaa");
            stringQueue.Enqueue(null);
            stringQueue.Enqueue("");
            stringQueue.Enqueue(null);
            stringQueue.Enqueue("Bbbb");
            stringQueue.Enqueue("AaBb");
            Assert.Equal(null, stringQueue.Dequeue());
            Assert.Equal(null, stringQueue.Dequeue());
            Assert.Equal("", stringQueue.Dequeue());
            Assert.Equal("Aaaa", stringQueue.Dequeue());
            Assert.Equal("AaBb", stringQueue.Dequeue());
            Assert.Equal("Bbbb", stringQueue.Dequeue());
        }

        [Fact]
        public void ConstructorComparerTest()
        {
            Comparer<int> comparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            PriorityQueue<int> queue = new PriorityQueue<int>(comparer);
            Assert.Equal(comparer, queue.Comparer);
        }

        [Fact]
        public void ConstructorCapacityTest()
        {
            PriorityQueue<int> queue;

            queue = new PriorityQueue<int>(0);
            queue.Enqueue(1);
            queue.Enqueue(5);
            queue.Enqueue(3);

            queue = new PriorityQueue<int>(1);
            queue.Enqueue(1);
            queue.Enqueue(5);

            Assert.Throws<ArgumentOutOfRangeException>(() => new PriorityQueue<int>(-1));
        }

        [Fact]
        public void ConstructorComparerCapacityTest()
        {
            Comparer<int> comparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            PriorityQueue<int> queue;
                
            queue = new PriorityQueue<int>(0, comparer);
            Assert.Equal(comparer, queue.Comparer);
            queue.Enqueue(1);
            queue.Enqueue(5);
            queue.Enqueue(3);

            queue = new PriorityQueue<int>(1, comparer);
            Assert.Equal(comparer, queue.Comparer);
            queue.Enqueue(1);
            queue.Enqueue(5);

            Assert.Throws<ArgumentOutOfRangeException>(() => new PriorityQueue<int>(-1, comparer));
        }

        [Fact]
        public void ConstructorCollectionTest()
        {
            PriorityQueue<int> queue;

            // IEnumerable<T>
            queue = new PriorityQueue<int>(new EnumerableWrapper<int>(new int[] { 8, 81, 4, 0, 0, 2, 8, -2, 4 }));
            Assert.Equal(9, queue.Count);
            Assert.Equal(-2, queue.Dequeue());
            Assert.Equal(0, queue.Dequeue());
            Assert.Equal(0, queue.Dequeue());
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(4, queue.Dequeue());
            Assert.Equal(4, queue.Dequeue());
            Assert.Equal(8, queue.Dequeue());
            Assert.Equal(8, queue.Dequeue());
            Assert.Equal(81, queue.Dequeue());

            // IEnumerable<T> empty
            queue = new PriorityQueue<int>(new EnumerableWrapper<int>(new int[0]));
            Assert.Equal(0, queue.Count);

            // IList<T>
            queue = new PriorityQueue<int>(new List<int> { 8, 81, 4, 0, 0, 2, 8, -2, 4 });
            Assert.Equal(9, queue.Count);
            Assert.Equal(-2, queue.Dequeue());
            Assert.Equal(0, queue.Dequeue());
            Assert.Equal(0, queue.Dequeue());
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(4, queue.Dequeue());
            Assert.Equal(4, queue.Dequeue());
            Assert.Equal(8, queue.Dequeue());
            Assert.Equal(8, queue.Dequeue());
            Assert.Equal(81, queue.Dequeue());

            // IList<T> empty
            queue = new PriorityQueue<int>(new List<int>());
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void ComparerPropertyTest()
        {
            PriorityQueue<int> intQueue;
            
            intQueue = new PriorityQueue<int>();
            Assert.Equal(-1, intQueue.Comparer.Compare(1, 2));
            Assert.Equal(1, intQueue.Comparer.Compare(2, 1));
            Assert.Equal(0, intQueue.Comparer.Compare(1, 1));

            intQueue = new PriorityQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.Equal(1, intQueue.Comparer.Compare(1, 2));
            Assert.Equal(-1, intQueue.Comparer.Compare(2, 1));

            PriorityQueue<object> objectQueue;
            objectQueue = new PriorityQueue<object>();
            Assert.Equal(-1, objectQueue.Comparer.Compare(1, 2));
            Assert.Equal(1, objectQueue.Comparer.Compare(2, 1));
            Assert.Equal(0, objectQueue.Comparer.Compare(1, 1));
            Assert.Equal(0, objectQueue.Comparer.Compare(null, null));
            Assert.Equal(-1, objectQueue.Comparer.Compare(null, 1));
            Assert.Equal(1, objectQueue.Comparer.Compare(1, null));

            objectQueue = new PriorityQueue<object>(Comparer<object>.Create((a, b) => 0));
            Assert.Equal(0, objectQueue.Comparer.Compare(new object(), new object()));
        }

        [Fact]
        public void PeekTest()
        {
            var intQueue = new PriorityQueue<int>();
            intQueue.Enqueue(5);
            Assert.Equal(5, intQueue.Peek());
            intQueue.Enqueue(10);
            Assert.Equal(5, intQueue.Peek());
            intQueue.Enqueue(0);
            Assert.Equal(0, intQueue.Peek());
            intQueue.Enqueue(-1);
            Assert.Equal(-1, intQueue.Peek());
            intQueue.Enqueue(15);
            Assert.Equal(-1, intQueue.Peek());
            intQueue.Enqueue(0);
            Assert.Equal(-1, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Equal(0, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Equal(0, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Equal(5, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Equal(10, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Equal(15, intQueue.Peek());
            intQueue.Dequeue();
            Assert.Throws<InvalidOperationException>(() => intQueue.Peek());
        }

        [Fact]
        public void ClearTest()
        {
            PriorityQueue<int> queue;

            queue = new PriorityQueue<int>();
            queue.Clear();
            Assert.Equal(0, queue.Count);
            Assert.True(queue.ToArray().Length == 0);

            queue = new PriorityQueue<int>();
            queue.Enqueue(0);
            queue.Dequeue();
            queue.Clear();
            Assert.Equal(0, queue.Count);
            Assert.True(queue.ToArray().Length == 0);

            queue = new PriorityQueue<int>();
            queue.Enqueue(0);
            queue.Enqueue(0);
            queue.Enqueue(5);
            queue.Enqueue(-5);
            queue.Enqueue(5);
            queue.Clear();
            Assert.Equal(0, queue.Count);
            Assert.True(queue.ToArray().Length == 0);
        }

        [Fact]
        public void SynchronizedPropertyTest()
        {
            var queue = new PriorityQueue<int>();
            Assert.False(((ICollection)queue).IsSynchronized);
        }

        [Fact]
        public void ContainsOnlyEnqueuedItems()
        {
            IComparer<TestItem> comparer = Comparer<TestItem>.Create((x, y) =>
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return x.Rank - y.Rank;
            });

            var item1 = new TestItem { Id = 3, Rank = 1 };
            var item2 = new TestItem { Id = 5, Rank = 3 };
            var unqueued = new TestItem { Id = 7, Rank = 3 };

            var queue = new PriorityQueue<TestItem>(comparer);
            queue.Enqueue(item1);
            queue.Enqueue(item2);

            Assert.True(queue.Contains(item1));
            Assert.True(queue.Contains(item2));
            Assert.False(queue.Contains(unqueued));
        } 

        /// <summary>
        /// Wrapper class used to ensure a collection cannot be cast to anything other than <see cref="IEnumerable{T}"/>.
        /// </summary>
        private class EnumerableWrapper<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _collection;

            public EnumerableWrapper(IEnumerable<T> collection)
            {
                _collection = collection;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        private class TestItem
        {            
            public int Id { get; set; }
            public int Rank { get; set; }

            public override bool Equals(object obj)
            {
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                var other = obj as TestItem;
                if (other == null)
                {
                    return false;
                }

                return Id.Equals(other.Id);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}
