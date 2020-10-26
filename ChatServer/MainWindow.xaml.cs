using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace ChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum PacketType
        {
            SetName,
            Message
        }

        private bool isRunning = false;

        private Server server;

        private Dictionary<uint, string> clients;
        
        public MainWindow() {
            InitializeComponent();

            clients = new Dictionary<uint, string>();
        }
        
        private void BTN_Start_Click(object sender, RoutedEventArgs e) {
            if (isRunning) {
                server.Stop();
                isRunning = false;

                WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + ": Server stopped!", true);

                ChangeStartButton("Start");

                TB_Port.IsEnabled = true;

                return;
            }

            int.TryParse(TB_Port.Text, out int port);
            server = new Server(port);
            server.OnClientConnect += Server_OnClientConnect;
            server.OnClientDisconnect += Server_OnClientDisconnect;
            server.OnDataReceived += Server_OnDataReceived;
            server.Start();

            isRunning = true;

            WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + ": Server started!", true);

            ChangeStartButton("Stop");

            TB_Port.IsEnabled = false;
        }

        private void Server_OnClientConnect(uint clientID) {
            clients.Add(clientID, "");
        }

        private void Server_OnClientDisconnect(uint clientID) {
            WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + ": " + clients[clientID] + " disconnected!", true);

            clients.Remove(clientID);
        }

        private void Server_OnDataReceived(uint clientID, byte[] data) {
            PacketType packetType = (PacketType)data[0];
            switch (packetType) {
                case PacketType.Message:
                    WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + " " + clients[clientID] + ": " + Encoding.ASCII.GetString(data, 1, data.Length - 1));

                    byte[] msg = new byte[data.Length + 4];
                    Buffer.BlockCopy(data, 0, msg, 0, 1);
                    Buffer.BlockCopy(BitConverter.GetBytes(clientID), 0, msg, 1, 4);
                    Buffer.BlockCopy(data, 1, msg, 5, data.Length - 1);

                    server.SendData(msg);
                    break;

                case PacketType.SetName:
                    int nameLength = BitConverter.ToInt32(data, 1);
                    string name = Encoding.ASCII.GetString(data, 5, nameLength);
                    clients[clientID] = name;

                    WriteToMessageHistory(DateTime.Now.ToString("HH:mm:ss") + ": User " + name + " joined!", true);

                    byte[] setName = new byte[1 + 4 + 4 + nameLength];
                    Buffer.BlockCopy(data, 0, setName, 0, 1);
                    Buffer.BlockCopy(BitConverter.GetBytes(clientID), 0, setName, 1, 4);
                    Buffer.BlockCopy(data, 1, setName, 5, 4);
                    Buffer.BlockCopy(data, 5, setName, 9, nameLength);

                    server.SendData(setName);
                    break;

                default:
                    break;
            }
        }

        private void WriteToMessageHistory(string text, bool isSystemMessage = false) {
            if (TB_MessageHistory.Dispatcher.CheckAccess()) {
                if (isSystemMessage)
                    text = "[SYSTEM] " + text;
                TB_MessageHistory.AppendText(text + Environment.NewLine);
                TB_MessageHistory.ScrollToEnd();
            } else {
                try {
                    TB_MessageHistory.Dispatcher.Invoke(new Action(() => {
                        TB_MessageHistory.AppendText(text + Environment.NewLine);
                        TB_MessageHistory.ScrollToEnd();
                    }));
                } catch (ThreadInterruptedException) {
                    // do nothing
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (isRunning) {
                server.Stop();
                isRunning = false;
            }
        }

        private void ChangeStartButton(string text) {
            if (BTN_Start.Dispatcher.CheckAccess())
                BTN_Start.Content = text;
            else
                try {
                    BTN_Start.Dispatcher.Invoke(new Action(() => BTN_Start.Content = text));
                } catch (ThreadInterruptedException) {
                    // do nothing
                }
        }
    }
}
