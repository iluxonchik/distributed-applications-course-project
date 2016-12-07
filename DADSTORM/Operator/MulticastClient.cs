using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{
    public class MulticastClient
    {
        UdpClient client;
        IPEndPoint localEp;
        public MulticastClient(String multicastAddrs, int multicastEndPoint)
        {

            client = new UdpClient();

            client.ExclusiveAddressUse = false;
            //TODO: what is this??????
            localEp = new IPEndPoint(IPAddress.Any, multicastEndPoint);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEp);
            IPAddress multicastaddress = IPAddress.Parse(multicastAddrs);
            client.JoinMulticastGroup(multicastaddress);
        }
        public String receiveHeartBeat()
        {
            Byte[] data = client.Receive(ref localEp);
            String s = Encoding.ASCII.GetString(data);
            return s;
        }

    }


}
