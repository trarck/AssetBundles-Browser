using System;
using System.IO;
using System.Collections.Generic; //for testing
using System.Net.Sockets;
using System.Threading; //for Semaphore and Interlocked
using System.Net;
using System.Text; //for testing
using System.Diagnostics; //for testing
using System.Collections.Concurrent;
using SocketAsyncServer.Packet;

#if NET_DEBUG
using UnityEngine;
using Debug = UnityEngine.Debug;
#endif

namespace SocketAsyncServer
{   //____________________________________________________________________________
    // Implements the logic for the socket server.  

    public class SocketListener
    {
        //__variables for testing ____________________________________________
#if NET_DEBUG
        //total clients connected to the server, excluding backlog
        internal int numberOfAcceptedSockets;    
                
        //****for testing threads
        Process m_TheProcess; //for testing only
        ProcessThreadCollection m_ArrayOfLiveThreadsInThisProcess;   //for testing
        HashSet<int> m_ManagedThreadIds = new HashSet<int>();  //for testing
        HashSet<Thread> m_ManagedThreads = new HashSet<Thread>();  //for testing        
        //object that will be used to lock the HashSet of thread references 
        //that we use for testing.
        private object m_LockerForThreadHashSet = new object();
        //****end variables for displaying what's happening with threads        
        //__END variables for testing ____________________________________________
#endif
        //__variables that might be used in a  real app__________________________________

        //Buffers for sockets are unmanaged by .NET. 
        //So memory used for buffers gets "pinned", which makes the
        //.NET garbage collector work around it, fragmenting the memory. 
        //Circumvent this problem by putting all buffers together 
        //in one block in memory. Then we will assign a part of that space 
        //to each SocketAsyncEventArgs object, and
        //reuse that buffer space each time we reuse the SocketAsyncEventArgs object.
        //Create a large reusable set of buffers for all socket operations.
        BufferManager m_ReceiveBufferManager;
        BufferManager m_SendBufferManager;

        // the socket used to listen for incoming connection requests
        Socket m_ListenSocket;

        //A Semaphore has two parameters, the initial number of available slots
        // and the maximum number of slots. We'll make them the same. 
        //This Semaphore is used to keep from going over max connection #. (It is not about 
        //controlling threading really here.)   
        Semaphore m_MaxConnectionsEnforcer;

        SocketListenerSettings m_Settings;

        // pool of reusable SocketAsyncEventArgs objects for accept operations
        SocketAsyncEventArgsPool m_AcceptPool;
        //pool of user token
        AsyncUserTokenPool m_UserTokenPool;
        //client users
        private readonly ConcurrentDictionary<int, AsyncUserToken> m_Users;
        //__END variables for real app____________________________________________

        //Event______________________________________________________
        internal event Action<AsyncUserToken, byte[]> onReceiveMessage;

        //__END Event__________________________________________________________
        //_______________________________________________________________________________
        // Constructor.
        public SocketListener(SocketListenerSettings theSocketListenerSettings)
        {
#if NET_DEBUG
            Debug.Log("SocketListener constructor");

            m_TheProcess = Process.GetCurrentProcess(); //for testing only             
            DealWithThreadsForTesting("constructor");

            numberOfAcceptedSockets = 0; //for testing
#endif

            m_Settings = theSocketListenerSettings;

            //Allocate memory for buffers. We are using a separate buffer space for
            //receive and send, instead of sharing the buffer space, like the Microsoft
            //example does.            
            m_ReceiveBufferManager = new BufferManager(m_Settings.BufferSize * m_Settings.MaxConnections, m_Settings.BufferSize);
            m_SendBufferManager = new BufferManager(m_Settings.BufferSize * m_Settings.MaxConnections, m_Settings.BufferSize);

            m_AcceptPool = new SocketAsyncEventArgsPool(m_Settings.MaxAcceptOps);
            m_UserTokenPool = new AsyncUserTokenPool(m_Settings.MaxConnections);

            // Create connections count enforcer
            m_MaxConnectionsEnforcer = new Semaphore(m_Settings.MaxConnections, m_Settings.MaxConnections);

            //Microsoft's example called these from Main method, which you 
            //can easily do if you wish.
            //Init();
            //StartListen();
        }

        //____________________________________________________________________________
        // initializes the server by preallocating reusable buffers and 
        // context objects (SocketAsyncEventArgs objects).  
        //It is NOT mandatory that you preallocate them or reuse them. But, but it is 
        //done this way to illustrate how the API can 
        // easily be used to create reusable objects to increase server performance.

        internal void Init()
        {
#if NET_DEBUG
            Debug.Log("Init method");
            DealWithThreadsForTesting("Init()");
#endif

            // Allocate one large byte buffer block, which all I/O operations will 
            //use a piece of. This gaurds against memory fragmentation.
            m_ReceiveBufferManager.InitBuffer();
#if NET_DEBUG
            Debug.Log("Starting creation of accept SocketAsyncEventArgs pool:");
#endif
            // preallocate pool of SocketAsyncEventArgs objects for accept operations           
            for (int i = 0; i < m_Settings.MaxAcceptOps; i++)
            {
                // add SocketAsyncEventArg to the pool
                m_AcceptPool.Push(CreateNewSaeaForAccept(m_AcceptPool));
            }
#if NET_DEBUG
            Debug.Log("Starting creation of receive/send SocketAsyncEventArgs pool");
#endif

            //The pool that we built ABOVE is for SocketAsyncEventArgs objects that do
            // accept operations. 
            //Now we will build a separate pool for SAEAs objects 
            //that do receive/send operations. One reason to separate them is that accept
            //operations do NOT need a buffer, but receive/send operations do. 
            //ReceiveAsync and SendAsync require
            //a parameter for buffer size in SocketAsyncEventArgs.Buffer.
            // So, create pool of SAEA objects for receive/send operations.
            AsyncUserToken userToken;
            int tokenId;

            for (int i = 0; i < m_Settings.MaxConnections; i++)
            {
                //receive
                tokenId = m_UserTokenPool.AssignTokenId() + 1000000;
                userToken = CreateUserToken(tokenId);
                m_UserTokenPool.Push(userToken);
            }
        }

        AsyncUserToken CreateUserToken(int tokenId)
        {
            //Allocate the SocketAsyncEventArgs object for this loop, 
            //to go in its place in the stack which will be the pool
            //for receive/send operation context objects.
            SocketAsyncEventArgs receiveSaea = new SocketAsyncEventArgs();

            // assign a byte buffer from the buffer block to 
            //this particular SocketAsyncEventArg object
            m_ReceiveBufferManager.SetBuffer(receiveSaea);

            //Attach the SocketAsyncEventArgs object
            //to its event handler. Since this SocketAsyncEventArgs object is 
            //used for both receive and send operations, whenever either of those 
            //completes, the IO_Completed method will be called.
            receiveSaea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_ReceiveCompleted);

            SocketAsyncEventArgs sendSaea = new SocketAsyncEventArgs();
            m_SendBufferManager.SetBuffer(receiveSaea);
            sendSaea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_SendCompleted);

            //We can store data in the UserToken property of SAEA object.
            AsyncUserToken userToken = new AsyncUserToken(receiveSaea, sendSaea, m_Settings.HeaderLength, tokenId);
            receiveSaea.UserToken = userToken;
            sendSaea.UserToken = userToken;
            userToken.messageHandler = onReceiveMessage;

            return userToken;
        }

        //____________________________________________________________________________
        // This method is called when we need to create a new SAEA object to do
        //accept operations. The reason to put it in a separate method is so that
        //we can easily add more objects to the pool if we need to.
        //You can do that if you do NOT use a buffer in the SAEA object that does
        //the accept operations.
        internal SocketAsyncEventArgs CreateNewSaeaForAccept(SocketAsyncEventArgsPool pool)
        {
            //Allocate the SocketAsyncEventArgs object. 
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();

            //SocketAsyncEventArgs.Completed is an event, (the only event,) 
            //declared in the SocketAsyncEventArgs class.
            //See http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.completed.aspx.
            //An event handler should be attached to the event within 
            //a SocketAsyncEventArgs instance when an asynchronous socket 
            //operation is initiated, otherwise the application will not be able 
            //to determine when the operation completes.
            //Attach the event handler, which causes the calling of the 
            //AcceptEventArg_Completed object when the accept op completes.
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);

            AcceptUserToken theAcceptOpToken = new AcceptUserToken(pool.AssignTokenId() + 10000);
            acceptEventArg.UserToken = theAcceptOpToken;

            return acceptEventArg;

            // accept operations do NOT need a buffer.                
            //You can see that is true by looking at the
            //methods in the .NET Socket class on the Microsoft website. AcceptAsync does
            //not take require a parameter for buffer size.
        }

        //____________________________________________________________________________
        // This method starts the socket server such that it is listening for 
        // incoming connection requests.            
        internal void StartListen()
        {
#if NET_DEBUG
            Debug.Log("StartListen method. Before Listen operation is started.");
            DealWithThreadsForTesting("StartListen()");
#endif

            // create the socket which listens for incoming connections
            m_ListenSocket = new Socket(m_Settings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //bind it to the port
            m_ListenSocket.Bind(m_Settings.LocalEndPoint);

            // Start the listener with a backlog of however many connections.
            //"backlog" means pending connections. 
            //The backlog number is the number of clients that can wait for a
            //SocketAsyncEventArg object that will do an accept operation.
            //The listening socket keeps the backlog as a queue. The backlog allows 
            //for a certain # of excess clients waiting to be connected.
            //If the backlog is maxed out, then the client will receive an error when
            //trying to connect.
            //max # for backlog can be limited by the operating system.
            m_ListenSocket.Listen(m_Settings.Backlog);

#if NET_DEBUG
            Debug.Log("StartListen method Listen operation was just started.");
            Debug.Log("\r\n\r\n*************************\r\n** Server is listening **\r\n*************************\r\n\r\nAfter you are finished, type 'Z' and press\r\nEnter key to terminate the server process.\r\nIf you terminate it by clicking X on the Console,\r\nthen the log will NOT write correctly.\r\n");
#endif
            // Calls the method which will post accepts on the listening socket.            
            // This call just occurs one time from this StartListen method. 
            // After that the StartAccept method will be called in a loop.
            StartAccept();
        }

        //____________________________________________________________________________
        // Begins an operation to accept a connection request from the client         
        internal void StartAccept()
        {
#if NET_DEBUG
            Debug.Log("StartAccept method");
#endif
            SocketAsyncEventArgs acceptEventArg;

            //Get a SocketAsyncEventArgs object to accept the connection.                        
            //Get it from the pool if there is more than one in the pool.
            //We could use zero as bottom, but one is a little safer.            
            if (m_AcceptPool.Count > 1)
            {
                try
                {
                    acceptEventArg = m_AcceptPool.Pop();
                }
                //or make a new one.
                catch
                {
                    acceptEventArg = CreateNewSaeaForAccept(m_AcceptPool);
                }
            }
            //or make a new one.
            else
            {
                acceptEventArg = CreateNewSaeaForAccept(m_AcceptPool);
            }


#if NET_DEBUG
            {
                AcceptUserToken theAcceptOpToken = (AcceptUserToken)acceptEventArg.UserToken;
                DealWithThreadsForTesting("StartAccept()", theAcceptOpToken);

                Debug.Log("still in StartAccept, id = " + theAcceptOpToken.TokenId);
            }
#endif

            //Semaphore class is used to control access to a resource or pool of 
            //resources. Enter the semaphore by calling the WaitOne method, which is 
            //inherited from the WaitHandle class, and release the semaphore 
            //by calling the Release method. This is a mechanism to prevent exceeding
            // the max # of connections we specified. We'll do this before
            // doing AcceptAsync. If maxConnections value has been reached,
            //then the application will pause here until the Semaphore gets released,
            //which happens in the CloseClientSocket method.            
            m_MaxConnectionsEnforcer.WaitOne();

            //Socket.AcceptAsync begins asynchronous operation to accept the connection.
            //Note the listening socket will pass info to the SocketAsyncEventArgs
            //object that has the Socket that does the accept operation.
            //If you do not create a Socket object and put it in the SAEA object
            //before calling AcceptAsync and use the AcceptSocket property to get it,
            //then a new Socket object will be created for you by .NET.            
            bool willRaiseEvent = m_ListenSocket.AcceptAsync(acceptEventArg);
            //Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is 
            //working asynchronously. The 
            //SocketAsyncEventArgs.Completed event on the acceptEventArg parameter 
            //will be raised upon completion of accept op.
            //AcceptAsync will call the AcceptEventArg_Completed
            //method when it completes, because when we created this SocketAsyncEventArgs
            //object before putting it in the pool, we set the event handler to do it.
            //AcceptAsync returns false if the I/O operation completed synchronously.            
            //The SocketAsyncEventArgs.Completed event on the acceptEventArg 
            //parameter will NOT be raised when AcceptAsync returns false.
            if (!willRaiseEvent)
            {
#if NET_DEBUG
                {
                    AcceptUserToken theAcceptOpToken = (AcceptUserToken)acceptEventArg.UserToken;
                    Debug.Log("StartAccept in if (!willRaiseEvent), accept token id " + theAcceptOpToken.TokenId);
                }
#endif

                //The code in this if (!willRaiseEvent) statement only runs 
                //when the operation was completed synchronously. It is needed because 
                //when Socket.AcceptAsync returns false, 
                //it does NOT raise the SocketAsyncEventArgs.Completed event.
                //And we need to call ProcessAccept and pass it the SAEA object.
                //This is only when a new connection is being accepted.
                // Probably only relevant in the case of a socket error.
                ProcessAccept(acceptEventArg);
            }
        }

        internal void SendMessage(AsyncUserToken asyncUserToken, byte[] data)
        {
            asyncUserToken.sendPacket.Add(data);
            if (!asyncUserToken.sendPacket.isSending)
            {
                StartSend(asyncUserToken.sendSaea);
            }
        }

        internal void SendToOthers(AsyncUserToken asyncUserToken, byte[] data)
        {
            foreach (var iter in m_Users)
            {
                if (iter.Value != asyncUserToken)
                {
                    iter.Value.sendPacket.Add(data);
                    if (!iter.Value.sendPacket.isSending)
                    {
                        StartSend(iter.Value.sendSaea);
                    }
                }
            }
        }

        //____________________________________________________________________________
        // This method is the callback method associated with Socket.AcceptAsync 
        // operations and is invoked when an async accept operation completes.
        // This is only when a new connection is being accepted.
        // Notice that Socket.AcceptAsync is returning a value of true, and
        // raising the Completed event when the AcceptAsync method completes.
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error. It might be better to put the code
            //in the ProcessAccept method.
#if NET_DEBUG
            {
                AcceptUserToken theAcceptOpToken = (AcceptUserToken)e.UserToken;
                Debug.Log("AcceptEventArg_Completed, id " + theAcceptOpToken.TokenId);
                DealWithThreadsForTesting("AcceptEventArg_Completed()", theAcceptOpToken);
            }
#endif
            ProcessAccept(e);
        }

        //____________________________________________________________________________       
        //The e parameter passed from the AcceptEventArg_Completed method
        //represents the SocketAsyncEventArgs object that did
        //the accept operation. in this method we'll do the handoff from it to the 
        //SocketAsyncEventArgs object that will do receive/send.
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            // This is when there was an error with the accept op. That should NOT
            // be happening often. It could indicate that there is a problem with
            // that socket. If there is a problem, then we would have an infinite
            // loop here, if we tried to reuse that same socket.
            AcceptUserToken theAcceptOpToken = null;
            if (acceptEventArgs.SocketError != SocketError.Success)
            {
                // Loop back to post another accept op. Notice that we are NOT
                // passing the SAEA object here.
                LoopToStartAccept();


#if NET_DEBUG
                theAcceptOpToken = (AcceptUserToken)acceptEventArgs.UserToken;
                Debug.LogError("SocketError, accept id " + theAcceptOpToken.TokenId);
#endif

                //Let's destroy this socket, since it could be bad.
                HandleBadAccept(acceptEventArgs);

                //Jump out of the method.
                return;
            }


            theAcceptOpToken = (AcceptUserToken)acceptEventArgs.UserToken;
#if NET_DEBUG
            Interlocked.Increment(ref numberOfAcceptedSockets);
            Debug.Log("ProcessAccept, accept id " + theAcceptOpToken.TokenId);
#endif


            // Get a SocketAsyncEventArgs object from the pool of receive/send op 
            //SocketAsyncEventArgs objects
            AsyncUserToken userToken = m_UserTokenPool.Pop();
            SocketAsyncEventArgs receiveEventArgs = userToken.receiveSaea;

            //Create sessionId in UserToken.
            userToken.CreateSessionId();
            userToken.socket = acceptEventArgs.AcceptSocket;
            m_Users[userToken.SessionId] = userToken;

            //A new socket was created by the AcceptAsync method. The 
            //SocketAsyncEventArgs object which did the accept operation has that 
            //socket info in its AcceptSocket property. Now we will give
            //a reference for that socket to the SocketAsyncEventArgs 
            //object which will do receive/send.
            receiveEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;

#if NET_DEBUG
            Debug.Log("Accept id " + theAcceptOpToken.TokenId + ". RecSend id " + userToken.TokenId + ".  Remote endpoint = " + IPAddress.Parse(((IPEndPoint)receiveEventArgs.AcceptSocket.RemoteEndPoint).Address.ToString()) + ": " + ((IPEndPoint)receiveEventArgs.AcceptSocket.RemoteEndPoint).Port.ToString() + ". client(s) connected = " + numberOfAcceptedSockets);
            theAcceptOpToken.socketHandleNumber = (int)acceptEventArgs.AcceptSocket.Handle;
            DealWithThreadsForTesting("ProcessAccept()", theAcceptOpToken);
            userToken.socketHandleNumber = (int)receiveEventArgs.AcceptSocket.Handle;
#endif

            //We have handed off the connection info from the
            //accepting socket to the receiving socket. So, now we can
            //put the SocketAsyncEventArgs object that did the accept operation 
            //back in the pool for them. But first we will clear 
            //the socket info from that object, so it will be 
            //ready for a new socket when it comes out of the pool.
            acceptEventArgs.AcceptSocket = null;
            m_AcceptPool.Push(acceptEventArgs);

#if NET_DEBUG
            Debug.Log("back to poolOfAcceptEventArgs goes accept id " + theAcceptOpToken.TokenId);
#endif

            StartReceive(receiveEventArgs);

            //Now that the accept operation completed, we can start another
            //accept operation, which will do the same. Notice that we are NOT
            //passing the SAEA object here.
            LoopToStartAccept();
        }

        //____________________________________________________________________________
        //LoopToStartAccept method just sends us back to the beginning of the 
        //StartAccept method, to start the next accept operation on the next 
        //connection request that this listening socket will pass of to an 
        //accepting socket. We do NOT actually need this method. You could
        //just call StartAccept() in ProcessAccept() where we called LoopToStartAccept().
        //This method is just here to help you visualize the program flow.
        private void LoopToStartAccept()
        {
#if NET_DEBUG
            Debug.Log("LoopToStartAccept");
#endif
            StartAccept();
        }


        //____________________________________________________________________________
        // Set the receive buffer and post a receive op.
        private void StartReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncUserToken receiveUserToken = (AsyncUserToken)receiveEventArgs.UserToken;
#if NET_DEBUG
            Debug.Log("StartReceive(), receiveUserToken id " + receiveUserToken.TokenId);
#endif

            //Set the buffer for the receive operation.
            //receiveEventArgs.SetBuffer(receiveUserToken.bufferOffsetReceive, m_Settings.BufferSize);                    

            // Post async receive operation on the socket.
            bool willRaiseEvent = receiveEventArgs.AcceptSocket.ReceiveAsync(receiveEventArgs);

            //Socket.ReceiveAsync returns true if the I/O operation is pending. The 
            //SocketAsyncEventArgs.Completed event on the e parameter will be raised 
            //upon completion of the operation. So, true will cause the IO_Completed
            //method to be called when the receive operation completes. 
            //That's because of the event handler we created when building
            //the pool of SocketAsyncEventArgs objects that perform receive/send.
            //It was the line that said
            //eventArgObjectForPool.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

            //Socket.ReceiveAsync returns false if I/O operation completed synchronously. 
            //In that case, the SocketAsyncEventArgs.Completed event on the e parameter 
            //will not be raised and the e object passed as a parameter may be 
            //examined immediately after the method call 
            //returns to retrieve the result of the operation.
            // It may be false in the case of a socket error.
            if (!willRaiseEvent)
            {
#if NET_DEBUG
                Debug.Log("StartReceive in if (!willRaiseEvent), receiveSendToken id " + receiveUserToken.TokenId);
#endif
                //If the op completed synchronously, we need to call ProcessReceive 
                //method directly. This will probably be used rarely, as you will 
                //see in testing.
                ProcessReceive(receiveEventArgs);
            }
        }

        //____________________________________________________________________________
        // This method is called whenever a receive or send operation completes.
        // Here "e" represents the SocketAsyncEventArgs object associated 
        //with the completed receive or send operation
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error.

            AsyncUserToken receiveSendToken = (AsyncUserToken)e.UserToken;
#if NET_DEBUG
            DealWithThreadsForTesting("IO_Completed()", receiveSendToken);
#endif
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
#if NET_DEBUG
                        Debug.Log("IO_Completed method in Receive, receiveSendToken id " + receiveSendToken.TokenId);
#endif
                    ProcessReceive(e);
                    break;

                case SocketAsyncOperation.Send:
#if NET_DEBUG
                    Debug.Log("IO_Completed method in Send, id " + receiveSendToken.TokenId);
#endif

                    ProcessSend(e);
                    break;

                default:
                    //This exception will occur if you code the Completed event of some
                    //operation to come to this method, by mistake.
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        void IO_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
#if NET_DEBUG
            if(e.LastOperation!= SocketAsyncOperation.Receive)
            {
                throw new ArgumentException("The last operation completed on the socket was not a receive");
            }
            else
#endif
            {
                ProcessReceive(e);
            }
        }

        void IO_SendCompleted(object sender, SocketAsyncEventArgs e)
        {
#if NET_DEBUG
            if (e.LastOperation != SocketAsyncOperation.Send)
            {
                throw new ArgumentException("The last operation completed on the socket was not a send");
            }
            else
#endif
            {
                ProcessSend(e);
            }
        }

        //____________________________________________________________________________
        // This method is invoked by the IO_Completed method
        // when an asynchronous receive operation completes. 
        // If the remote host closed the connection, then the socket is closed.
        // Otherwise, we process the received data. And if a complete message was
        // received, then we do some additional processing, to 
        // respond to the client.
        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncUserToken userToken = (AsyncUserToken)receiveEventArgs.UserToken;
            // If there was a socket error, close the connection. This is NOT a normal
            // situation, if you get an error here.
            // In the Microsoft example code they had this error situation handled
            // at the end of ProcessReceive. Putting it here improves readability
            // by reducing nesting some.
            if (receiveEventArgs.SocketError != SocketError.Success)
            {
#if NET_DEBUG
                Debug.Log("ProcessReceive ERROR, receiveUserToken id " + userToken.TokenId);
#endif

                userToken.Reset();
                CloseClientSocket(receiveEventArgs);
                m_UserTokenPool.Push(userToken);
                //Jump out of the ProcessReceive method.
                return;
            }

            // If no data was received, close the connection. This is a NORMAL
            // situation that shows when the client has finished sending data.
            if (receiveEventArgs.BytesTransferred == 0)
            {
#if NET_DEBUG
                Debug.Log("ProcessReceive NO DATA, receiveUserToken id " +  userToken.TokenId);
#endif

                userToken.Reset();
                CloseClientSocket(receiveEventArgs);
                m_UserTokenPool.Push(userToken);
                return;
            }

            //The BytesTransferred property tells us how many bytes 
            //we need to process.
            int remainingBytesToProcess = receiveEventArgs.BytesTransferred;
#if NET_DEBUG
            Debug.Log("ProcessReceive " + userToken.TokenId + ". remainingBytesToProcess = " + remainingBytesToProcess);
            DealWithThreadsForTesting("ProcessReceive()", userToken);
#endif
            userToken.receivePacket.Receive(receiveEventArgs, remainingBytesToProcess);
            StartReceive(receiveEventArgs);
        }

        //____________________________________________________________________________
        //Post a send.    
        private void StartSend(SocketAsyncEventArgs sendEventArgs)
        {
            AsyncUserToken userToken = (AsyncUserToken)sendEventArgs.UserToken;
#if NET_DEBUG
            Debug.Log("StartSend, id " + userToken.TokenId);
            DealWithThreadsForTesting("StartSend()", userToken);
#endif

            //Set the buffer. You can see on Microsoft's page at 
            //http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.setbuffer.aspx
            //that there are two overloads. One of the overloads has 3 parameters.
            //When setting the buffer, you need 3 parameters the first time you set it,
            //which we did in the Init method. The first of the three parameters
            //tells what byte array to use as the buffer. After we tell what byte array
            //to use we do not need to use the overload with 3 parameters any more.
            //(That is the whole reason for using the buffer block. You keep the same
            //byte array as buffer always, and keep it all in one block.)
            //Now we use the overload with two parameters. We tell 
            // (1) the offset and
            // (2) the number of bytes to use, starting at the offset.

            //The number of bytes to send depends on whether the message is larger than
            //the buffer or not. If it is larger than the buffer, then we will have
            //to post more than one send operation. If it is less than or equal to the
            //size of the send buffer, then we can accomplish it in one send op.
            PacketSender sendPacket = userToken.sendPacket;

            if (sendPacket.Send(sendEventArgs, m_Settings.BufferSize) > 0)
            {
                //post asynchronous send operation
                bool willRaiseEvent = sendEventArgs.AcceptSocket.SendAsync(sendEventArgs);

                if (!willRaiseEvent)
                {
#if NET_DEBUG
                    Debug.Log("StartSend in if (!willRaiseEvent), receiveSendToken id " + userToken.TokenId);
#endif

                    ProcessSend(sendEventArgs);
                }
            }
        }

        //____________________________________________________________________________
        // This method is called by I/O Completed() when an asynchronous send completes.  
        // If all of the data has been sent, then this method calls StartReceive
        //to start another receive op on the socket to read any additional 
        // data sent from the client. If all of the data has NOT been sent, then it 
        //calls StartSend to send more data.        
        private void ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            AsyncUserToken userToken = (AsyncUserToken)sendEventArgs.UserToken;
#if NET_DEBUG
            Debug.Log("ProcessSend, id " + userToken.TokenId);
            DealWithThreadsForTesting("ProcessSend()", userToken);
#endif

            if (sendEventArgs.SocketError == SocketError.Success)
            {
#if NET_DEBUG
                Debug.Log("ProcessSend, if Success, id " + userToken.TokenId);
#endif
                PacketSender sendPacket = userToken.sendPacket;
                if (sendPacket.messageSize > 0)
                {
                    // So let's loop back to StartSend().
                    StartSend(sendEventArgs);
                }
                else
                {
                    sendPacket.isSending = false;
                }
            }
            else
            {
                //If we are in this else-statement, there was a socket error.
#if NET_DEBUG
                Debug.Log("ProcessSend ERROR, id " + userToken.TokenId + "\r\n");
#endif

                // We'll just close the socket if there was a
                // socket error when receiving data from the client.
                userToken.Reset();
                CloseClientSocket(sendEventArgs);
                m_UserTokenPool.Push(userToken);
            }
        }

        //____________________________________________________________________________
        // Does the normal destroying of sockets after 
        // we finish receiving and sending on a connection.        
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var receiveSendToken = (e.UserToken as AsyncUserToken);
#if NET_DEBUG
            Debug.Log("CloseClientSocket, id " + receiveSendToken.TokenId);
            DealWithThreadsForTesting("CloseClientSocket()", receiveSendToken);
#endif
            // do a shutdown before you close the socket
            try
            {
#if NET_DEBUG
                Debug.Log("CloseClientSocket, Shutdown try, id " + receiveSendToken.TokenId + "\r\n");
#endif
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            // throws if socket was already closed
            catch (Exception)
            {
#if NET_DEBUG
                Debug.Log("CloseClientSocket, Shutdown catch, id " + receiveSendToken.TokenId + "\r\n");
#endif
            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            e.AcceptSocket.Close();

            // decrement the counter keeping track of the total number of clients 
            //connected to the server, for testing
#if NET_DEBUG
            Interlocked.Decrement(ref numberOfAcceptedSockets);
            Debug.Log(receiveSendToken.TokenId + " disconnected. " + numberOfAcceptedSockets + " client(s) connected.");
#endif

            //Release Semaphore so that its connection counter will be decremented.
            //This must be done AFTER putting the SocketAsyncEventArg back into the pool,
            //or you can run into problems.
            m_MaxConnectionsEnforcer.Release();
        }

        //____________________________________________________________________________
        private void HandleBadAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            var acceptOpToken = (acceptEventArgs.UserToken as AcceptUserToken);
#if NET_DEBUG
            Debug.Log("Closing socket of accept id " + acceptOpToken.TokenId);
#endif
            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.           
            acceptEventArgs.AcceptSocket.Close();

            //Put the SAEA back in the pool.
            m_AcceptPool.Push(acceptEventArgs);
        }

        //____________________________________________________________________________
        internal void CleanUpOnExit()
        {
            DisposeAllSaeaObjects();
        }

        //____________________________________________________________________________
        private void DisposeAllSaeaObjects()
        {
            SocketAsyncEventArgs eventArgs;
            while (m_AcceptPool.Count > 0)
            {
                eventArgs = m_AcceptPool.Pop();
                eventArgs.Dispose();
            }

            AsyncUserToken userToken;
            while (m_UserTokenPool.Count > 0)
            {
                userToken = m_UserTokenPool.Pop();
                userToken.receiveSaea.Dispose();
                userToken.sendSaea.Dispose();
                userToken.receiveSaea = null;
                userToken.sendSaea = null;
            }
        }


        //____________________________________________________________________________
        //Display thread info.
        //Note that there is NOT a 1:1 ratio between managed threads 
        //and system (native) threads.
        //
        //Overloaded.
        //Use this one after the DataHoldingUserToken is available.
        //
        private void DealWithThreadsForTesting(string methodName, AsyncUserToken receiveSendToken)
        {
#if NET_DEBUG
            StringBuilder sb = new StringBuilder();
            sb.Append(" In " + methodName + ", receiveSendToken id " + receiveSendToken.TokenId + ". Thread id " + Thread.CurrentThread.ManagedThreadId + ". Socket handle " + receiveSendToken.socketHandleNumber + ".");
            sb.Append(DealWithNewThreads());

            Debug.Log(sb.ToString());            
#endif
        }

        //Use this for testing, when there is NOT a UserToken yet. Use in SocketListener
        //method or Init().
        private void DealWithThreadsForTesting(string methodName)
        {
#if NET_DEBUG
            StringBuilder sb = new StringBuilder();
            sb.Append(" In " + methodName + ", no usertoken yet. Thread id " + Thread.CurrentThread.ManagedThreadId);
            sb.Append(DealWithNewThreads());
            Debug.Log(sb.ToString());
#endif
        }

        //____________________________________________________________________________
        //Display thread info.
        //Overloaded.
        //Use this one in method where AcceptOpUserToken is available.
        //
        private void DealWithThreadsForTesting(string methodName, AcceptUserToken theAcceptOpToken)
        {
            StringBuilder sb = new StringBuilder();
            string hString = hString = ". Socket handle " + theAcceptOpToken.socketHandleNumber;
            sb.Append(" In " + methodName + ", acceptToken id " + theAcceptOpToken.TokenId + ". Thread id " + Thread.CurrentThread.ManagedThreadId + hString + ".");
            sb.Append(DealWithNewThreads());
#if NET_DEBUG
            Debug.Log(sb.ToString());            
#endif
        }

        //____________________________________________________________________________
        //Display thread info.
        //called by DealWithThreadsForTesting
        private string DealWithNewThreads()
        {
#if NET_DEBUG
            StringBuilder sb = new StringBuilder();
            bool newThreadChecker = false;
            lock (m_LockerForThreadHashSet)
            {
                if (m_ManagedThreadIds.Add(Thread.CurrentThread.ManagedThreadId) == true)
                {
                    m_ManagedThreads.Add(Thread.CurrentThread);
                    newThreadChecker = true;
                }
            }
            if (newThreadChecker == true)
            {
                
                //Display system threads
                //Note that there is NOT a 1:1 ratio between managed threads 
                //and system (native) threads.
                sb.Append("\r\n**** New managed thread.  Threading info:\r\nSystem thread numbers: ");
                m_ArrayOfLiveThreadsInThisProcess = m_TheProcess.Threads; //for testing only
                
                foreach (ProcessThread theNativeThread in m_ArrayOfLiveThreadsInThisProcess)
                {
                    sb.Append(theNativeThread.Id.ToString() + ", ");
                }
                //Display managed threads
                //Note that there is NOT a 1:1 ratio between managed threads 
                //and system (native) threads.
                sb.Append("\r\nManaged threads that have been used: ");               
                foreach (int theManagedThreadId in m_ManagedThreadIds)
                {
                    sb.Append(theManagedThreadId.ToString() + ", ");                    
                }

                //Managed threads above were/are being used.
                //Managed threads below are still being used now.
                sb.Append("\r\nManagedthread.IsAlive true: ");                
                foreach (Thread theManagedThread in m_ManagedThreads)
                {
                    if (theManagedThread.IsAlive == true)
                    {
                        sb.Append(theManagedThread.ManagedThreadId.ToString() + ", ");
                    }
                }                
                sb.Append("\r\nEnd thread info.");
            }
            return sb.ToString();
#else
            return string.Empty;
#endif
        }

    }
}
