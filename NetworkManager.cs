using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NetworkResetTool.Models;

namespace NetworkResetTool
{
    public class NetworkManager
    {
        public static List<AdapterInfo> GetActiveAdapters()
        {
            var adapters = new List<AdapterInfo>();
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    // Filter out loopback, virtual interfaces (optional, let's keep all ethernet & wireless)
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    // Show active or recently active adapters
                    var info = new AdapterInfo
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        Status = ni.OperationalStatus.ToString(),
                    };

                    try
                    {
                        var ipProps = ni.GetIPProperties();
                        
                        var ipv4List = new List<string>();
                        var ipv6List = new List<string>();
                        var maskList = new List<string>();

                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ipv4List.Add(addr.Address.ToString());
                                if (addr.IPv4Mask != null)
                                {
                                    maskList.Add(addr.IPv4Mask.ToString());
                                }
                            }
                            else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                // Remove scope ID
                                ipv6List.Add(addr.Address.ToString().Split('%')[0]);
                            }
                        }

                        info.IPv4Address = ipv4List.Count > 0 ? string.Join(", ", ipv4List) : "N/A";
                        info.IPv6Address = ipv6List.Count > 0 ? string.Join(", ", ipv6List) : "N/A";
                        info.SubnetMask = maskList.Count > 0 ? string.Join(", ", maskList) : "N/A";

                        // Default Gateways
                        var gatewayList = new List<string>();
                        foreach (var gw in ipProps.GatewayAddresses)
                        {
                            gatewayList.Add(gw.Address.ToString());
                        }
                        info.Gateway = gatewayList.Count > 0 ? string.Join(", ", gatewayList) : "N/A";

                        // DNS Servers
                        var dnsList = new List<string>();
                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            dnsList.Add(dns.ToString());
                        }
                        info.DnsServers = dnsList.Count > 0 ? string.Join(", ", dnsList) : "N/A";

                        // DHCP status
                        try
                        {
                            var ipv4Props = ipProps.GetIPv4Properties();
                            info.DhcpEnabled = ipv4Props.IsDhcpEnabled ? "Yes" : "No";
                        }
                        catch
                        {
                            info.DhcpEnabled = "Unknown";
                        }
                    }
                    catch
                    {
                        // Non-operational adapters may throw exceptions on property access
                    }

                    adapters.Add(info);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading network adapters: {ex.Message}");
            }

            return adapters;
        }

        public static async Task<(int ExitCode, string Output)> ExecuteCommandAsync(
            string fullCommand, 
            Action<string>? onOutputLine, 
            Action<string>? onErrorLine)
        {
            string fileName;
            string arguments;

            var parts = fullCommand.Split(' ', 2);
            fileName = parts[0];
            arguments = parts.Length > 1 ? parts[1] : string.Empty;

            var tcs = new TaskCompletionSource<(int ExitCode, string Output)>();
            var outputBuilder = new StringBuilder();

            // Set up ProcessStartInfo to run quietly with output redirected
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    onOutputLine?.Invoke(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    onErrorLine?.Invoke(e.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                tcs.SetResult((process.ExitCode, outputBuilder.ToString()));
            }
            catch (Exception ex)
            {
                onErrorLine?.Invoke($"Error executing command '{fullCommand}': {ex.Message}");
                tcs.SetResult((-1, ex.Message));
            }
            finally
            {
                process.Dispose();
            }

            return await tcs.Task;
        }

        public static bool GetInsecureGuestLogonsState()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AllowInsecureGuestAuth");
                        if (val != null && val is int intVal)
                        {
                            return intVal == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading registry: {ex.Message}");
            }
            return false;
        }

        public static bool SetInsecureGuestLogonsState(bool enabled)
        {
            try
            {
                using (var parentKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanWorkstation", true))
                {
                    if (parentKey != null)
                    {
                        using (var key = parentKey.CreateSubKey("Parameters"))
                        {
                            if (key != null)
                            {
                                key.SetValue("AllowInsecureGuestAuth", enabled ? 1 : 0, RegistryValueKind.DWord);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing registry: {ex.Message}");
            }
            return false;
        }

        public static async Task<bool> IsServiceRunningAsync(string serviceName)
        {
            var result = await ExecuteCommandAsync($"sc query {serviceName}", null, null);
            return result.Output.Contains("RUNNING");
        }
    }
}
