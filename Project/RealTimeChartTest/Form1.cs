using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net.Sockets;
using System.Net;

namespace LineChartTEST
{
    public partial class Form1 : Form
    {
        private delegate void CanIJust();
        private List<float> _valueList;
        private Thread _thread;
        private CanIJust _doIt;
        private Random _ran;
        private int _interval;
        private List<double> _timeList;
        private List<int> _customValueList;
        int flag = 0;
        UdpClient listener;
        IPEndPoint groupEp;
        private const int ListenPort = 5001;
        public Form1()
        {
            InitializeComponent();

            chart1.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
            chart1.Series[0].XValueType = ChartValueType.Time;
            chart1.ChartAreas[0].AxisX.ScaleView.SizeType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.Interval = 0;

            _valueList = new List<float>();
            _ran = new Random();
            _interval = 500;
            tbUpdateInterval.Text = "500";
            GoBoy();


            _timeList = new List<double>();
            _customValueList = new List<int>();
        }

        private void GoBoy()
        {

            _doIt += new CanIJust(AddData);
            DateTime now = DateTime.Now;
            chart1.ChartAreas[0].AxisX.Minimum = now.ToOADate();
            chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(10).ToOADate();
            _thread = new Thread(new ThreadStart(ComeOnYouThread));
            _thread.Start();
        }

        private void ComeOnYouThread()
        {

            listener = new UdpClient(ListenPort);
            //IPAddress address = IPAddress.Parse("192.168.15.26");
            groupEp = new IPEndPoint(IPAddress.Any, ListenPort);
            //IPEndPoint groupEp = new IPEndPoint(address, ListenPort);
            Console.WriteLine("Waiting for broadcast");

            Console.WriteLine("Received a broadcast from {0}", groupEp);
            while (true)
            {
                try
                {

                    chart1.Invoke(_doIt);


                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception : " + e.ToString());
                }
            }
            //  listener.Close();
        }

        private void AddData()
        {

            //int x = 100;
            //if (flag == 1)
            //{
            //    x = 0;
            //    flag = 0;
            //}
            //else
            //    flag = 1;
            //DateTime now = DateTime.Now;
            ////_valueList.Add();
            //chart1.ResetAutoValues();

            ////Remove old datas from the chart.
            //if (chart1.Series[0].Points.Count > 0)
            //{
            //    while (chart1.Series[0].Points[0].XValue < now.AddSeconds(-5).ToOADate())
            //    {
            //        chart1.Series[0].Points.RemoveAt(0);

            //        chart1.ChartAreas[0].AxisX.Minimum = chart1.Series[0].Points[0].XValue;
            //        chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(5).ToOADate();
            //    }
            //}

            ////Insert a data into the chart.

            //chart1.Series[0].Points.AddXY(now.ToOADate(),x);
            //chart1.Invalidate();

            var receiveByteArray = listener.Receive(ref groupEp);
            float[] floats = new float[receiveByteArray.Length / 4];

            for (int i = 0; i < receiveByteArray.Length / 4; i+=2)
            {


                DateTime now = DateTime.Now;

                //_valueList.Add();
                chart1.ResetAutoValues();

                //Remove old datas from the chart.
                if (chart1.Series[0].Points.Count > 0)
                {
                    while (chart1.Series[0].Points[0].XValue < now.AddSeconds(-5).ToOADate())
                    {
                        chart1.Series[0].Points.RemoveAt(0);

                        chart1.ChartAreas[0].AxisX.Minimum = chart1.Series[0].Points[0].XValue;
                        chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(5).ToOADate();

                    }
                }
                // DateTime now2 = DateTime.Now;
                //Insert a data into the chart.

                chart1.Series[0].Points.AddXY(now.ToOADate(), BitConverter.ToSingle(receiveByteArray, i * 4));

                //if (1 - Int32.Parse(now2.ToLongTimeString().Substring(now2.ToLongTimeString().LastIndexOf(':') + 1, 2)) - Int32.Parse(now.ToLongTimeString().Substring(now.ToLongTimeString().LastIndexOf(':') + 1, 2)) > 0.75)
                //{

                //}
                Thread.Sleep(1);
                chart1.Invalidate();

            }



        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_thread != null)
                _thread.Abort();
        }

        private void btn2D_Click(object sender, EventArgs e)
        {
            btn2D.Enabled = false;
            btn3D.Enabled = true;

            chart1.ChartAreas[0].Area3DStyle.Enable3D = false;
        }

        private void btn3D_Click(object sender, EventArgs e)
        {
            btn2D.Enabled = true;
            btn3D.Enabled = false;

            chart1.ChartAreas[0].Area3DStyle.Enable3D = true;
        }

        private void btnUpdateInterval_Click(object sender, EventArgs e)
        {
            int interval = 0;
            if (int.TryParse(tbUpdateInterval.Text, out interval))
            {
                if (interval > 0)
                    _interval = interval;
                else
                    MessageBox.Show("The data should be more than 0");
            }
            else
            {
                MessageBox.Show("Inappropriate data.");
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            _customValueList.Add(_ran.Next(0, 100));
            _timeList.Add(DateTime.Now.ToOADate());
            UpdateSecondChart();
        }

        private void UpdateSecondChart()
        {
            chart2.Series[0].Points.AddXY(_timeList[_timeList.Count - 1], _customValueList[_customValueList.Count - 1]);
            chart2.Invalidate();
        }

        private void btnSerialize_Click(object sender, EventArgs e)
        {
            string filePath = Application.StartupPath + "\\ChartData_Stream.xml";
            if (File.Exists(filePath))
            {
                File.Copy(filePath, Application.StartupPath + "\\ChartData_Stream.bak", true);
                File.Delete(filePath);
            }

            FileStream stream = new FileStream(filePath, FileMode.Create);

            chart2.Serializer.Content = SerializationContents.Default;
            chart2.Serializer.Format = System.Windows.Forms.DataVisualization.Charting.SerializationFormat.Xml;
            chart2.Serializer.Save(stream);

            stream.Close();

            //FileStream stream = new FileStream(filePath, FileMode.Create);
            //StreamWriter writer = new StreamWriter(stream);
        }

        private void btnDeserialize_Click(object sender, EventArgs e)
        {
            string filePath = Application.StartupPath + "\\ChartData_Stream.xml";
            FileStream stream = new FileStream(filePath, FileMode.Open);
            chart2.Serializer.IsResetWhenLoading = true;
            chart2.Serializer.Load(stream);

            stream.Close();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            chart2.Series[0].Points.Clear();
        }

        private void btnFilePathSe_Click(object sender, EventArgs e)
        {
            try
            {
                string filePath = Application.StartupPath + "\\ChartData_FilePath.xml";
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, Application.StartupPath + "\\ChartData_FilePath.bak", true);
                    File.Delete(filePath);
                }

                //FileStream stream = new FileStream(filePath, FileMode.Create);

                chart2.Serializer.Content = SerializationContents.Default;
                chart2.Serializer.Format = System.Windows.Forms.DataVisualization.Charting.SerializationFormat.Xml;
                chart2.Serializer.Save(filePath);

                //stream.Close();
                //FileStream stream = new FileStream(filePath, FileMode.Create);
                //StreamWriter writer = new StreamWriter(stream);
            }
            catch (Exception exc)
            {
                MessageBox.Show("An exception occurred.\nPlease try again.");
            }
        }

        private void btnFilePathDe_Click(object sender, EventArgs e)
        {
            string filePath = Application.StartupPath + "\\ChartData_FilePath.xml";
            chart2.Serializer.Reset();
            chart2.Serializer.Load(filePath);

        }
    }
}