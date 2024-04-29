using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GroupChatApp
{
    public partial class Form1 : Form
    {
        
        private TextBox txtNickname = new TextBox();
        private Button btnJoin = new Button();
        private Button btnLeave = new Button();
        private ListBox chatBox = new ListBox();
        private TextBox txtMessage = new TextBox();
        private Button btnSend = new Button();
        

        private UdpClient udpClient;
        private string userName = "";
        private int serverPort = 11000;
        private IPEndPoint serverEP;

        public Form1()
        {
            
            InitializeComponent();
            this.ClientSize = new Size(350, 500);
            this.BackColor = Color.FromArgb(39,58,82);
            //UDP client
            udpClient = new UdpClient();
            serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
            
        }
        
        private void InitializeComponent()
        {
            
            this.txtNickname = new TextBox();
            this.btnJoin = new Button();
            this.btnLeave = new Button();
            this.chatBox = new ListBox();
            this.txtMessage = new TextBox();
            this.btnSend = new Button();

            this.SuspendLayout();

            // TextBox for entering nickname
            this.txtNickname = new TextBox();
            this.txtNickname.Location = new System.Drawing.Point(10, 10);
            this.txtNickname.Size = new System.Drawing.Size(140, 20);
            txtNickname.BackColor = Color.FromArgb(230,230,230);

            // Button to join chat
            this.btnJoin = new Button();
            this.btnJoin.Location = new System.Drawing.Point(170, 10);
            this.btnJoin.Size = new System.Drawing.Size(75, 23);
            this.btnJoin.Text = "Join";
            this.btnJoin.Click += new EventHandler(btnJoin_Click);
            btnJoin.BackColor = Color.FromArgb(18,107,125);
            btnJoin.Font = new Font("Arial", 10, FontStyle.Bold); // You can adjust the font family, size, and style (bold, italic, etc.) as needed
            btnJoin.ForeColor = Color.White;  // Change the text color of a button

            // Button to leave chat
            this.btnLeave = new Button();
            this.btnLeave.Location = new System.Drawing.Point(250, 10);
            this.btnLeave.Size = new System.Drawing.Size(75, 23);
            this.btnLeave.Text = "Leave";
            this.btnLeave.Click += new EventHandler(btnLeave_Click);
            btnLeave.BackColor = Color.FromArgb(179,70,70);
            btnLeave.Font = new Font("Arial", 8, FontStyle.Bold); // You can adjust the font family, size, and style (bold, italic, etc.) as needed
            btnLeave.ForeColor = Color.White;  // Change the text color of a button

            // ListBox to display chat messages
            this.chatBox = new ListBox();
            this.chatBox.FormattingEnabled = true;
            this.chatBox.IntegralHeight = false; // Enable auto-resize
            this.chatBox.Location = new System.Drawing.Point(10, 40);
            this.chatBox.Size = new System.Drawing.Size(315, 200);
            this.chatBox.TextChanged += new EventHandler(chatBox_TextChanged);
            chatBox.BackColor = Color.FromArgb(230,230,230);
            chatBox.Font = new Font("Arial", 11); // You can adjust the font family, size, and style (bold, italic, etc.) as needed
            chatBox.ForeColor = Color.FromArgb(70,76,64);  // Change the text color of a button

            // TextBox for entering messages
            this.txtMessage = new TextBox();
            this.txtMessage.Location = new System.Drawing.Point(10, 260);
            this.txtMessage.Size = new System.Drawing.Size(255, 40);
            this.txtMessage.KeyDown += new KeyEventHandler(txtMessage_KeyDown);
            txtMessage.BackColor = Color.FromArgb(230,230,230);

            // Button to send messages
            this.btnSend = new Button();
            this.btnSend.Location = new System.Drawing.Point(260, 248);
            this.btnSend.Size = new System.Drawing.Size(75, 32);
            this.btnSend.Text = "Send";
            this.btnSend.Click += new EventHandler(btnSend_Click);
            btnSend.BackColor = Color.FromArgb(18,107,125);
            btnSend.Font = new Font("Arial", 10, FontStyle.Bold); // You can adjust the font family, size, and style (bold, italic, etc.) as needed
            btnSend.ForeColor = Color.White;  // Change the text color of a button

            // Add controls to the form
            this.Controls.Add(this.txtNickname);
            this.Controls.Add(this.btnJoin);
            this.Controls.Add(this.btnLeave);
            this.Controls.Add(this.chatBox);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnSend);

            this.ResumeLayout(false);
            this.Resize += new EventHandler(Form1_Resize);
        }

        private void chatBox_TextChanged(object sender, EventArgs e)
        {
            // Automatically scroll to the bottom of the chat box
            chatBox.TopIndex = chatBox.Items.Count - 1;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            // Calculate the new sizes and positions of the controls
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            // Adjust the text boxes' sizes and positions based on the form's new size
            int textBoxWidth = formWidth - 40; // Adjust as needed
            int textBoxHeight = 20; // Adjust as needed

            // Set new size and position for txtNickname
            //txtNickname.Size = new Size(textBoxWidth - 170, textBoxHeight);
            txtNickname.Location = new Point(10, 10);

            // Set new size and position for txtMessage
            txtMessage.Size = new Size(textBoxWidth - btnSend.Width - 15, textBoxHeight);
            txtMessage.Location = new Point(10, formHeight - txtMessage.Height - 10);

            // Set new size and position for btnSend
            btnSend.Location = new Point(formWidth - btnSend.Width - 30, formHeight - btnSend.Height - 5);

            // Adjust chatBox size to fill the remaining space
            chatBox.Size = new Size(textBoxWidth, formHeight - txtNickname.Height - txtMessage.Height - 40);
            chatBox.Location = new Point(10, txtNickname.Bottom + 10);

            // Adjust btnJoin and btnLeave positions
            btnJoin.Location = new Point(txtNickname.Right + 10, 10);
            btnLeave.Location = new Point(btnJoin.Right + 10, 10);
        }
        
        private void btnJoin_Click(object sender, EventArgs e)
        {
        // Handle join button click event
        userName = txtNickname.Text;
        // Debugging: Output the username to verify it's read correctly
        this.chatBox.Items.Add("Joining with username: " + userName);
        SendJoinRequest();
        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();
        }

        private void btnLeave_Click(object sender, EventArgs e)
        {
            // Handle leave button click event
            SendLeaveNotification();
            udpClient.Close();
            // Leave the chat and disconnect from the server
            this.Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // Handle send button click event
            string message = txtMessage.Text;
            SendMessage(message);
            txtMessage.Clear();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle sending message when Enter key is pressed
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevents Enter from adding newline to textbox
                btnSend_Click(sender, e);
            }
        }
        
        private void SendJoinRequest()
        {
            string message = "JOIN " + userName;
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            udpClient.Send(buffer, buffer.Length, serverEP);
        }

        private void SendLeaveNotification()
        {
            string message = "LEAVE";
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            udpClient.Send(buffer, buffer.Length, serverEP);
        }

        private void SendMessage(string message)
        {
            string formattedMessage = "MSG " + message;
            byte[] buffer = Encoding.ASCII.GetBytes(formattedMessage);
            udpClient.Send(buffer, buffer.Length, serverEP);
        }

        private void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpClient.Receive(ref from);
                    string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                    if (from.Equals(serverEP))
                    {
                        // Display received message in chatBox
                        if (receivedMessage.StartsWith(userName))
                        {
                            // Display user's own messages on the right side
                            this.Invoke((MethodInvoker)delegate
                            {
                                chatBox.Items.Add(receivedMessage);
                            });
                        }
                        else
                        {
                            // Display messages from others on the left side
                            this.Invoke((MethodInvoker)delegate
                            {
                                chatBox.Items.Add(receivedMessage);
                            });
                        }

                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnected from server: " + ex.Message);
            }
        }
    }
}