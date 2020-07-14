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
    internal class PacketSender:IPacketSender
    {
        //saea buff offset
        internal int bufferOffset = 0;
        //buff send position
        internal int bufferPosition = 0;
        //message header size
        internal readonly int headerLength = 4;
        //message header bytes
        internal byte[] headerData = null;
        internal int sendedHeaderBytesCount = 0;
        //message body size
        internal int messageSize = 0;
        //msaage bytes
        internal byte[] messageData = null;
        //发送队列
        internal ConcurrentQueue<byte[]> messages;

        //发送的消息体字节数
        internal int sendedMessageBytesCount = 0;

        int m_Sending = 0;

        internal PacketSender(int bufferOffset, int headerLength)
        {
            this.bufferOffset = bufferOffset;
            this.headerLength = headerLength;
        }

        internal void Add(byte[] data)
        {
            messages.Enqueue(data);
        }

        internal void Reset()
        {
            sendedHeaderBytesCount = 0;
            sendedMessageBytesCount = 0;
            messageData = null;
            headerData = null;
            messageSize = 0;
        }

        private void PrepareMessageData()
        {
            if (messages.TryDequeue(out messageData))
            {
                messageSize = messageData.Length;
                sendedMessageBytesCount = 0;
                sendedHeaderBytesCount = 0;
                headerData = BitConverter.GetBytes(messageSize);
            }
            else
            {
                Reset();
            }
        }

        private int SendHeader(SocketAsyncEventArgs sendEventArgs,int remainingBufferSize)
        {
            int leftHeaderSize = headerLength - sendedHeaderBytesCount;
            if (remainingBufferSize >= headerLength)
            {
                Buffer.BlockCopy(headerData, sendedHeaderBytesCount, sendEventArgs.Buffer, bufferPosition, leftHeaderSize);
                remainingBufferSize -=leftHeaderSize;
                bufferPosition += leftHeaderSize;

                sendedHeaderBytesCount = headerLength;
            }
            else
            {
                Buffer.BlockCopy(headerData, sendedHeaderBytesCount, sendEventArgs.Buffer, bufferPosition, remainingBufferSize);
                sendedHeaderBytesCount += remainingBufferSize;
                remainingBufferSize = 0;
            }

            return remainingBufferSize;
        }

        int SendMessage(SocketAsyncEventArgs sendEventArgs, int remainingBufferSize)
        {
            int sendBytesRemainingCount = messageSize - sendedMessageBytesCount;
            if (sendBytesRemainingCount <= remainingBufferSize)
            {
                Buffer.BlockCopy(messageData, sendedMessageBytesCount, sendEventArgs.Buffer, bufferPosition, sendBytesRemainingCount);
                remainingBufferSize -= sendBytesRemainingCount;
                bufferPosition += sendBytesRemainingCount;
                PrepareMessageData();
            }
            else
            {
                Buffer.BlockCopy(messageData, sendedMessageBytesCount, sendEventArgs.Buffer, bufferPosition, remainingBufferSize);
                sendedMessageBytesCount += remainingBufferSize;
                remainingBufferSize = 0;
            }

            return remainingBufferSize;
        }

        public int Send(SocketAsyncEventArgs sendEventArgs,int bufferSize)
        {
            int sendBytesCount = 0;

            if (messageData == null)
            {
                PrepareMessageData();
            }

            if (messageData == null)
            {
                return sendBytesCount;
            }

            int remainingBufferSize = bufferSize;

            while (remainingBufferSize > 0 && messageData!=null)
            {
                if (sendedHeaderBytesCount < headerLength)
                {
                    remainingBufferSize = SendHeader(sendEventArgs, remainingBufferSize);
                }
                else
                {
                    remainingBufferSize = SendMessage(sendEventArgs, remainingBufferSize);
                }
            }

            sendBytesCount = bufferSize - remainingBufferSize;

            if (sendBytesCount>0)
            {
                sendEventArgs.SetBuffer(bufferOffset, sendBytesCount);
                bufferPosition = bufferOffset;
            }
            return sendBytesCount;
        }

        internal bool isSending
        {
            get
            {
                return m_Sending == 1;
            }
            set
            {
                Interlocked.Exchange(ref m_Sending, value?1:0);
            }
        }
    }
}
