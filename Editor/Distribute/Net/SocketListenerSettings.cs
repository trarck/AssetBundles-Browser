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

        // max # of pending connections the listener can hold in queue
        private int m_Backlog=100;

        // tells us how many objects to put in pool for accept operations
        private int m_MaxAcceptOps=10;

        // buffer size to use for each socket receive operation
        private int rm_RceiveBufferSize=1024;

        // length of message prefix for receive ops
        private int m_HeaderLength=4;

        // Endpoint for the listener.
        private IPEndPoint m_LocalEndPoint;

        public SocketListenerSettings()
        {

        }

        public SocketListenerSettings(int maxConnections,  int backlog, int maxSimultaneousAcceptOps, int headerLength, int receiveBufferSize, IPEndPoint theLocalEndPoint)
        {
            m_MaxConnections = maxConnections;
            m_Backlog = backlog;
            m_MaxAcceptOps = maxSimultaneousAcceptOps;
            m_HeaderLength = headerLength;
            rm_RceiveBufferSize = receiveBufferSize;
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
        public int HeaderLength
        {
            get
            {
                return m_HeaderLength;
            }
        }
        public int BufferSize
        {
            get
            {
                return rm_RceiveBufferSize;
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
