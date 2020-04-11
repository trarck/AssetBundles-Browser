using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SocketAsyncServer
{
    public class SocketListenerSettings
    {
        // the maximum number of connections the sample is designed to handle simultaneously 
        private int m_MaxConnections=100;

        // this variable allows us to create some extra SAEA objects for the pool,
        // if we wish.
        private int m_ExcessSaeaObjectsInPool=1;

        // max # of pending connections the listener can hold in queue
        private int m_Backlog=100;

        // tells us how many objects to put in pool for accept operations
        private int m_MaxAcceptOps=10;

        // buffer size to use for each socket receive operation
        private int rm_RceiveBufferSize=1024;

        // length of message prefix for receive ops
        private int m_ReceivePrefixLength=4;

        // length of message prefix for send ops
        private int m_SendPrefixLength=4;

        // See comments in buffer manager.
        private int m_OpsToPreAllocate=2;

        // Endpoint for the listener.
        private IPEndPoint m_LocalEndPoint;

        public SocketListenerSettings()
        {

        }

        public SocketListenerSettings(int maxConnections, int excessSaeaObjectsInPool, int backlog, int maxSimultaneousAcceptOps, int receivePrefixLength, int receiveBufferSize, int sendPrefixLength, int opsToPreAlloc, IPEndPoint theLocalEndPoint)
        {
            m_MaxConnections = maxConnections;
            m_ExcessSaeaObjectsInPool = excessSaeaObjectsInPool;
            m_Backlog = backlog;
            m_MaxAcceptOps = maxSimultaneousAcceptOps;
            m_ReceivePrefixLength = receivePrefixLength;
            rm_RceiveBufferSize = receiveBufferSize;
            m_SendPrefixLength = sendPrefixLength;
            m_OpsToPreAllocate = opsToPreAlloc;
            m_LocalEndPoint = theLocalEndPoint;
        }

        public int MaxConnections
        {
            get
            {
                return m_MaxConnections;
            }
            set
            {
                m_MaxConnections = value;
            }
        }

        public int ExcessSaeaObjectsInPool
        {
            get
            {
                return m_ExcessSaeaObjectsInPool;
            }
            set
            {
                m_ExcessSaeaObjectsInPool = value;
            }
        }

        public int NumberOfSaeaForRecSend
        {
            get
            {
                return m_MaxConnections+ m_ExcessSaeaObjectsInPool;
            }
        }
        public int Backlog
        {
            get
            {
                return m_Backlog;
            }
        }
        public int MaxAcceptOps
        {
            get
            {
                return m_MaxAcceptOps;
            }
        }
        public int ReceivePrefixLength
        {
            get
            {
                return m_ReceivePrefixLength;
            }
        }
        public int BufferSize
        {
            get
            {
                return rm_RceiveBufferSize;
            }
        }
        public int SendPrefixLength
        {
            get
            {
                return m_SendPrefixLength;
            }
        }
        public int OpsToPreAllocate
        {
            get
            {
                return m_OpsToPreAllocate;
            }
        }
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return m_LocalEndPoint;
            }
        }    
    }    
}
