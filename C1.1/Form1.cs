using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Timers; // This imports the Timer class from System.Timers

namespace GroupChatApp
{
    public partial class Form1 : Form
    {
        private int totalLineCount = 0;
        private TextBox txtNickname = new TextBox();
        private Button btnJoin = new Button();
        private Button btnLeave = new Button();
        private ListView chatBox = new ListView();
        private TextBox txtMessage = new TextBox();
        private Button btnSend = new Button();
        private Panel pnlChat = new Panel();
        private int minimumWidth = 370; // Minimum width you want to allow
        private int minimumHeight = 300; // Minimum height you want to allow

        //Connection related
        private UdpClient udpClient;
        private string userName = "";
        private int serverPort = 11000;
        private IPEndPoint serverEP;
        private int messageId = 0;
        private Dictionary<int, string> pendingMessages;
        private System.Timers.Timer retryTimer;
        private Random random;
        
        public Form1()
        {
            
            InitializeComponent();
            this.ClientSize = new Size(430, 500);
            this.BackColor = Color.FromArgb(39,58,82);
            this.MinimumSize = new Size(minimumWidth, minimumHeight);
            //UDP client
            this.userName = userName;
            udpClient = new UdpClient(0);
            serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
            pendingMessages = new Dictionary<int, string>();
            retryTimer = new System.Timers.Timer(10000); // Using System.Timers.Timer
            retryTimer.Elapsed += HandleRetry;
            retryTimer.AutoReset = true; // Ensure the timer fires repeatedly
            random = new Random();
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
            retryTimer.Start();
            
        }
    
        private void InitializeComponent()
        {

            this.txtNickname = new TextBox();
            this.btnJoin = new Button();
            this.btnLeave = new Button();
            this.chatBox = new ListView();
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

            // Panel for chat messages
            this.pnlChat = new Panel();
            this.pnlChat.Location = new Point(10, 40);
            this.pnlChat.Size = new Size(310, 415);
            this.pnlChat.BackColor = Color.FromArgb(230, 230, 230);
            this.pnlChat.AutoScroll = true;

            // ListView to display chat messages 
            this.chatBox = new ListView();
            this.chatBox.View = View.Details;
            this.chatBox.FullRowSelect = true;
            this.chatBox.HeaderStyle = ColumnHeaderStyle.None;
            this.chatBox.Columns.Add(new ColumnHeader() { Width = this.pnlChat.Width - 4 });
            this.chatBox.Dock = DockStyle.Top;
            this.chatBox.GridLines = true;
            this.chatBox.MultiSelect = false;

            chatBox.View = View.Details;
            chatBox.FullRowSelect = true;
            chatBox.HeaderStyle = ColumnHeaderStyle.None;
            //chatBox.Columns.Add(new ColumnHeader() { Width = 300 }); // Set initial width
            //chatBox.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.None); // Disable auto-resize
            chatBox.Dock = DockStyle.Fill;
            chatBox.GridLines = true;
            chatBox.MultiSelect = false;
            chatBox.OwnerDraw = true;
            this.chatBox.DrawItem += chatBox_DrawItem;
            //chatBox.DrawItem += new DrawListViewItemEventHandler(chatBox_DrawItem);

            //this.chatBox.SelectedIndexChanged += new EventHandler(chatBox_SelectedIndexChanged);
            chatBox.GridLines = false;
            chatBox.BackColor = Color.FromArgb(230,230,230);
            chatBox.Font = new Font("Arial", 11); // You can adjust the font family, size, and style (bold, italic, etc.) as needed
            chatBox.ForeColor = Color.FromArgb(70,76,64);  // Change the text color of a button
           
            // Add ListView to Panel
            this.pnlChat.Controls.Add(this.chatBox);

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
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.pnlChat);

            this.ResumeLayout(false);
            this.Resize += new EventHandler(Form1_Resize);
        }

        private int totalHeight = 0; // Variable to keep track of total height of messages

        private void chatBox_DrawItem(object sender, DrawListViewItemEventArgs e)
        {

            e.DrawDefault = false; // We'll handle the drawing ourselves

            // Define the text to draw and its font
            string text = e.Item.Text;
            Font font = e.Item.Font;

            // Define the drawing rectangle
            Rectangle bounds = e.Bounds;

            // Define the string format for text drawing
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Center;
            stringFormat.Trimming = StringTrimming.EllipsisWord; // Truncate with ellipsis if needed

            // Calculate the size of the text
            SizeF textSize = e.Graphics.MeasureString(text, font, bounds.Width, stringFormat);

            // Adjust the bounds height to fit the entire text
            bounds.Height = (int)textSize.Height;

            // Calculate the position to draw the text
            int yPos;
            if (e.ItemIndex > 0)
            {
                // Calculate yPos relative to the previous item's position
                yPos = chatBox.GetItemRect(e.ItemIndex - 1).Bottom + 2;
            }
            else
            {
                // If it's the first item, draw it at the top
                yPos = bounds.Top;
            }
            
            if (text.StartsWith(userName) && text.IndexOf(userName + ":") == 0)
            {
                e.Item.BackColor = Color.LightBlue; // Optionally, change background color for own messages
                e.Item.ForeColor = Color.Black; // Optionally, change text color for own messages

                // Calculate the position to draw the text (align to the right)
                int xPos = chatBox.Width - (int)textSize.Width;;
                int adjustedYPos = yPos + (e.ItemIndex > 0 ? 2 : 0); // Add some spacing if not the first item

                // Draw the text
                e.Graphics.DrawString(text, font, Brushes.Black, new Rectangle(xPos, adjustedYPos, bounds.Width, bounds.Height), stringFormat);
            }
            else
            {
                int adjustedYPos = yPos + (e.ItemIndex > 0 ? 2 : 0); // Add some spacing if not the first item

                // Draw the text
                e.Graphics.DrawString(text, font, Brushes.Black, bounds, stringFormat);
            }
        }

        // Method to wrap text into multiple lines
        private string WrapText(string text, Font font, int maxWidth)
        {
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = TextRenderer.MeasureText(" ", font).Width;

            foreach (string word in words)
            {
                Size wordSize = TextRenderer.MeasureText(word, font);
                if (lineWidth + wordSize.Width < maxWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += wordSize.Width + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = wordSize.Width + spaceWidth;
                }
            }

            string wrappedText = sb.ToString().Trim(); // Trim to remove any trailing whitespace

            // Split the wrapped text into lines
            string[] lines = wrappedText.Split('\n');

            // Join the lines with line breaks
            return string.Join(Environment.NewLine, lines);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            totalHeight = 0;
                // Ensure the form size doesn't fall below the minimum size
            if (this.Width < minimumWidth)
            {
                this.Width = minimumWidth;
            }
            if (this.Height < minimumHeight)
            {
                this.Height = minimumHeight;
            }
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
            chatBox.Columns[0].Width = chatBox.Width - SystemInformation.VerticalScrollBarWidth;
            //chatBox.Columns[0].Width = (textBoxWidth - formWidth);
            //chatBox.Columns.Add(new ColumnHeader() { Width = textBoxWidth - formWidth });
            
            // Adjust pnlChat size to fill match chatbox space
            pnlChat.Size = new Size(textBoxWidth, formHeight - txtNickname.Height - txtMessage.Height - 40);
            pnlChat.Location = new Point(10, txtNickname.Bottom + 10);

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
            retryTimer.Stop();
            this.Close(); // Comment if app exit not needed
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
            SendMessageInternal(message);
        }

        private void SendLeaveNotification()
        {
            string message = "LEAVE";
            SendMessageInternal(message);
        }

        private void SendMessage(string message)
        {
            string formattedMessage = "MSG " + message;
            SendMessageInternal(formattedMessage);
        }

        private void SendMessageInternal(string messageContent)
        {
            string message = $"{messageId}:{messageContent}";
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            // Simulate packet drop
            if (random.Next(2) == 1) // 50% chance to send or drop
            {
                udpClient.Send(buffer, buffer.Length, serverEP);
                Console.WriteLine($"Sent message: {message}");
            }
            else
            {
                Console.WriteLine($"Dropped message: {message}");
            }
            if (!pendingMessages.ContainsKey(messageId))
            {
                pendingMessages[messageId] = message;
            }
            messageId++;
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

                    if (receivedMessage.StartsWith("ACK:"))
                    {
                        HandleAck(receivedMessage);
                    }
                    else if (from.Equals(serverEP))
                    {
                        // Display received message in chatBox
                        this.Invoke((MethodInvoker)delegate
                        {
                            ListViewItem item = new ListViewItem(receivedMessage);
                            chatBox.Items.Add(item);
                            chatBox.EnsureVisible(item.Index);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnected from server: " + ex.Message);
            }
        }


        private void HandleAck(string ackMessage)
        {
            int ackId = int.Parse(ackMessage.Split(':')[1]);
            if (pendingMessages.ContainsKey(ackId))
            {
                pendingMessages.Remove(ackId);
            }
        }

        private void HandleRetry(object sender, ElapsedEventArgs e)
        {
            foreach (var message in pendingMessages.Values)
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                udpClient.Send(buffer, buffer.Length, serverEP);
                Console.WriteLine("Resending message: " + message);
            }
        }

    }
}