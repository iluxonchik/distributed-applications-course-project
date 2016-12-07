using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{ 
    public class MulticastServer
    {

       
        UdpClient udpClient;
        IPEndPoint remoteep;
         public MulticastServer(String multicastAddrs,int multicastEndPoint)
        {
            udpClient = new UdpClient();
            IPAddress multicastaddress = IPAddress.Parse(multicastAddrs);
            udpClient.JoinMulticastGroup(multicastaddress);
            remoteep = new IPEndPoint(multicastaddress, multicastEndPoint);
           


        }


        public void sendHeartBeat(Byte[] sendBytes)
        {
            udpClient.Send(sendBytes, sendBytes.Length, remoteep);
        }
    }
}
