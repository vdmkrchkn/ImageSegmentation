using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace GrowCut
{
    public partial class Form1 : Form
    {
        Pen pen;
        Image srcImage;
        Dictionary<LABEL,List<Point>> segmentSeeds;
        Point pStart, pCur, pNull;
        bool doSegmentation;
        Thread calcThread;
     
        public Form1()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            this.segmentSeeds = new Dictionary<LABEL,List<Point>>();
            this.pNull = new Point(int.MaxValue, 0);
            this.pen = radioButton1.Checked ? Pens.Red : Pens.Blue;
            this.radioButton4.Checked = true;            
        }        

        private void open1_Click(object sender, EventArgs e)
        {
            pStart = pNull;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string s = openFileDialog1.FileName;
                try
                {
                    srcImage = new Bitmap(s);
                    if (pictureBox1.Image != null)
                        pictureBox1.Image.Dispose();
                    pictureBox1.Image = srcImage.Clone() as Image;
                }
                catch
                {
                    MessageBox.Show("File " + s + " has a wrong format", "Error");
                    return;
                }
                Text = "Image segmentation - " + Path.GetFileNameWithoutExtension(s);
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(s);
                openFileDialog1.FileName = "";
                segmentSeeds.Clear();
            }
        }

        private void exit1_Click(object sender, EventArgs e)
        {
            Close();
        }        

        private void new1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "";
            srcImage = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
            Graphics g = Graphics.FromImage(srcImage);
            g.Clear(Color.White);
            g.Dispose();
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            pictureBox1.Image = srcImage.Clone() as Image;
            segmentSeeds.Clear();
        }

        //Функция запускаемая из другого потока
        void threadFuncIterative(object o)
        {
            int t = 0;            
            while (doSegmentation && (o as Automaton).evolution())
            {
                Invoke(new RefreshImageDelegate(RefreshImage), o,true);
                Console.WriteLine("iter: " + ++t);                
                //Thread.Sleep(0);
            }
            doSegmentation = false;
        }

        //Функция запускаемая из другого потока
        void threadFuncAuto(object o)
        {            
            (o as ISODATA).run();
            Invoke(new RefreshImageDelegate(RefreshImage), o, false);                
            doSegmentation = false;
        }

        delegate void RefreshImageDelegate(object a, bool type);
        // Функция для делегата
        void RefreshImage(object a, bool type = true)
        {                        
            Bitmap bmp = pictureBox1.Image as Bitmap;
            if (type)
                (a as Automaton).convertBitmap(ref bmp);
            else
                (a as ISODATA).convertBitmap(ref bmp);
            pictureBox1.Image = bmp;      
        }             

        private void segment1_Click(object sender, EventArgs e)
        {
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(srcImage);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Для сегментации инициализируйте изображение.");
                return;
            }
            this.doSegmentation = true;
            if (radioButton3.Checked)
            {
                Automaton a = new Automaton(bmp);
                // интерактивная разметка
                a.userAction(segmentSeeds);
                // Создание отдельного потока для эволюции
                calcThread = new Thread(threadFuncIterative);                
                calcThread.Start(a); //запуск эволюции                
            }
            else
            {
                // получение признакового пространства
                List<SamplePoint> samples = new List<SamplePoint>();
                int minARGB = int.MaxValue;
                int maxARGB = int.MinValue;
                int minIdx = -1, maxIdx = -1;
                for (int x = 0; x < bmp.Width; ++x)
                    for (int y = 0; y < bmp.Height; ++y)
                    {
                        Color clr = bmp.GetPixel(x, y);
                        samples.Add(new SamplePoint(x * bmp.Height + y, clr));
                        if (clr.ToArgb() < minARGB)
                        {
                            minARGB = clr.ToArgb();
                            minIdx = x * bmp.Height + y;
                        }
                        if (clr.ToArgb() > maxARGB)
                        {
                            maxARGB = clr.ToArgb();
                            maxIdx = x * bmp.Height + y;
                        }                        
                    }
                // параметры алгоритма ISODATA
                int clusters = 2; // кол-во кластеров
                int nTheta = 1;
                int sTheta = 1;
                int cTheta = 4;
                int iters = (int)numericUpDown1.Value; // кол-во итераций                
                if (samples.Count < clusters)
                    throw new Exception("Размерность признакового пространства должна превосходить число кластеров");
                // инициализация кластеров
                Cluster[] groups = new Cluster[clusters];
                groups[0] = new Cluster(0, samples[minIdx]);
                groups[1] = new Cluster(1, samples[maxIdx]);
                foreach(Cluster c in groups)                    
                    Console.WriteLine(c);                
                //
                ISODATA iso = new ISODATA(groups, samples, clusters, nTheta, sTheta, cTheta, 1, iters);
                // Создание отдельного потока процедуры
                calcThread = new Thread(threadFuncAuto);
                calcThread.Start(iso); //запуск                                
            }
        }

        private void reset1_Click(object sender, EventArgs e)
        {           
            try
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();                
                pictureBox1.Image = srcImage.Clone() as Image;
                segmentSeeds.Clear();
                doSegmentation = false;
                if (calcThread != null && calcThread.IsAlive)
                    //while (GrowCutThread != null && GrowCutThread.IsAlive)
                    calcThread.Abort();
            }
            catch
            {
                return;
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {            
            if (e.Button == MouseButtons.Left)
            {
                pCur = pStart = e.Location;
                var l = new List<Point>();
                l.Add(pCur);
                LABEL curLabel = radioButton1.Checked ? LABEL.OBJECT : LABEL.BACKGROUND;
                if (!segmentSeeds.ContainsKey(curLabel))
                    segmentSeeds.Add(curLabel, l);
                else
                    segmentSeeds[curLabel].Add(pCur);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pStart == pNull)
                return;
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    Graphics g = Graphics.FromImage(pictureBox1.Image);
                    g.DrawLine(pen, pCur, e.Location);
                    g.Dispose();
                    pictureBox1.Invalidate();
                    pCur = e.Location;
                    if (pictureBox1.Image.Size.Width > pCur.X && pCur.X >= 0 && pictureBox1.Image.Size.Height > pCur.Y && pCur.Y >= 0)                        
                        segmentSeeds[radioButton1.Checked ? LABEL.OBJECT : LABEL.BACKGROUND].Add(pCur);
                }
                catch
                {
                    return;
                }                         
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            this.pen = sender == radioButton1 ? Pens.Red : Pens.Blue;            
        }        

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            this.groupBox1.Visible = !this.groupBox1.Visible;            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.doSegmentation = false;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            this.groupBox3.Visible = !this.groupBox3.Visible;
        }       
    }
}
