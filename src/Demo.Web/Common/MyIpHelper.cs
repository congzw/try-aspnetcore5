using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Demo.Web.Common
{
    public class MyIpHelper
    {
        public static MyIpHelper Instance = new MyIpHelper();
        
        public MyIpHelper()
        {
            InnerIpStarts = new List<string>
            {
                "10.", "010."
                ,"172.16.", "172.016."
                ,"172.17.", "172.017."
                ,"172.18.", "172.018."
                ,"172.19.", "172.019."
                ,"172.20.", "172.020."
                ,"172.21.", "172.021."
                ,"172.22.", "172.022."
                ,"172.23.", "172.023."
                ,"172.24.", "172.024."
                ,"172.25.", "172.025."
                ,"172.26.", "172.026."
                ,"172.27.", "172.027."
                ,"172.28.", "172.028."
                ,"172.29.", "172.029."
                ,"172.30.", "172.030."
                ,"172.31.", "172.031."
                ,"172.32.", "172.032."
                ,"172.33.", "172.033."
                ,"172.34.", "172.034."
                ,"172.35.", "172.035."
                ,"192.168."
                ,"127.0.0.1"
                ,"localhost" 
                ,"::1"
            };
        }
        
        //不使用网关掩码,直接比较字符串即可（简单实现）
        //RFC1918 预留内网IP
        //A 10.0.0.0 => 10.255.255.255
        //B 172.16.0.0 => 172.31.255.255
        //C 192.168.0.0 => 192.168.255.255
        public List<string> InnerIpStarts { get; set; }
        
        public string GetRemoteIpV4(ConnectionInfo connectionInfo)
        {
            var ip = connectionInfo?.RemoteIpAddress?.ToString();
            return FixIpV4(ip);
        }

        public List<IPAddress> GetHostAddresses()
        {
            var hostName = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(hostName).ToList();
            return ips;
        }

        public bool IsInnerIp( string ipToMatch, List<InnerNetSetting> innerNetSettings = null)
        {
            var ipV4 = FixIpV4(ipToMatch);

            //国际规定，这四类必是内网IP
            //10.x.x.x(010.x.x.x)
            //172.16.x.x(172.016.x.x)至172.31.x.x(172.031.x.x)
            //192.168.x.x
            //127.0.0.1
            //localhost
            if (InnerIpStarts.Any(m => ipV4.StartsWith(m, StringComparison.InvariantCultureIgnoreCase)))
            {
                //如果发现此类Ip，直接判定为内网
                return true;
            }

            if (innerNetSettings == null || innerNetSettings.Count == 0)
            {
                return false;
            }

            return innerNetSettings.Any(innerNetSetting => IsMatchIpV4(innerNetSetting.SubNetMask, innerNetSetting.DefaultGateway, ipV4));
        }
        
        //192.168.1.010 => 192.168.1.10
        private string FixIpV4(string ip)
        {
            //127.0.0.1
            //localhost
            //::1
            if ("127.0.0.1".Equals(ip, StringComparison.OrdinalIgnoreCase)
                || "localhost".Equals(ip, StringComparison.OrdinalIgnoreCase)
                || "::1".Equals(ip, StringComparison.OrdinalIgnoreCase)
                || "0.0.0.1".Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                return "127.0.0.1";
            }

            var items = ip.Split('.');
            if (items.Length != 4)
            {
                return ip;
            }

            return string.Join('.', items.Select(x => Convert.ToInt32(x)));
        }

        private bool IsMatchIpV4(string subnetMask, string defaultGateway, string ipToMatch)
        {
            var maskTool = IPAddress.Parse(subnetMask.Trim());
            var gatewayTool = IPAddress.Parse(defaultGateway.Trim());
            var ipToMatchTool = IPAddress.Parse(ipToMatch.Trim());

            var innerIpCal = IPAddress.NetworkToHostOrder(IPAddress.HostToNetworkOrder(GetIpAddressLong(gatewayTool)) & IPAddress.HostToNetworkOrder(GetIpAddressLong(maskTool)));
            var ipToMatchCal = IPAddress.NetworkToHostOrder(IPAddress.HostToNetworkOrder(GetIpAddressLong(ipToMatchTool)) & IPAddress.HostToNetworkOrder(GetIpAddressLong(maskTool)));

            return innerIpCal == ipToMatchCal;
        }
        
        private static long GetIpAddressLong(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return BitConverter.ToInt32(bytes, 0);
        }
    }
    
    public class NetInfo
    {
        public List<string> V6Ips { get; set; } = new List<string>();
        public List<string> V4Ips { get; set; } = new List<string>();
        public string RequestIp { get; set; }
        public bool IsInnerIp { get; set; }
        

        public HttpConnectionInfo HttpConnectionInfo { get; set; }
    }

    public class HttpConnectionInfo
    {
        public string RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public string LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
    }


    public class InnerNetSetting
    {
        public string SubNetMask { get; set; }
        public string DefaultGateway { get; set; }
    }

    public static class MyIpHelperExtensions
    {
        public static NetInfo GetNetInfo(this MyIpHelper myIpHelper, ConnectionInfo connectionInfo)
        {
            var netInfo = new NetInfo();

            var connInfo = new HttpConnectionInfo();
            connInfo.LocalPort = connectionInfo.LocalPort;
            connInfo.LocalIpAddress = connectionInfo.LocalIpAddress?.ToString();
            connInfo.RemotePort = connectionInfo.RemotePort;
            connInfo.RemoteIpAddress = connectionInfo.RemoteIpAddress?.ToString();
            netInfo.HttpConnectionInfo = connInfo;


            netInfo.RequestIp = myIpHelper.GetRemoteIpV4(connectionInfo);
            netInfo.V4Ips = myIpHelper.GetHostAddresses().Select(x => x.MapToIPv4().ToString()).ToList();
            netInfo.V6Ips = myIpHelper.GetHostAddresses().Select(x => x.MapToIPv6().ToString()).ToList();

            netInfo.IsInnerIp = myIpHelper.IsInnerIp(netInfo.RequestIp, null);
            return netInfo;
        }
    }
}
