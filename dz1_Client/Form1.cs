using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using System.Runtime.InteropServices;

namespace dz1_Client
{
    public partial class Form1 : Form
    {
        public SynchronizationContext uiContext;
        Socket socket;
        public Form1()
        {
            InitializeComponent();
            uiContext = SynchronizationContext.Current;
        }
        
        // метод для соединения с сервером
        public void ConnectWithServer()
        {
            try
            {
                // IP-адрес сервера - здесь адрес текущего компьютера (127.0.0.1)
                IPAddress address = IPAddress.Loopback;
                // либо IP-адрес, взятый их текстБокса
                //textBoxMessage.Text = address.ToString();
                // конечная точка - IP-адрес сервера + порт на сервере
                IPEndPoint endPoint = new IPEndPoint(address, 49153);
                // создаем потоковый сокет
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // соединяем сокет с конечной точкой ( с сервером)
                socket.Connect(endPoint);

                // берем имя узла компьютера клиаента (Dns.GetHostName()), конвертируем
                // его в массив байт для передачи серверу
                byte[] message = Encoding.Default.GetBytes(Dns.GetHostName());
                // отпряавляем серверу сообщение
                int byteSent = socket.Send(message);
                // вывод информации о соединение на экран
                MessageBox.Show($"Клиент {Dns.GetHostName()} установил " +
                    $"соединение с сервером {socket.RemoteEndPoint.ToString()}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // метод для отправки сообщения серверу
        public void SendMessage()
        {
            try
            {
                // строка сообщения - из текстБокса
                string message = textBoxMessage.Text;
                // коневртируем строку сообщения в массив байт
                byte[] msg = Encoding.Default.GetBytes(message);
                // тправляем серверу сообщение через сокет с помощью метода Send()
                int byteSent = socket.Send(msg);
                // вызываем метод для получения ответа от сервера
                ReceiveMas(socket);

                // если наше сообщение (клиента) содержит "<end>
                if (message.IndexOf("<end>") > -1)
                {
                    // 1024 - объем сообщения, буфер, и мы будем пытаться считать данные в этот буфер.
                    // Но сколько именно вернется данных - вернется в int bytesRec с помощью Receive
                    byte[] bytes = new byte[1024];
                    // принимаем данные, переданные сервером. 
                    int bytesRec = socket.Receive(bytes);
                    // вывод сообщения 
                    MessageBox.Show("Сервер (" + socket.RemoteEndPoint.ToString() + ") ответил: " + 
                        Encoding.Default.GetString(bytes, 0, bytesRec));
                    // блокируем передачу и получение данных для объекта Socket.
                    socket.Shutdown(SocketShutdown.Both);
                    // закрываем сокет
                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Клиент: " + ex.Message);
            }
        }
        
        // метод для получение сообщений от сервера
        public void ReceiveMas(object param)
        {
            Socket handler = (Socket)param;
            try
            {
                // создаем буфер
                byte[] bytes = new byte[1024];
                //int bytesRec = handler.Receive(bytes);

                while (true)
                {
                    // получаем сообщение т сервера
                    int bytesRec = handler.Receive(bytes);
                    if (bytesRec == 0)  // если данных нет
                    {
                        // блокируем передачу данных
                        handler.Shutdown(SocketShutdown.Both);
                        // закрываем сокет
                        handler.Close();
                        return;
                    }
                    // полученное сообщение записываем в строку
                    string data = Encoding.Default.GetString(bytes, 0, bytesRec);
                    
                    // выводим сообщение в элемент ЛистБокс с пом контекста синхронизации
                    uiContext.Send(d => listBox1.Items.Add(data), null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Клиент: " + ex.Message);
                handler.Shutdown(SocketShutdown.Both); // Блокируем передачу и получение данных для объекта Socket.
                handler.Close(); // закрываем сокет
            }
        }

        // обработка кнопки Соединение с сервером
        private void buttonConnectServer_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(new ThreadStart(ConnectWithServer));
            th.IsBackground = true;
            th.Start();
        }

        // обработка кнопки Отправить сообщение (серверу)
        private void buttonSendMessage_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(new ThreadStart(SendMessage));
            th.IsBackground = true;
            th.Start();
        }

        // обработка события Закрытие формы
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Клиент: " + ex.Message);
            }
        }
    }
}
