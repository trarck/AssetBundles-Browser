using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text; //for testing

namespace SocketAsyncServer
{
    internal class DataHoldingUserToken
    {
        internal Mediator theMediator;
        internal DataHolder theDataHolder;

        internal int socketHandleNumber;

        internal readonly int bufferOffsetReceive;
        internal readonly int permanentReceiveMessageOffset;
        internal readonly int bufferOffsetSend;
        
        private int idOfThisObject; //for testing only        
               
        internal int lengthOfCurrentIncomingMessage;
        
        //receiveMessageOffset is used to mark the byte position where the message
        //begins in the receive buffer. This value can sometimes be out of
        //bounds for the data stream just received. But, if it is out of bounds, the 
        //code will not access it.
        internal int receiveMessageOffset;        
        internal Byte[] byteArrayForPrefix;        
        internal readonly int receivePrefixLength;
        internal int receivedPrefixBytesDoneCount = 0;
        internal int receivedMessageBytesDoneCount = 0;
        //This variable will be needed to calculate the value of the
        //receiveMessageOffset variable in one situation. Notice that the
        //name is similar but the usage is different from the variable
        //receiveSendToken.receivePrefixBytesDone.
        internal int recPrefixBytesDoneThisOp = 0;

        internal int sendBytesRemainingCount;
        internal readonly int sendPrefixLength;
        internal Byte[] dataToSend;
        internal int bytesSentAlreadyCount;

        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        private int sessionId;                

        public DataHoldingUserToken(SocketAsyncEventArgs e, int rOffset, int sOffset, int receivePrefixLength, int sendPrefixLength, int identifier)
        {
            idOfThisObject = identifier;
           
            //Create a Mediator that has a reference to the SAEA object.
            theMediator = new Mediator(e);
            bufferOffsetReceive = rOffset;
            bufferOffsetSend = sOffset;
            receivePrefixLength = receivePrefixLength;
            sendPrefixLength = sendPrefixLength;
            receiveMessageOffset = rOffset + receivePrefixLength;
            permanentReceiveMessageOffset = receiveMessageOffset;            
        }

        //Let's use an ID for this object during testing, just so we can see what
        //is happening better if we want to.
        public int TokenId
        {
            get
            {
                return idOfThisObject;
            }
        }

        internal void CreateNewDataHolder()
        {
            theDataHolder = new DataHolder();
        }
                
        //Used to create sessionId variable in DataHoldingUserToken.
        //Called in ProcessAccept().
        internal void CreateSessionId()
        {
            sessionId = Interlocked.Increment(ref Program.mainSessionId);                        
        }

        public int SessionId
        {
            get
            {
                return sessionId;
            }
        }

        public void Reset()
        {
            receivedPrefixBytesDoneCount = 0;
            receivedMessageBytesDoneCount = 0;
            recPrefixBytesDoneThisOp = 0;
            receiveMessageOffset = permanentReceiveMessageOffset;
        }
    }
}
