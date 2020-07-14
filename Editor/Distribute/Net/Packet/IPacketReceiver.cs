using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncServer.Packet
{
    internal interface IPacketReceiver
    {
        event Action<byte[]> onReceiveMessage;
        void Receive(SocketAsyncEventArgs receiveEventArgs, int remainingBytesToProcess);
    }
}

