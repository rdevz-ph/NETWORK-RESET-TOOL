namespace NetworkResetTool.Models
{
    public class AdapterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string IPv4Address { get; set; } = "N/A";
        public string IPv6Address { get; set; } = "N/A";
        public string SubnetMask { get; set; } = "N/A";
        public string Gateway { get; set; } = "N/A";
        public string DnsServers { get; set; } = "N/A";
        public string DhcpEnabled { get; set; } = "N/A";
    }
}
