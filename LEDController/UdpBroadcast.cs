using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace LEDController
{
    class UdpBroadcast
    {
        static public List<Device> BroadcastLEDController(string sentmessage, string receivedmessage, int port)
        {
            List<Device> FoundDevices = new List<Device>();
            try
            {
                UdpClient UdpClient = new UdpClient(port);
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, port);

                byte[] sendbuf = Encoding.ASCII.GetBytes(sentmessage);
                UdpClient.Send(sendbuf, sendbuf.Length, endpoint);

                Thread.Sleep(1000);

                while (UdpClient.Available > 0)
                {
                    byte[] ReceivedBytes = UdpClient.Receive(ref endpoint);

                    List<Match> Matches = SearchDelimiters(ReceivedBytes);

                    if (Matches.Count != 0)
                    {
                        byte[] macAddress = new byte[6];
                        byte[] ipAddres = new byte[4];
                        string macType = "";
                        string devName = "";

                        for (int i = 0; i < Matches.Count; i++)
                        {
                            if (Matches[i].Delimiter == 0x0a)
                            {
                                int LengthOfContent = Matches[i].EndPosition - Matches[i].Startposition - 2;
                                byte[] temp = new byte[LengthOfContent];
                                Array.Copy(ReceivedBytes, Matches[i].Startposition + 1, temp, 0, LengthOfContent);
                                if (Encoding.ASCII.GetString(temp) == receivedmessage)
                                {
                                    for (int j = 0; j < Matches.Count; j++)
                                    {
                                        if (Matches[j].Delimiter == 0x02)
                                        {
                                            LengthOfContent = Matches[j].EndPosition - Matches[j].Startposition - 2;
                                            temp = new byte[LengthOfContent];
                                            Array.Copy(ReceivedBytes, Matches[j].Startposition + 1, macAddress, 0, LengthOfContent);
                                        }

                                        if (Matches[j].Delimiter == 0x03)
                                        {
                                            LengthOfContent = Matches[j].EndPosition - Matches[j].Startposition - 2;
                                            temp = new byte[LengthOfContent];
                                            Array.Copy(ReceivedBytes, Matches[j].Startposition + 1, temp, 0, LengthOfContent);
                                            macType = Encoding.ASCII.GetString(temp);
                                        }
                                        if (Matches[j].Delimiter == 0x04)
                                        {
                                            LengthOfContent = Matches[j].EndPosition - Matches[j].Startposition - 2;
                                            temp = new byte[LengthOfContent];
                                            Array.Copy(ReceivedBytes, Matches[j].Startposition + 1, temp, 0, LengthOfContent);
                                            devName = Encoding.ASCII.GetString(temp);
                                        }
                                        if (Matches[j].Delimiter == 0x05)
                                        {
                                            LengthOfContent = Matches[j].EndPosition - Matches[j].Startposition - 2;
                                            temp = new byte[LengthOfContent];
                                            Array.Copy(ReceivedBytes, Matches[j].Startposition + 1, ipAddres, 0, LengthOfContent);
                                        }
                                    }
                                }
                            }
                        }
                        FoundDevices.Add(new Device
                        {
                            MacAddress = macAddress,
                            MacType = macType,
                            IPAddress = ipAddres,
                            DeviceName = devName
                        });
                    }
                }
                UdpClient.Close();
            }

            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
            return FoundDevices;
        }

        static private List<Match> SearchDelimiters(byte[] ReceivedBytes)
        {
            List<Match> Matches = new List<Match>();
            for (int i = 1; i < ReceivedBytes.Length; i++)
            {
                if (ReceivedBytes[i] == 0x0a && ReceivedBytes[i - 1] == 0x0d)
                {
                    if (Matches.Count == 0)
                    {
                        Matches.Add(new Match
                        {
                            Startposition = 0,
                            Delimiter = ReceivedBytes[0],
                            EndPosition = i
                        });
                    }
                    else
                    {
                        Match PreviousItem = Matches[Matches.Count - 1];
                        Matches.Add(new Match
                        {
                            Startposition = PreviousItem.EndPosition + 1,
                            Delimiter = ReceivedBytes[PreviousItem.EndPosition + 1],
                            EndPosition = i
                        });
                    }
                }
            }
            return Matches;
        }
    }

    public class Match
    {
        public int Startposition { get; set; }
        public int Delimiter { get; set; }
        public int EndPosition { get; set; }
    }

    class Device
    {
        public byte[] MacAddress { get; set; }
        public string MacType { get; set; }
        public string DeviceName { get; set; }
        public byte[] IPAddress { get; set; }
    }
}
