using System;
using System.Net.Sockets;
using System.Text;

namespace SocketAsyncServer
{
    internal class AcceptUserToken
    {
        //The only reason to use this UserToken in our app is to give it an identifier,
        //so that you can see it in the program flow. Otherwise, you would not need it.

        
        private int id; //for testing only
        internal int socketHandleNumber; //for testing only

        public AcceptUserToken(int identifier)
        {
            id = identifier;
            

            //if (Program.watchProgramFlow == true)   //for testing
            //{
            //    Program.testWriter.WriteLine("AcceptOpUserToken constructor, idOfThisObject " + id);
            //}
        }

        public int TokenId
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
    }
}
