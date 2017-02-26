using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace Segmentation
{
    public partial class Form1 : Form
    {
        Pen pen;
        Image srcImage;
        Dictionary<LABEL,List<Point>> segmentSeeds;
        Point pStart, pCur, pNull;
        public bool DoSegmentation { get; private set; }
        Thread calcThread;
     
        public Form1()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            segmentSeeds = new Dictionary<LABEL,List<Point>>();
            pNull = new Point(int.MaxValue, 0);
            pen = radioButton1.Checked ? Pens.Red : Pens.Blue;
            autoRadioButton.Checked = true;                        
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
                    MessageBox.Show($"File {s} has a wrong format", "Error");
                    return;
                }
                openFileDialog1.FileName = string.Empty;
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(s);
                Text = $"Image segmentation - {saveFileDialog1.FileName}";                                
                segmentSeeds.Clear();
            }
        }

        private void exit1_Click(object sender, EventArgs e)
        {
            Close();
        }        

        private void new1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = string.Empty;
            srcImage = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
            using (Graphics g = Graphics.FromImage(srcImage))                            
                g.Clear(Color.White);            
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = srcImage.Clone() as Image;
            segmentSeeds.Clear();
        }        
        // функция для дочернего потока
        void threadFunc(object o)
        {
            var algo = o as SegmentAlgorithmClient;
            int t = 0;
            while (DoSegmentation && algo.evolution())
            {
                Invoke(new RefreshImageDelegate(RefreshImage), algo);
                Console.WriteLine("iter: " + ++t);
                if (algo.iSegAlgo is ISODATA || algo.iSegAlgo is Graph) break;
                Thread.Sleep(0);
            }            
        }

        delegate void RefreshImageDelegate(SegmentAlgorithmClient algo);
        // Функция для делегата
        void RefreshImage(SegmentAlgorithmClient algo)
        {                        
            Bitmap bmp = pictureBox1.Image as Bitmap;            
            algo.convertBitmap(ref bmp);
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
            DoSegmentation = true;
            SegmentAlgorithmClient algo = new SegmentAlgorithmClient(new Graph(bmp));
            if (activeRadioButton.Checked)
            {
                algo.iSegAlgo = new Automaton(bmp);
                // интерактивная разметка
                (algo.iSegAlgo as Automaton).userAction(segmentSeeds);
            }
            else
                algo.iSegAlgo = new ISODATA(bmp, (int)numericUpDown1.Value);            
            // Создание & запуск отдельного потока для эволюции
            calcThread = new Thread(threadFunc);
            calcThread.Start(algo);
        }

        private void reset1_Click(object sender, EventArgs e)
        {           
            try
            {                
                pictureBox1.Image?.Dispose();                
                pictureBox1.Image = srcImage.Clone() as Image;
                segmentSeeds.Clear();
                DoSegmentation = false;
                if (calcThread != null && calcThread.IsAlive)                    
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
                    using (Graphics g = Graphics.FromImage(pictureBox1.Image))                    
                        g.DrawLine(pen, pCur, e.Location);                    
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
            pen = sender == radioButton1 ? Pens.Red : Pens.Blue;            
        }        

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Visible = !groupBox1.Visible;            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DoSegmentation = false;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            groupBox3.Visible = !groupBox3.Visible;
        }       
    }
}
