using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using TcpConnector;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum PacketType {
            SetName,
            Message
        }

        private Client client;

        private Dictionary<uint, string> clientNames = new Dictionary<uint, string>();
        
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (client != null)
                client.Disconnect();
        }

        private void BTN_Connect_Click(object sender, RoutedEventArgs e) {
            if (client != null && client.IsConnecting)
                return;
            if (client != null && client.Connected) {
                client.Disconnect();

                ChangeConnectButton("Connect");

                TB_Name.IsEnabled = true;
                TB_IPAddress.IsEnabled = true;
                TB_Port.IsEnabled = true;

                return;
            }

            string name = TB_Name.Text.Trim();
            if (name.Length >= 3 && IPAddress.TryParse(TB_IPAddress.Text, out IPAddress address) && int.TryParse(TB_Port.Text, out int port)) {
                ChangeConnectButton("Connecting...");
                TB_Name.IsEnabled = false;
                TB_IPAddress.IsEnabled = false;
                TB_Port.IsEnabled = false;

                client = new Client(address, port);
                client.OnDataReceived += Client_OnDataReceived;
                client.OnConnectionEstablished += Client_OnConnectionEstablished;
                client.OnConnectionFailed += Client_OnConnectionFailed;
                client.OnConnectionTerminated += Client_OnConnectionTerminated;
                client.Connect();
            } else {
                WriteToMessageHistory("IpAddress or Port are in the wrong format.", true);
            }
        }

        private void Client_OnDataReceived(byte[] data) {
            PacketType packetType = (PacketType)data[0];
            switch (packetType) {
                case PacketType.Message:
                    uint clientID = BitConverter.ToUInt32(data, 1);
                    string text = Encoding.ASCII.GetString(data, 5, data.Length - 5);

                    WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + " " + clientNames[clientID] + ": " + text);
                    break;

                case PacketType.SetName:
                    uint clientID2 = BitConverter.ToUInt32(data, 1);
                    int nameLength = BitConverter.ToInt32(data, 5);
                    string name = Encoding.ASCII.GetString(data, 9, nameLength);
                    clientNames[clientID2] = name;

                    WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + ": " + name + " joined!", true);
                    break;

                default:
                    break;
            }
        }

        private void BTN_SendMessage_Click(object sender, RoutedEventArgs e) {
            if (!client.Connected)
                return;

            string msg = TB_Message.Text.Trim();
            if (msg.Length <= 0)
                return;

            byte[] packetType = { (byte)PacketType.Message };
            byte[] message = Encoding.ASCII.GetBytes(TB_Message.Text);

            byte[] data = new byte[msg.Length + 1];
            Buffer.BlockCopy(packetType, 0, data, 0, packetType.Length);
            Buffer.BlockCopy(message, 0, data, 1, message.Length);

            client.SendData(data);
        }

        private void Client_OnConnectionFailed() {
            ChangeConnectButton("Connect");

            WriteToMessageHistory("Connection failed!", true);
        }

        private void Client_OnConnectionEstablished() {
            ChangeConnectButton("Disconnect");

            WriteToMessageHistory("Connection established!", true);

            byte[] packetType = { (byte)PacketType.SetName };

            string nameText;
            if (TB_Name.Dispatcher.CheckAccess()) {
                nameText = TB_Name.Text;

                byte[] name = Encoding.ASCII.GetBytes(nameText);

                byte[] data = new byte[nameText.Length + 5];
                Buffer.BlockCopy(packetType, 0, data, 0, packetType.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(name.Length), 0, data, 1, 4);
                Buffer.BlockCopy(name, 0, data, 5, name.Length);

                client.SendData(data);
            } else {
                TB_Name.Dispatcher.Invoke(new Action(() => {
                    nameText = TB_Name.Text;

                    byte[] name = Encoding.ASCII.GetBytes(nameText);

                    byte[] data = new byte[nameText.Length + 5];
                    Buffer.BlockCopy(packetType, 0, data, 0, packetType.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(name.Length), 0, data, 1, 4);
                    Buffer.BlockCopy(name, 0, data, 5, name.Length);

                    client.SendData(data);
                }));
            }
        }

        private void Client_OnConnectionTerminated() {
            ChangeConnectButton("Connect");

            WriteToMessageHistory("Connection broken", true);
        }

        private void WriteToMessageHistory(string text, bool isSystemMessage = false) {
            if (TB_MessageHistory.Dispatcher.CheckAccess()) {
                if (isSystemMessage)
                    text = "[SYSTEM] " + text;
                TB_MessageHistory.AppendText(text + Environment.NewLine);
                TB_MessageHistory.ScrollToEnd();
            } else {
                TB_MessageHistory.Dispatcher.Invoke(new Action(() => WriteToMessageHistory(text, isSystemMessage)));
            }
        }

        private void ChangeConnectButton(string text) {
            if (BTN_Connect.Dispatcher.CheckAccess())
                BTN_Connect.Content = text;
            else
                try {
                    BTN_Connect.Dispatcher.Invoke(new Action(() => BTN_Connect.Content = text));
                } catch (ThreadInterruptedException) {
                    // do nothing
                }
        }
    }
}
