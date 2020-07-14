using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace SocketAsyncServer
{    
    internal class AsyncUserTokenPool
    {        
        //just for assigning an ID so we can watch our objects while testing.
        private int m_NextTokenId = 0;
        
        // Pool of reusable SocketAsyncEventArgs objects.        
        Stack<AsyncUserToken> m_Pool;
        
        // initializes the object pool to the specified size.
        // "capacity" = Maximum number of SocketAsyncEventArgs objects
        internal AsyncUserTokenPool(int capacity)
        {

#if NET_DEBUG
                Debug.Log("AsyncUserTokenPool constructor");
#endif

            m_Pool = new Stack<AsyncUserToken>(capacity);
        }

        // The number of SocketAsyncEventArgs instances in the pool.         
        internal int Count
        {
            get { return m_Pool.Count; }
        }

        internal int AssignTokenId()
        {
            int tokenId = Interlocked.Increment(ref m_NextTokenId);            
            return tokenId;
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        internal AsyncUserToken Pop()
        {
            lock (m_Pool)
            {
                return m_Pool.Pop();
            }
        }

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        internal void Push(AsyncUserToken item)
        {
            if (item == null) 
            { 
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); 
            }
            lock (m_Pool)
            {
                m_Pool.Push(item);
            }
        }
    }
}
