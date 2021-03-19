using System;

namespace simple_http
{
    class ConnectParam
    {
        public string DestinationIP { get; set; }
        public int Port { get; set; }

        public ConnectParam(string ip, int port)
        {
            DestinationIP = ip;
            Port = port;
        }
    }
}