using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace RealTimeChartTest
{
    public partial class Test : Form
    {

        private Thread chartThread;
        UdpClient listener;
        IPEndPoint groupEp;
        private const int ListenPort = 5001;
        float[] fft;
        private int count;
        private int xcount;

        double[] f;

        private readonly double sampleRate = 1e6;
        private readonly double baseFreq = 1e5;
        private readonly int fftSize = 1024;
        private double scaleFactor;

        private double mReal;
        private double mImg;
        private int tcount = 0;

        private double[] psdVal;

        public Test()
        {
            mReal = 0;
            mImg = 0;
            xcount = 0;

            psdVal = new double[fftSize];
            f = new double[fftSize];

            var x = Math.Max(Math.Abs(sampleRate), Math.Abs(baseFreq));
            if (x > 1e9)
                scaleFactor = 1e-9;
            else if (x > 1e6)
                scaleFactor = 1e-6;
            else
                scaleFactor = 1e-3;

            fft = new float[fftSize * 2];

            InitializeComponent();

            chartThread = new Thread(renderChart) { IsBackground = true };

            chartThread.Start();

            Chart1.ChartAreas[0].AxisY.Interval = 0;
            Chart1.ChartAreas[0].AxisY.Minimum = -80;
            Chart1.ChartAreas[0].AxisY.Maximum = 40;
            Chart1.ChartAreas[0].AxisX.Interval = 50;
            Chart1.ChartAreas[0].AxisX.Minimum = 100;
            Chart1.ChartAreas[0].AxisX.Maximum = 600;

        }


        private void renderChart()
        {

            listener = new UdpClient(ListenPort);
            groupEp = new IPEndPoint(IPAddress.Any, ListenPort);

            while (true)
            {

                if (Chart1.IsHandleCreated)
                {
                    {
                        this.Invoke((MethodInvoker)UpdateChart);
                        Thread.Sleep(10);
                    }
                }
            }
        }

        private void UpdateChart()
        {
            count = 0;
            Chart1.Series["Series1"].Points.Clear();

            var receiveByteArray = listener.Receive(ref groupEp);

            if (receiveByteArray != null)
            {
                for (int i = 0; i < count + receiveByteArray.Length / 4; i++)
                {
                    fft[i] = BitConverter.ToSingle(receiveByteArray, i * 4);
                }
            }

            //Calculate mag squared

            float[] cplxVal1 = new float[4];
            float[] cplxVal2 = new float[4];

            List<float> lstMagSqrd = new List<float>();

            for (int i = 0; i < fftSize; i += 8)
            {
                for (int j = 0; j < 4; j++)
                {
                    cplxVal1[j] = (float)Math.Pow(fft[i + j], 2);
                    cplxVal2[j] = (float)Math.Pow(fft[i + j + 4], 2);
                }

                //Reverse each 8 float for sse3
                lstMagSqrd.AddRange(new float[]
                {
                    cplxVal2[0] + cplxVal2[1],
                    cplxVal2[2] + cplxVal2[3],
                    cplxVal1[0] + cplxVal1[1],
                    cplxVal1[2] + cplxVal1[3]
                });

                ////Reverse each 8 float for sse
                //lstMagSqrd.AddRange(new float[]
                //{
                //    cplxVal1[3] + cplxVal1[2],
                //    cplxVal1[0] + cplxVal1[1],
                //    cplxVal2[3] + cplxVal2[2],
                //    cplxVal2[0] + cplxVal2[1]
                //});
            }

            List<OnePole> lstFilter = new List<OnePole>();
            for (int i = 0; i < fftSize / 2; i++)
            {
                lstFilter.Add(new OnePole((float)1.0));
            }

            //Take first N/2+1 elements of magArr
            List<float> lstFilterMag = new List<float>();
            for (int i = 0; i < fftSize / 2; i++)
            {
                lstFilterMag.Add(lstFilter[i].Process(lstMagSqrd[i]));
            }

            //Frequecy
            for (int i = 0; i < fftSize / 2; i++)
            {
                f[i] = i * sampleRate * scaleFactor / (fftSize / 2 + 1) + baseFreq * scaleFactor;
            }

            //Blackman Harris
            double[] myWindow = blackmanharris(fftSize);
            var power = 0.0;
            foreach (var tap in myWindow)
            {
                power += tap * tap;
            }

            List<float> psdVal = new List<float>();

            psdVal.Add((float)(10 * Math.Log10(Math.Max(lstFilterMag[0], 1e-18))
                        - 20 * Math.Log10(fftSize)
                        - 10 * Math.Log10(power / fftSize)
                        - 20 * Math.Log10(2 / 2)));

            for (int i = 1; i < fftSize / 2; i++)
            {
                psdVal.Add((float)(10 * Math.Log10(2 * Math.Max(lstFilterMag[i], 1e-18))
                            - 20 * Math.Log10(fftSize)
                            - 10 * Math.Log10(power / fftSize)
                            - 20 * Math.Log10(2 / 2) + 3));
            }


            int t = 0;


            for (int i = 0; i < fftSize / 2; i++)
            {
                Chart1.Series["Series1"].Points.AddXY(f[i], psdVal[i]);
            }

        }

        private double[] blackmanharris(int N)
        {
            double a0 = 0.35875,
                a1 = 0.48829,
                a2 = 0.14128,
                a3 = 0.01168;

            var d = Math.PI / (N - 1);

            double[] res = new double[N];

            for (int i = 0; i < N; i++)
            {
                res[i] = a0 - a1 * Math.Cos(2 * d) + a2 * Math.Cos(4 * d) + a3 * Math.Cos(6 * d);
            }

            return res;
        }

        #region button
        private void btnStart_Click(object sender, EventArgs e)
        {
            chartThread.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            chartThread.Join();
        }
        #endregion
    }
}
