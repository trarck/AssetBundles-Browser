//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using System.Text;
//using UnityEngine;

//namespace SocketAsyncServer
//{
//    internal class ReceiveHandler
//    {
//        public Action<AsyncUserToken, byte[]> onReceiveMessage;

//        public void ReceiveData(SocketAsyncEventArgs e, AsyncUserToken receiveUserToken, int remainingBytesToProcess)
//        {
//            while (remainingBytesToProcess > 0)
//            {
//                if (receiveUserToken.receivePacket.receivedHeaderBytesCount < receiveUserToken.receivePacket.headerLength)
//                {
//                    remainingBytesToProcess = HandleHead(e, receiveUserToken, remainingBytesToProcess);
//#if NET_DEBUG
//                    Debug.Log("ProcessReceive, after prefix work " + receiveUserToken.TokenId + ". remainingBytesToProcess = " + remainingBytesToProcess);
//#endif
//                    //可以不加以下代码，进行下一次循环体
//                    //if (remainingBytesToProcess > 0)
//                    //{
//                    //    //head is ok
//                    //    remainingBytesToProcess = HandleMessage(e, receiveUserToken, remainingBytesToProcess);
//                    //}
//                    //else
//                    //{
//                    //    break;
//                    //}
//                }
//                else
//                {
//                    remainingBytesToProcess = HandleMessage(e, receiveUserToken, remainingBytesToProcess);
//                }
//            }

//            //reset buffer position to buffer offset in buffer manager
//            receiveUserToken.receivePacket.bufferPosition = receiveUserToken.receivePacket.bufferOffset;
//        }

//        private int HandleHead(SocketAsyncEventArgs e, AsyncUserToken receiveUserToken, int remainingBytesToProcess)
//        {
//            PacketReceiver receivePacket = receiveUserToken.receivePacket;
//            //receivedPrefixBytesDoneCount tells us how many prefix bytes were
//            //processed during previous receive ops which contained data for 
//            //this message. Usually there will NOT have been any previous 
//            //receive ops here. So in that case,
//            //receiveSendToken.receivedPrefixBytesDoneCount would equal 0.
//            //Create a byte array to put the new prefix in, if we have not
//            //already done it in a previous loop.
//            if (receivePacket.receivedHeaderBytesCount == 0)
//            {
//                receivePacket.headerData = new byte[receivePacket.headerLength];
//            }

//            // If this next if-statement is true, then we have received >=
//            // enough bytes to have the prefix. So we can determine the 
//            // length of the message that we are working on.
//            int leftHeaderSize = receivePacket.headerLength - receivePacket.receivedHeaderBytesCount;
//            if (remainingBytesToProcess >= leftHeaderSize)
//            {
//                //Now copy that many bytes to byteArrayForPrefix.
//                //We can use the variable receiveMessageOffset as our main
//                //index to show which index to get data from in the TCP
//                //buffer.
                
//                Buffer.BlockCopy(e.Buffer, receivePacket.bufferPosition, receivePacket.headerData, receivePacket.receivedHeaderBytesCount, leftHeaderSize);
//                remainingBytesToProcess = remainingBytesToProcess - leftHeaderSize;
//                receivePacket.bufferPosition += leftHeaderSize;

//                receivePacket.receivedHeaderBytesCount = receivePacket.headerLength;

//                receivePacket.messageSize = BitConverter.ToInt32(receivePacket.headerData, 0);
//            }

//            //This next else-statement deals with the situation 
//            //where we have some bytes
//            //of this prefix in this receive operation, but not all.
//            else
//            {
//                //Write the bytes to the array where we are putting the
//                //prefix data, to save for the next loop.
//                Buffer.BlockCopy(e.Buffer, receivePacket.bufferPosition, receivePacket.headerData, receivePacket.receivedHeaderBytesCount, remainingBytesToProcess);
//                //the remainingBytesToProcess whill set to zero.no set bufferPosition.
//                //receiveUserToken.bufferPosition += remainingBytesToProcess;
//                receivePacket.receivedHeaderBytesCount += remainingBytesToProcess;
//                remainingBytesToProcess = 0;
//            }
//            return remainingBytesToProcess;
//        }

//        private int HandleMessage(SocketAsyncEventArgs receiveEventArgs, AsyncUserToken receiveUserToken, int remainingBytesToProcess)
//        {
//            PacketReceiver receivePacket = receiveUserToken.receivePacket;
//            //Create the array where we'll store the complete message, 
//            //if it has not been created on a previous receive op.
//            if (receivePacket.receivedMessageBytesCount == 0)
//            {
//                receivePacket.messageData = new byte[receivePacket.messageSize];
//            }

//            // Remember there is a receiveSendToken.receivedPrefixBytesDoneCount
//            // variable, which allowed us to handle the prefix even when it
//            // requires multiple receive ops. In the same way, we have a 
//            // receiveSendToken.receivedMessageBytesDoneCount variable, which
//            // helps us handle message data, whether it requires one receive
//            // operation or many.
//            int leftMessageSize= receivePacket.messageSize - receivePacket.receivedMessageBytesCount;
//            if (remainingBytesToProcess  >= leftMessageSize)
//            {
//                // If we are inside this if-statement, then we got 
//                // the end of the message. In other words,
//                // the total number of bytes we received for this message matched the 
//                // message length value that we got from the prefix.

//                // Write/append the bytes received to the byte array in the 
//                // DataHolder object that we are using to store our data.
//                Buffer.BlockCopy(receiveEventArgs.Buffer, receivePacket.bufferPosition, receivePacket.messageData, receivePacket.receivedMessageBytesCount, leftMessageSize);
//                remainingBytesToProcess = remainingBytesToProcess - leftMessageSize;
//                receivePacket.bufferPosition += leftMessageSize;
//                try
//                {
//                    onReceiveMessage?.Invoke(receiveUserToken, receivePacket.messageData);
//                }
//                catch(Exception e)
//                {
//                    Debug.LogException(e);
//                }
//                receivePacket.Reset();
//            }
//            else
//            {
//                // If we are inside this else-statement, then that means that we
//                // need another receive op. We still haven't got the whole message,
//                // even though we have examined all the data that was received.
//                // Not a problem. In SocketListener.ProcessReceive we will just call
//                // StartReceive to do another receive op to receive more data.
//                Buffer.BlockCopy(receiveEventArgs.Buffer, receivePacket.bufferPosition, receivePacket.messageData, receivePacket.receivedMessageBytesCount, remainingBytesToProcess);
//                receivePacket.receivedMessageBytesCount += remainingBytesToProcess;
//                remainingBytesToProcess = 0;
//            }

//            return remainingBytesToProcess;
//        }
//    }
//}
