using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text; //for testing

namespace SocketAsyncServer
{
    using Packet;

    internal class AsyncUserToken
    {
        static int s_SessionId=0;

#if NET_DEBUG
        internal int socketHandleNumber;
#endif

        //object id
        private int m_Id;

        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        private int m_SessionId;

        internal PacketReceiver receivePacket;
        internal PacketSender sendPacket;

        internal Socket socket;
        internal SocketAsyncEventArgs receiveSaea;
        internal SocketAsyncEventArgs sendSaea;

        internal Action<AsyncUserToken, byte[]> messageHandler;

        public AsyncUserToken(SocketAsyncEventArgs receiveSaea, SocketAsyncEventArgs sendSaea, int headerLength,  int identifier)
        {
            m_Id = identifier;
            this.receiveSaea = receiveSaea;
            this.sendSaea = sendSaea;
            receivePacket = new PacketReceiver(receiveSaea.Offset, headerLength);
            receivePacket.onReceiveMessage += DoReceiveMessage;
            sendPacket = new PacketSender(sendSaea.Offset, headerLength);
        }

        //Let's use an ID for this object during testing, just so we can see what
        //is happening better if we want to.
        public int TokenId
        {
            get
            {
                return m_Id;
            }
        }
                
        //Used to create sessionId variable in DataHoldingUserToken.
        //Called in ProcessAccept().
        internal void CreateSessionId()
        {
            m_SessionId = Interlocked.Increment(ref s_SessionId);                        
        }

        public int SessionId
        {
            get
            {
                return m_SessionId;
            }
        }

        public void Reset()
        {
            receivePacket.Reset();
            receivePacket.bufferPosition = receivePacket.bufferOffset;
            sendPacket.Reset();
            sendPacket.bufferPosition = sendPacket.bufferOffset;
        }

        void DoReceiveMessage(byte[] data)
        {
            messageHandler?.Invoke(this, data);
        }
    }
}
