using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;

namespace SocketAsyncServer.Packet
{
    internal class PacketReceiver: IPacketReceiver
    {
        //saea buff offset
        internal int bufferOffset = 0;
        //buff receive position
        internal int bufferPosition = 0;
        //message header size
        internal readonly int headerLength = 4;
        //message header bytes
        internal byte[] headerData = null;
        //已经接收的头部字节数
        internal int receivedHeaderBytesCount = 0;
        //message body size
        internal int messageSize = 0;
        //msaage bytes
        internal byte[] messageData = null;
        //已经接收的消息体字节数
        internal int receivedMessageBytesCount = 0;

        public event Action<byte[]> onReceiveMessage;

        internal PacketReceiver(int bufferOffset,int headerLength)
        {
            this.bufferOffset = bufferOffset;
            this.headerLength = headerLength;
        }

        internal void Reset()
        {
            receivedHeaderBytesCount = 0;
            receivedMessageBytesCount = 0;
            headerData = null;
            messageData = null;
            messageSize = 0;
        }

        public void Receive(SocketAsyncEventArgs receiveEventArgs, int remainingBytesToProcess)
        {
            while (remainingBytesToProcess > 0)
            {
                if (receivedHeaderBytesCount < headerLength)
                {
                    remainingBytesToProcess = HandleHead(receiveEventArgs,  remainingBytesToProcess);

                    //可以不加以下代码，进行下一次循环体
                    //if (remainingBytesToProcess > 0)
                    //{
                    //    //head is ok
                    //    remainingBytesToProcess = HandleMessage(e, receiveUserToken, remainingBytesToProcess);
                    //}
                    //else
                    //{
                    //    break;
                    //}
                }
                else
                {
                    remainingBytesToProcess = HandleMessage(receiveEventArgs,  remainingBytesToProcess);
                }
            }

            //reset buffer position to buffer offset in buffer manager
            bufferPosition = bufferOffset;
        }

        private int HandleHead(SocketAsyncEventArgs receiveEventArgs,  int remainingBytesToProcess)
        {
            //receivedPrefixBytesDoneCount tells us how many prefix bytes were
            //processed during previous receive ops which contained data for 
            //this message. Usually there will NOT have been any previous 
            //receive ops here. So in that case,
            //receiveSendToken.receivedPrefixBytesDoneCount would equal 0.
            //Create a byte array to put the new prefix in, if we have not
            //already done it in a previous loop.
            if (receivedHeaderBytesCount == 0)
            {
                headerData = new byte[headerLength];
            }

            // If this next if-statement is true, then we have received >=
            // enough bytes to have the prefix. So we can determine the 
            // length of the message that we are working on.
            int leftHeaderSize = headerLength - receivedHeaderBytesCount;
            if (remainingBytesToProcess >= leftHeaderSize)
            {
                //Now copy that many bytes to byteArrayForPrefix.
                //We can use the variable receiveMessageOffset as our main
                //index to show which index to get data from in the TCP
                //buffer.

                Buffer.BlockCopy(receiveEventArgs.Buffer, bufferPosition, headerData, receivedHeaderBytesCount, leftHeaderSize);
                remainingBytesToProcess = remainingBytesToProcess - leftHeaderSize;
                bufferPosition += leftHeaderSize;

                receivedHeaderBytesCount = headerLength;

                messageSize = BitConverter.ToInt32(headerData, 0);
            }

            //This next else-statement deals with the situation 
            //where we have some bytes
            //of this prefix in this receive operation, but not all.
            else
            {
                //Write the bytes to the array where we are putting the
                //prefix data, to save for the next loop.
                Buffer.BlockCopy(receiveEventArgs.Buffer, bufferPosition, headerData, receivedHeaderBytesCount, remainingBytesToProcess);
                //the remainingBytesToProcess whill set to zero.no set bufferPosition.
                //receiveUserToken.bufferPosition += remainingBytesToProcess;
                receivedHeaderBytesCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }
            return remainingBytesToProcess;
        }

        private int HandleMessage(SocketAsyncEventArgs receiveEventArgs, int remainingBytesToProcess)
        {
            //Create the array where we'll store the complete message, 
            //if it has not been created on a previous receive op.
            if (receivedMessageBytesCount == 0)
            {
                messageData = new byte[messageSize];
            }

            // Remember there is a receiveSendToken.receivedPrefixBytesDoneCount
            // variable, which allowed us to handle the prefix even when it
            // requires multiple receive ops. In the same way, we have a 
            // receiveSendToken.receivedMessageBytesDoneCount variable, which
            // helps us handle message data, whether it requires one receive
            // operation or many.
            int leftMessageSize = messageSize - receivedMessageBytesCount;
            if (remainingBytesToProcess >= leftMessageSize)
            {
                // If we are inside this if-statement, then we got 
                // the end of the message. In other words,
                // the total number of bytes we received for this message matched the 
                // message length value that we got from the prefix.

                // Write/append the bytes received to the byte array in the 
                // DataHolder object that we are using to store our data.
                Buffer.BlockCopy(receiveEventArgs.Buffer, bufferPosition, messageData, receivedMessageBytesCount, leftMessageSize);
                remainingBytesToProcess = remainingBytesToProcess - leftMessageSize;
                bufferPosition += leftMessageSize;
                try
                {
                    onReceiveMessage?.Invoke(messageData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                Reset();
            }
            else
            {
                // If we are inside this else-statement, then that means that we
                // need another receive op. We still haven't got the whole message,
                // even though we have examined all the data that was received.
                // Not a problem. In SocketListener.ProcessReceive we will just call
                // StartReceive to do another receive op to receive more data.
                Buffer.BlockCopy(receiveEventArgs.Buffer, bufferPosition, messageData, receivedMessageBytesCount, remainingBytesToProcess);
                receivedMessageBytesCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }

            return remainingBytesToProcess;
        }
    }
}
