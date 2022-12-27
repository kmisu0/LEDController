using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;

namespace LEDController
{
    class tcpHandler
    {
        static Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public static List<Parameters> TcpClientHandler(string ipAddress, int port, List<Parameters> parameters)
        {
            try
            {
                byte[] receivedBytes = new byte[192];
                byte[] bytesToSend = new byte[101];
                string[] message = new string[13];
                char firstChar = '\x0002';
                char lastChar = '\x0003';
                List<Parameters> currentControllerParameters = new List<Parameters>();

                message[0] = firstChar.ToString();
                for (int i = 0; i <= 4; i++ )
                {
                    message[i + 1] = "PWM" + i.ToString() + "=" + parameters[i].fanSpeedSetpoint.ToString().PadLeft(3,'0') + "\n";
                    message[i + 6] = "LED" + i.ToString() + "=" + parameters[i].brightness.ToString().PadLeft(3, '0') + "\n";
                }
                message[11] = "COMM=001\n";
                message[12] = lastChar.ToString();

                foreach (string sample in message)
                {
                    byte delimit = 0;
                    byte[] sample2 = Encoding.ASCII.GetBytes(sample);
                    int offset;
                    offset = Array.IndexOf(bytesToSend, delimit);

                    System.Buffer.BlockCopy(sample2, 0, bytesToSend, offset, sample2.Length);
                }

                for (int i = 0; i < bytesToSend.Length; i++)
                {
                    if (bytesToSend[i] == 10)
                        bytesToSend[i] = 0;
                }

                try
                {
                    if (!sender.Connected)
                    {
                        sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sender.Connect(IPAddress.Parse(ipAddress), port);
                    }
                    sender.SendTimeout = 3000;
                    sender.ReceiveTimeout = 3000;
                    sender.LingerState = new LingerOption(true, 0);
                    sender.Blocking = true;

                    Thread.Sleep(1);

                    sender.SendBufferSize = bytesToSend.Length;
                    sender.ReceiveBufferSize = receivedBytes.Length;

                    sender.Send(bytesToSend);

                    Thread.Sleep(1);

                    while (sender.Available == 0)
                    {
                        Thread.Sleep(1);
                    }

                    while (sender.Available != receivedBytes.Length)
                    {
                        Thread.Sleep(1);
                    }

                    sender.Receive(receivedBytes);

                    if(receivedBytes[0] == 0x02)
                    {
                        if(receivedBytes[receivedBytes.Length-1] == 0x03)
                        {
                            List<Match> Matches = SearchDelimiters(receivedBytes);
                            int[] pwm = new int[5];
                            int[] icp = new int[5];
                            int[] led = new int[5];
                            int[] tmp = new int[5];

                            foreach (Match sample in Matches)
                            {
                                int valueLength = sample.EndPosition-sample.Startposition-5;
                                byte[] tempOfType = new byte[3];
                                byte[] tempOfNumber = new byte[1];
                                byte[] tempOfValue = new byte[valueLength];
                                Array.Copy(receivedBytes, sample.Startposition, tempOfType, 0, 3);
                                Array.Copy(receivedBytes, sample.Startposition + 3, tempOfNumber, 0, 1);
                                Array.Copy(receivedBytes, sample.Startposition + 5, tempOfValue, 0, valueLength);
                                string numberStr = Encoding.ASCII.GetString(tempOfNumber);
                                string type = Encoding.ASCII.GetString(tempOfType);
                                string value = Encoding.ASCII.GetString(tempOfValue);

                                int numberInt = Int32.Parse(numberStr);

                                switch(type)
                                {
                                    case "PWM":
                                        pwm[numberInt] = Int32.Parse(value);
                                        break;

                                    case "LED":
                                        led[numberInt] = Int32.Parse(value);
                                        break;

                                    case "ICP":
                                        icp[numberInt] = Int32.Parse(value);
                                        break;

                                    case "TMP":
                                        tmp[numberInt] = Int32.Parse(value);
                                        break;

                                    default:
                                    break;
                                }
                            }
                            for (int i = 0; i < 5; i++)
                            {
                                currentControllerParameters.Add(new Parameters
                                {
                                    brightness = led[i],
                                    fanActualSpeed = icp[i],
                                    fanSpeedSetpoint = pwm[i],
                                    tempOfHeatsink = tmp[i]
                                });
                            }
                        }
                    }
                    else
                    {
                        currentControllerParameters.Clear();
                    }
                    return currentControllerParameters;
                }
                catch (ArgumentNullException ane)
                {
                    MessageBox.Show("ArgumentNullException : {0}" + ane.ToString());
                }
                catch (SocketException se)
                {
                    MessageBox.Show("SocketException : {0}" + se.ToString());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unexpected exception : {0}" + e.ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            return null;
        }

        static private List<Match> SearchDelimiters(byte[] ReceivedBytes)
        {
            List<Match> Matches = new List<Match>();
            for (int i = 1; i < ReceivedBytes.Length; i++)
            {
                if (ReceivedBytes[i] == 0x00)
                {
                    if (Matches.Count == 0)
                    {
                        Matches.Add(new Match
                        {
                            Startposition = 1,
                            Delimiter = ReceivedBytes[1],
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

        static public void closeTcpClient()
        {
            if(sender.Connected)
            {
                sender.Close();
            }
        }
    }

    class Parameters
    {
        public int brightness { get; set; }
        public int fanSpeedSetpoint { get; set; }
        public int fanActualSpeed { get; set; }
        public int tempOfHeatsink { get; set; }
    }

}
