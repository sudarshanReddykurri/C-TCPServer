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


namespace TCPServer
{
    public partial class Form1 : Form
    {

        TcpListener mTCPListener;

        TcpClient mTCPClient;

        byte[] mRx;

        //Newly Added 1 line

        private List<ClientNode> mlClientSocks;

        public Form1()
        {
            InitializeComponent();
            //Newly Added 2lines 
            mlClientSocks = new List<ClientNode>(2);
            CheckForIllegalCrossThreadCalls = false;
        }

        IPAddress findMyIPV4Address()
        {
            string strThisHostName = string.Empty;
            IPHostEntry thisHostDNSEntry = null;
            IPAddress[] allIPsOfThisHost = null;
            IPAddress ipv4Ret = null;
            try
            {
                strThisHostName = System.Net.Dns.GetHostName();

                printLine(strThisHostName);

                thisHostDNSEntry = System.Net.Dns.GetHostEntry(strThisHostName);

                allIPsOfThisHost = thisHostDNSEntry.AddressList;

                for(int idx = allIPsOfThisHost.Length-1; idx >0; idx--)
                {
                    if (allIPsOfThisHost[idx].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4Ret = allIPsOfThisHost[idx];
                    }
                }


            }            
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return ipv4Ret;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnStartListening_Click(object sender, EventArgs e)
        {


            IPAddress ipaddr;
            int nPort;

            if (!int.TryParse(tbPort.Text, out nPort))
            {
                nPort = 23000;
            }

            if(!IPAddress.TryParse(tbIPAddress.Text, out ipaddr))
            {
                MessageBox.Show("Invalid IP address supplied");
                return;
            }

            mTCPListener = new TcpListener(ipaddr,nPort);

            mTCPListener.Start();

            mTCPListener.BeginAcceptTcpClient(onCompleteAcceptTcpClient, mTCPListener);




        }

        void onCompleteAcceptTcpClient(IAsyncResult iar)
        {

            TcpListener tcpl = (TcpListener)iar.AsyncState;

            //Newly Added 2 lines 
            TcpClient tclient = null;
            ClientNode cNode = null;

            try
            {
                // //Newly Added 1 line
                tclient = tcpl.EndAcceptTcpClient(iar);

                
                // mTCPClient = tcpl.EndAcceptTcpClient(iar);
                printLine("Client Connected....");

                tcpl.BeginAcceptTcpClient(onCompleteAcceptTcpClient, tcpl);
                // mRx = new byte[512];
                //mTCPClient.GetStream().BeginRead(mRx, 0, mRx.Length, onCompletedReadFromTCPClientStream, mTCPClient);


                //Newly Added 4 lines excluding flower brackets
                lock (mlClientSocks)
                {
                    mlClientSocks.Add((cNode = new ClientNode(tclient, new byte[512], new byte[512], tclient.Client.RemoteEndPoint.ToString())));
                    lbClients.Items.Add(cNode.ToString());
                    
                }

                tclient.GetStream().BeginRead(cNode.Rx, 0, cNode.Rx.Length, onCompleteReadFromTCPClientStream, tclient);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
         }

        void onCompleteReadFromTCPClientStream(IAsyncResult iar)
        {

            //TcpClient tcpc;
            //int nCountReadBytes = 0;
            //string strRecv;

            //try
            //{
            //    tcpc = (TcpClient)iar.AsyncState;
            //    nCountReadBytes = tcpc.GetStream().EndRead(iar);

            //    if (nCountReadBytes == 0)
            //    {
            //        MessageBox.Show("Client disconnected.");
            //        return;
            //    }

            //    strRecv = Encoding.ASCII.GetString(mRx, 0, nCountReadBytes);

            //    printLine(strRecv);

            //    mRx = new byte[512];

            //    tcpc.GetStream().BeginRead(mRx, 0, mRx.Length, onCompleteReadFromTCPClientStream, tcpc);

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}




            // Newly Added Below Lines of this method

            TcpClient tcpc;
            int nCountReadBytes = 0;
            string strRecv;
            
            ClientNode cn = null;

            try
            {


                lock (mlClientSocks)
                {
                tcpc = (TcpClient)iar.AsyncState;
                cn = mlClientSocks.Find(x => x.strId == tcpc.Client.RemoteEndPoint.ToString());

                    nCountReadBytes = tcpc.GetStream().EndRead(iar);

                if(nCountReadBytes==0) // this happens when the client is disconnected
                 {
                    MessageBox.Show("Client Disconnected");
                        mlClientSocks.Remove(cn);
                        lbClients.Items.Remove(cn.ToString());
                        return;
                 }

                    // strRecv = Encoding.ASCII.GetString(mRx, 0, nCountReadBytes);


                    //     printLine(strRecv);

                    //  mRx = new byte[512];

                    // tcpc.GetStream().BeginRead(mRx, 0, mRx.Length, onCompleteReadFromTCPClientStream, tcpc);


                    strRecv = Encoding.ASCII.GetString(cn.Rx, 0, nCountReadBytes).Trim();
                    //strRecv = Encoding.ASCII.GetString(mRx, 0, nCountReadBytes);

                    printLine(DateTime.Now + " - " + cn.ToString() + ": " + strRecv);

                    cn.Rx = new byte[512];

                    tcpc.GetStream().BeginRead(cn.Rx, 0, cn.Rx.Length, onCompleteReadFromTCPClientStream, tcpc);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lock (mlClientSocks)
                {
                    printLine("Client disconnected: " + cn.ToString());
                    mlClientSocks.Remove(cn);
                    lbClients.Items.Remove(cn.ToString());
                }
            }

        }

        public void printLine(string _strPrint)
        {
            tbConsoleOutput.Invoke(new Action<string>(doInvoke), _strPrint);
        }
        public void doInvoke(string _strPrint)
        {
            tbConsoleOutput.Text = _strPrint + Environment.NewLine + tbConsoleOutput.Text;
        }

        private void tbConsoleOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //byte[] tx = new byte[512];

            //if (string.IsNullOrEmpty(tbPayload.Text))
            //    return;
            //try
            //{
            //    if(mTCPClient != null)
            //    {
            //        if (mTCPClient.Client.Connected)
            //        {
            //            tx = Encoding.ASCII.GetBytes(tbPayload.Text);
            //            mTCPClient.GetStream().BeginWrite(tx,0,tx.Length, onCompleteWriteToClientStream, mTCPClient);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            // //Newly Added all the below lines of this method

            if (lbClients.Items.Count <= 0) return;
            if (string.IsNullOrEmpty(tbPayload.Text)) return;

            ClientNode cn = null;

            lock (mlClientSocks)
            {
                cn = mlClientSocks.Find(x => x.strId == lbClients.SelectedItem.ToString());
                cn.Tx = new byte[512];

                try
                {
                    if (cn != null)
                    {
                        if (cn.tclient != null)
                        {
                            if (cn.tclient.Client.Connected)
                            {
                                cn.Tx = Encoding.ASCII.GetBytes(tbPayload.Text);
                                cn.tclient.GetStream().BeginWrite(cn.Tx, 0, cn.Tx.Length, onCompleteWriteToClientStream, cn.tclient);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void onCompleteWriteToClientStream(IAsyncResult iar)
        {
            try
            {
                TcpClient tcpc = (TcpClient)iar.AsyncState;
                tcpc.GetStream().EndWrite(iar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFindIP_Click(object sender, EventArgs e)
        {
            IPAddress ipa = null;
            ipa = findMyIPV4Address();
            if (ipa != null)
            {
                tbIPAddress.Text = ipa.ToString();
            }

        }
    }
}
