using Kztek.Tool.MathematicHelpers;
using Kztek.Tool.NetWorkHelpers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kztek.Tool.SocketHelpers
{
    public class UdpTools
    {
        public const string STX = "02";
        public const string ETX = "03";
        public static bool Start_UDP_Server(string ipAddress, int Port, ref Socket UdpServer, ref IPEndPoint ipEpBroadcast)
        {
            try
            {
                UdpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ipEpBroadcast = new IPEndPoint(IPAddress.Parse(ipAddress), Port);
                UdpServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                return true;
            }
            catch
            {

            }
            return false;
        }

        public static string ExecuteCommand_Ascii(string ip_Address, int port, string command, int delayTime = 500)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket? UdpServer = null;
                IPEndPoint? ipEpBroadcast = null;

                //if (NetWorkTools.IsPingSuccess(ip_Address, 50))
                //{
                if (Start_UDP_Server(ip_Address, port, ref UdpServer, ref ipEpBroadcast))
                {
                    //command = "SetRelay?/Relay=01/State=OFF";
                    byte[] bData = Encoding.ASCII.GetBytes(command);

                    // Send data
                    UdpServer.SendTo(bData, ipEpBroadcast);

                    UdpServer.ReceiveTimeout = 1000;
                    try
                    {
                        if (UdpServer.ReceiveBufferSize != 0)
                        {
                            byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                            int readLen = UdpServer.Receive(bRebuff);
                            byte checksum = 1;
                            for (int i = 1; i < readLen - 2; i++)
                                checksum += bRebuff[i];

                            message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);

                            ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                            ret = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = ex.ToString();
                    }
                    finally
                    {
                        UdpServer.Close();
                        UdpServer.Dispose();
                        ipEpBroadcast = null;
                    }
                }
                else
                {
                    ret = "ERROR: Socket Start Error";
                }
                //}

                //else
                //{
                //    ret = "ERROR: Ping Error";
                //}
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }
            return ret;
        }
        public static string ExecuteCommand_UTF8(string comport, int baudrate, string command, int delayTime = 100)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;

                if (NetWorkTools.IsPingSuccess(comport, 500))
                {
                    if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                    {
                        //command = "SetRelay?/Relay=01/State=OFF";
                        byte[] bData = Encoding.UTF8.GetBytes(command);

                        UdpServer.SendTo(bData, ipEpBroadcast);

                        UdpServer.ReceiveTimeout = 2000;
                        Thread.Sleep(150);
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];
                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                ret = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer = null;
                            ipEpBroadcast = null;
                        }
                    }
                    else
                    {
                        ret = "Socket Start Error";
                    }
                    Thread.Sleep(delayTime);
                }
                else
                {
                    ret = "Ping Error";
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return ret;
        }

        public static string ExecuteCommand_Ascii(string comport, int baudrate, string command, int delayTime, string startStr = STX)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;

                if (NetWorkTools.IsPingSuccess(comport, 500))
                {
                    if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                    {
                        //command = "SetRelay?/Relay=01/State=OFF";
                        byte[] bData = Encoding.ASCII.GetBytes(command);
                        UdpServer.SendTo(bData, ipEpBroadcast);
                        UdpServer.ReceiveTimeout = 2000;
                        Thread.Sleep(150);
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];

                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                                if (IsSuccess(message[0], startStr))// && checksum == bRebuff[readLen - 2])
                                {
                                    ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                    string s1 = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                                    ret = Encoding.UTF8.GetString(bRebuff, 1, readLen - 2);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer.Dispose();
                            ipEpBroadcast = null;
                        }
                    }

                }
                else
                    Thread.Sleep(delayTime);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                GC.Collect();
            }
            return ret;
        }
        public static string ExecuteCommand_UTF8(string comport, int baudrate, string command, int delayTime, string startStr)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;

                if (NetWorkTools.IsPingSuccess(comport, 500))
                {
                    if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                    {
                        //command = "SetRelay?/Relay=01/State=OFF";
                        byte[] bData = Encoding.UTF8.GetBytes(command);
                        UdpServer.ReceiveTimeout = 2000;
                        UdpServer.SendTo(bData, ipEpBroadcast);

                        //UdpServer.ReceiveTimeout = 2000;
                        Thread.Sleep(150);
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];

                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                                if (IsSuccess(message[0], startStr))// && checksum == bRebuff[readLen - 2])
                                {
                                    ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                    string s1 = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                                    ret = Encoding.UTF8.GetString(bRebuff, 1, readLen - 2);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer = null;
                            ipEpBroadcast = null;
                        }
                        return ret;
                    }
                }
                else
                    Thread.Sleep(delayTime);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                GC.Collect();
            }
            return ret;
        }

        public static string ExecuteCommand(string comport, int baudrate, string command, int delayTime, Encoding encodingType)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;

                if (NetWorkTools.IsPingSuccess(comport, 500))
                {
                    if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                    {
                        byte[] bData = encodingType.GetBytes(command);
                        UdpServer.SendTo(bData, ipEpBroadcast);
                        UdpServer.ReceiveTimeout = 2000;
                        Thread.Sleep(150);
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];

                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);

                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                ret = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer.Dispose();
                            ipEpBroadcast = null;
                        }
                    }
                }
                else
                    Thread.Sleep(delayTime);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                GC.Collect();
            }
            return ret;
        }
        public static string ExecuteCommand(string comport, int baudrate, string command, int delayTime, string startStr, Encoding encodingType)
        {
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;

                //if (NetWorkTools.IsPingSuccess(comport, 500))
                //{
                if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                {
                    byte[] bData = encodingType.GetBytes(command);
                    UdpServer.SendTo(bData, ipEpBroadcast);
                    UdpServer.ReceiveTimeout = 2000;
                    try
                    {
                        if (UdpServer.ReceiveBufferSize != 0)
                        {
                            byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                            int readLen = UdpServer.Receive(bRebuff);
                            byte checksum = 1;
                            for (int i = 1; i < readLen - 2; i++)
                                checksum += bRebuff[i];

                            message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                            if (IsSuccess(message[0], startStr))// && checksum == bRebuff[readLen - 2])
                            {
                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                string s1 = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                                ret = Encoding.UTF8.GetString(bRebuff, 1, readLen - 2);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = ex.ToString();
                    }
                    finally
                    {
                        UdpServer.Close();
                        UdpServer = null;
                        ipEpBroadcast = null;
                    }
                }
                //}
                //else
                //    Thread.Sleep(delayTime);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                GC.Collect();
            }
            return ret;
        }

        public static bool IsSuccess(string response, string searchStr)
        {
            if (response.Contains(searchStr))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<string> ReceiveSocketResponseAsync(string deviceId, string deviceName, string comport, int baudrate, string cmd, bool isRetry)
        {
            string response = await Task.Run(() =>
            {
                return ExecuteCommand_Ascii(comport, baudrate, cmd, 500);
            });

            if (string.IsNullOrEmpty(response) && isRetry)
            {
                await Task.Delay(200);
                return await ReceiveSocketResponseAsync(deviceId, deviceName, comport, baudrate, cmd, false);
            }
            return response ?? string.Empty;
        }
    }
}

