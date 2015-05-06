// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// A debugger view of the PriorityQueue that makes it simple to browse the
    /// collection's contents at a point in time.
    /// </summary>
    /// <typeparam name="T">The type of elements stored within.</typeparam>
    internal sealed class PriorityQueueDebugView<T>
    {
        private readonly PriorityQueue<T> _queue;

        /// <summary>
        /// Constructs a new debugger view object for the provided priority queue.
        /// </summary>
        /// <param name="queue">A priority queue to browse in the debugger.</param>
        public PriorityQueueDebugView(PriorityQueue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            _queue = queue;
        }

        /// <summary>
        /// Returns a snapshot of the underlying queue's elements.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _queue.ToArray();
            }
        }
    }
}
