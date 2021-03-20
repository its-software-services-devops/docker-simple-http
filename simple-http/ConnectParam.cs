using System;

namespace simple_http
{
    class ConnectParam
    {
        public string DestinationIP { get; set; }
        public int Port { get; set; }
        public string Url { get; set; }
        public string Keyword { get; set; }
        public string BigQueryTable { get; set; }

        public ConnectParam(string ip, int port, string keyword, string table)
        {
            DestinationIP = ip;
            Port = port;
            BigQueryTable = table;
            Keyword = keyword;
        }

        public ConnectParam(string url, string keyword, string table)
        {
            Url = url;
            Keyword = keyword;
            BigQueryTable = table;
        }        
    }
}