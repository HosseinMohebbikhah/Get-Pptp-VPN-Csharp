using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GetPptpVPN
{
    class Program
    {
        [Obsolete]
        static void Main(string[] args)
        {
            CheckHost checkHost = CheckExist();
            Console.Title = $"VPN Connection{(checkHost.State ? (checkHost.Connected ? " - Connected" : " - Disconnected") : "")}";
        GoBack:
            if (!WriteMenu())
            {
                WriteMenu();
                goto GoBack;
            }
            while (true)
            {
                string enterWord = Console.ReadLine();
                if (int.TryParse(enterWord, out int result))
                {
                    switch (result)
                    {
                        case 1:
                            ShowVPN(checkHost);
                            break;
                        case 2:
                            DisconnectVPN(checkHost.State);
                            break;
                        case 3:
                            DeleteVPN();
                            break;
                        case 4:
                            return;
                        default:
                            SystemSounds.Exclamation.Play();
                            break;
                    }
                }
                else
                {
                    WriteMenu();
                    SystemSounds.Exclamation.Play();
                }
            }
        }

        public static void DeleteVPN()
        {
            void ResultDisconnect(object sender, EventArgs e)
            {
                Console.Title = $"VPN Connection";
                if ((int)sender == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("   Remove VPN(VPNConnection)");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("   We don't find it");
                }
                Console.ResetColor();
            }

            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();
                cmd.StandardInput.WriteLine("powershell");
                cmd.StandardInput.WriteLine("rasdial /disconnect");
                cmd.StandardInput.WriteLine("Remove-Vpnconnection -Name \"VPNConnection\"");
                cmd.StandardInput.WriteLine("y");
                cmd.StandardInput.WriteLine("exit");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    var line = cmd.StandardOutput.ReadLine();
                    if (line.Contains("not found"))
                    {
                        EventHandler handler = ResultDisconnect;
                        handler?.Invoke(0, new EventArgs());
                        break;
                    }
                    else if (line == "y")
                    {
                        EventHandler handler = ResultDisconnect;
                        handler?.Invoke(1, new EventArgs());
                        break;
                    }
                }
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
            }
            catch
            {
                EventHandler handler = ResultDisconnect;
                handler?.Invoke(0, new EventArgs());
                return;
            }
        }

        public static bool WriteMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n VPN PPTP ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("(Mohebbikhah)");
            Console.WriteLine("   Version 1.0.1\n");
            if (ReciveInternet().Result)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("     +");
                Console.ResetColor();
                Console.WriteLine(" Menu");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("     | 1. Connect VPN");
                Console.WriteLine("     | 2. Disconnect VPN");
                Console.WriteLine("     | 3. Delete VPN");
                Console.WriteLine("     | 4. Exit");
                Console.ResetColor();
                Console.Write("\n   Please Enter number's menu (1 - 4) > ");
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("     It's not avaraible internet.");
                return false;
            }
        }

        [Obsolete]
        public static void ShowVPN(CheckHost checkHost)
        {
            if (ReciveInternet().Result)
            {
                List<string> tt = new List<string>();
                GoBack:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\n       Starting Downloading...");
                Console.ResetColor();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    using (WebClient client = new WebClient())
                        File.WriteAllText("txtSource.txt", client.DownloadString("https://freevpn724.com/"));
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n       It's not avaraible Website.");
                    goto GoBack;
                }
                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n       Downloaded Successfully is {0:00}.{1:00}s", ts.Seconds, ts.Milliseconds / 10);
                Console.ResetColor();
                using (StreamReader reader = new StreamReader("txtSource.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("<strong>"))
                        {
                            string lin = RemoveWhitespace(line.Replace("<li>", "").Replace("</li>", "").Replace("<span>", "").Replace("</span>", "").Replace("<strong>", "").Replace("</strong>", ""));
                            tt.Add(lin);
                            if (lin.Contains("Password:"))
                                break;
                        }
                    }
                }

                List<string[]> NameHostIP = new List<string[]>();
                List<string> Login = new List<string>();

                foreach (string item in tt)
                {
                    if (!item.Contains("Username:") && !item.Contains("Password:"))
                    {
                        string Host = item.Remove(0, item.IndexOf(':') + 1);
                        string IP = HostToIP(Host);
                        NameHostIP.Add(new string[] { item.Replace(":", "").Replace(Host, "").Replace("VPNServer", ""), Host, IP });
                    }
                    else
                        break;
                }
                Login.AddRange(new string[] { tt[tt.Count - 2].Replace("Username:", ""), tt[tt.Count - 1].Replace("Password:", "") });
                Console.WriteLine();
                Console.WriteLine("\n       List Servers of countries : =>");
                int i = 1;
                foreach (var item in NameHostIP)
                {
                    Console.Write($"        {i}.");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(item[0]);
                    Console.ResetColor();
                    i++;
                }
                Console.Write($"\n       Enter custom VPN to connect (1 - {i - 1}) > ");

                void ResultConnection(object sender, EventArgs e)
                {
                    switch ((int)sender)
                    {
                        case 0:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n       Error to Connecting or Create VPN on your Computer");
                            Console.ResetColor();
                            break;
                        case 1:
                            Console.Title = $"VPN Connection - Connected";
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\n       Connected");
                            Console.ResetColor();
                            break;
                    }
                }

                if (int.TryParse(Console.ReadLine(), out int selectVPN))
                {
                    Console.Write($"       Connecting to {NameHostIP[selectVPN - 1][0]}...");
                    ConnectVPN(NameHostIP[selectVPN - 1], Login, checkHost, ResultConnection);
                }
                else
                {
                    WriteMenu();
                    SystemSounds.Exclamation.Play();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" It's not avaraible internet.");
                Console.ReadKey();
            }
        }

        public static Task DisconnectVPN(bool state)
        {
            return Task.Run(() =>
            {
                void ResultDisconnect(object sender, EventArgs e)
                {
                    if (state)
                        Console.Title = $"VPN Connection - Disconnected";
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("   Disconnected");
                    Console.ResetColor();
                }

                try
                {
                    Process cmd = new Process();
                    cmd.StartInfo.FileName = "cmd.exe";
                    cmd.StartInfo.RedirectStandardInput = true;
                    cmd.StartInfo.RedirectStandardOutput = true;
                    cmd.StartInfo.CreateNoWindow = true;
                    cmd.StartInfo.UseShellExecute = false;
                    cmd.Start();
                    cmd.StandardInput.WriteLine("powershell");
                    cmd.StandardInput.WriteLine("rasdial /disconnect");
                    cmd.StandardInput.WriteLine("exit");
                    while (!cmd.StandardOutput.EndOfStream)
                    {
                        var line = cmd.StandardOutput.ReadLine();
                        if (line == "Command completed successfully.")
                        {
                            EventHandler handler = ResultDisconnect;
                            handler?.Invoke(1, new EventArgs());
                        }
                    }
                    cmd.StandardInput.Flush();
                    cmd.StandardInput.Close();
                    cmd.WaitForExit();
                }
                catch
                {
                    EventHandler handler = ResultDisconnect;
                    handler?.Invoke(0, new EventArgs());
                }
                Console.ReadLine();
            });
        }

        public class CheckHost
        {
            public string IpHost { get; set; }
            public bool State { get; set; }
            public bool Connected { get; set; }
        }

        public static CheckHost CheckExist()
        {
            CheckHost checkHost = new CheckHost();
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();
                cmd.StandardInput.WriteLine("powershell");
                cmd.StandardInput.WriteLine("Get-VpnConnection -Name \"VPNConnection\"");
                cmd.StandardInput.WriteLine("exit");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    var line = cmd.StandardOutput.ReadLine();
                    if (line == "")
                        continue;
                    if (line.Contains("VPNConnection") && !line.Contains("Get-VpnConnection") && !line.Contains("not find"))
                        checkHost.State = true;
                    else if (line.Contains("ServerAddress") && !line.Contains("Get-VpnConnection") && !line.Contains("not find"))
                        checkHost.IpHost = line.Replace("ServerAddress", "").Replace(" ", "").Replace(":", "");
                    else if (line.Contains("ConnectionStatus"))
                    {
                        checkHost.Connected = line.Contains("Disconnected") ? false : true;
                        return checkHost;
                    }
                    else if (line.Contains("not find"))
                    {
                        checkHost.State = false;
                        break;
                    }
                }
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
                return checkHost;
            }
            catch
            { return checkHost; }
        }

        public static Task ConnectVPN(string[] HostIP, List<string> Login, CheckHost checkHost, EventHandler Result)
        {
            return Task.Run(() =>
            {
                if (!checkHost.State)
                {
                    try
                    {
                        Process cmd = new Process();
                        cmd.StartInfo.FileName = "cmd.exe";
                        cmd.StartInfo.RedirectStandardInput = true;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.StartInfo.CreateNoWindow = true;
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.Start();
                        cmd.StandardInput.WriteLine("powershell");
                        cmd.StandardInput.WriteLine("rasdial /disconnect");
                        cmd.StandardInput.WriteLine($"Add-VpnConnection -Name 'VPNConnection' -ServerAddress '{HostIP[2]}' -TunnelType Pptp -EncryptionLevel Required -PassThru");
                        cmd.StandardInput.WriteLine($"rasdial VPNConnection {Login[0]} {Login[1]}");
                        cmd.StandardInput.WriteLine("exit");
                        while (!cmd.StandardOutput.EndOfStream)
                        {
                            var line = cmd.StandardOutput.ReadLine();
                            if (line == "Successfully connected to VPNConnection.")
                            {
                                EventHandler handler = Result;
                                handler?.Invoke(1, new EventArgs());
                                break;
                            }
                            else if (line.Contains("problem") || line.Contains("hh netcfg.chm"))
                            {
                                EventHandler handler = Result;
                                handler?.Invoke(0, new EventArgs());
                                break;
                            }
                            else if (line.Contains("not find"))
                            {
                                EventHandler handler = Result;
                                handler?.Invoke(0, new EventArgs());
                                break;
                            }
                        }
                        cmd.StandardInput.Flush();
                        cmd.StandardInput.Close();
                        cmd.WaitForExit();
                    }
                    catch
                    {
                        EventHandler handler = Result;
                        handler?.Invoke(0, new EventArgs());
                    }
                }
                else
                {
                    if (checkHost.IpHost != HostIP[2])
                    {
                        try
                        {
                            Process cmd = new Process();
                            cmd.StartInfo.FileName = "cmd.exe";
                            cmd.StartInfo.RedirectStandardInput = true;
                            cmd.StartInfo.RedirectStandardOutput = true;
                            cmd.StartInfo.CreateNoWindow = true;
                            cmd.StartInfo.UseShellExecute = false;
                            cmd.Start();
                            cmd.StandardInput.WriteLine("powershell");
                            cmd.StandardInput.WriteLine("rasdial /disconnect");
                            cmd.StandardInput.WriteLine($"Set-VpnConnection -Name \"VPNConnection\" -ServerAddress \"{HostIP[2]}\" -PassThru");
                            cmd.StandardInput.WriteLine($"rasdial VPNConnection {Login[0]} {Login[1]}");
                            cmd.StandardInput.WriteLine("exit");
                            while (!cmd.StandardOutput.EndOfStream)
                            {
                                var line = cmd.StandardOutput.ReadLine();
                                if (line == "Successfully connected to VPNConnection.")
                                {
                                    EventHandler handler = Result;
                                    handler?.Invoke(1, new EventArgs());
                                }
                                if (line.Contains("not find"))
                                {
                                    EventHandler handler = Result;
                                    handler?.Invoke(0, new EventArgs());
                                    break;
                                }
                            }
                            cmd.StandardInput.Flush();
                            cmd.StandardInput.Close();
                            cmd.WaitForExit();
                        }
                        catch
                        {
                            EventHandler handler = Result;
                            handler?.Invoke(0, new EventArgs());
                        }
                    }
                    else
                    {
                        try
                        {
                            Process cmd = new Process();
                            cmd.StartInfo.FileName = "cmd.exe";
                            cmd.StartInfo.RedirectStandardInput = true;
                            cmd.StartInfo.RedirectStandardOutput = true;
                            cmd.StartInfo.CreateNoWindow = true;
                            cmd.StartInfo.UseShellExecute = false;
                            cmd.Start();
                            cmd.StandardInput.WriteLine("powershell");
                            cmd.StandardInput.WriteLine("rasdial /disconnect");
                            cmd.StandardInput.WriteLine($"rasdial VPNConnection {Login[0]} {Login[1]}");
                            cmd.StandardInput.WriteLine("exit");
                            while (!cmd.StandardOutput.EndOfStream)
                            {
                                var line = cmd.StandardOutput.ReadLine();
                                if (line == "Successfully connected to VPNConnection.")
                                {
                                    EventHandler handler = Result;
                                    handler?.Invoke(1, new EventArgs());
                                }
                                if (line.Contains("not find"))
                                {
                                    EventHandler handler = Result;
                                    handler?.Invoke(0, new EventArgs());
                                    break;
                                }
                            }
                            cmd.StandardInput.Flush();
                            cmd.StandardInput.Close();
                            cmd.WaitForExit();
                        }
                        catch
                        {
                            EventHandler handler = Result;
                            handler?.Invoke(0, new EventArgs());
                        }
                    }
                }
            });
        }

        public static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        [Obsolete]
        public static string HostToIP(string hostname)
        {
            IPHostEntry iphost = Dns.Resolve(hostname);
            IPAddress[] addresses = iphost.AddressList;
            StringBuilder addressList = new StringBuilder();
            foreach (IPAddress address in addresses)
                addressList.Append(address.ToString());
            return addressList.ToString();
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static Task<bool> ReciveInternet()
        {
            return Task.Run(() =>
            {
                return InternetGetConnectedState(out _, 0);
            });
        }
    }
}
