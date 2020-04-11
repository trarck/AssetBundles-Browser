using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketAsyncServer
{   
    internal class BufferManager
    {
        // This class creates a single large buffer which can be divided up 
        // and assigned to SocketAsyncEventArgs objects for use with each 
        // socket I/O operation.  
        // This enables buffers to be easily reused and guards against 
        // fragmenting heap memory.
        // 
        //This buffer is a byte array which the Windows TCP buffer can copy its data to.

        // the total number of bytes controlled by the buffer pool
        int m_TotalBytesInBufferBlock;

        // Byte array maintained by the Buffer Manager.
        byte[] m_BufferBlock;         
        Stack<int> m_FreeIndexPool;     
        int m_CurrentIndex;
        int m_bufferSizeAllocatedForEachSaea;
        
        public BufferManager(int totalBytes, int bufferSizeInEachSaeaObject)
        {
            m_TotalBytesInBufferBlock = totalBytes;
            m_CurrentIndex = 0;
            m_bufferSizeAllocatedForEachSaea = bufferSizeInEachSaeaObject;
            m_FreeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool
        internal void InitBuffer()
        {
            // Create one large buffer block.
            m_BufferBlock = new byte[m_TotalBytesInBufferBlock];
        }

        // Divide that one large buffer block out to each SocketAsyncEventArg object.
        // Assign a buffer space from the buffer block to the 
        // specified SocketAsyncEventArgs object.
        //
        // returns true if the buffer was successfully set, else false
        internal bool SetBuffer(SocketAsyncEventArgs args)
        {
            
            if (m_FreeIndexPool.Count > 0)
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space 
                //back into this stack.
                args.SetBuffer(m_BufferBlock, m_FreeIndexPool.Pop(), m_bufferSizeAllocatedForEachSaea);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the 
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((m_TotalBytesInBufferBlock - m_bufferSizeAllocatedForEachSaea) < m_CurrentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_BufferBlock, m_CurrentIndex, m_bufferSizeAllocatedForEachSaea);
                m_CurrentIndex += m_bufferSizeAllocatedForEachSaea;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.   This frees the
        // buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        // unless you need to destroy the SAEA object, or maybe in the case
        // of some exception handling. Instead, on the server
        // keep the same buffer space assigned to one SAEA object for the duration of
        // this app's running.
        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_FreeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}
