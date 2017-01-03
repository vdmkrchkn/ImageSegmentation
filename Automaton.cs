using System;
using System.Collections.Generic;
using System.Drawing;

namespace Segmentation
{
    public enum LABEL { NONE, OBJECT, BACKGROUND };
    // тип клетка
    struct Cell
    {
        public Cell(Point p, Color c)
        {
            this.pt = p;
            this.l = LABEL.NONE;
            this.theta = 0;            
            this.clr = c;            
        }        
        // маркировка клетки
        public void mark(LABEL label,double strength)
        {
            this.l = label;
            this.theta = strength;
        }

        public Point pt;       // координаты
        public LABEL l;        // метка
        public double theta;   // сила in [0,1]
        public Color clr;      // цвет        
    }

    class Automaton : ISegmentAlgorithm
    {
        public Automaton(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            this.states = new Cell[w, h];
            // начальное состояние клеток
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)                
                {
                    this.states[x, y] = new Cell(new Point(x, y), bmp.GetPixel(x, y));  // инициализация S
                    //if(h - 1  > y && y > 0  && w - 1 > x && x > 0)                        
                }
            this.maxColorNorma = colorLength(Color.White); // max{||C||_2}
        }        
        // маркировка выбранных клеток
        public void userAction(Dictionary<LABEL, List<Point>> seed)
        {
            foreach (var pair in seed)            
                foreach (var pt in pair.Value)
                    this.states[pt.X, pt.Y].mark(pair.Key, 1);                        
        }        
        //
        public bool evolution()
        {
            bool isChanged = false;
            Cell[,] statesNext = new Cell[this.states.GetLength(0),this.states.GetLength(1)];            
            for (int x = 1; x < this.states.GetLength(0) - 1; ++x)
                for (int y = 1; y < this.states.GetLength(1) - 1; ++y) 
                {
                    Cell p = this.states[x, y];                    
                    statesNext[x, y] = p;                    
                    foreach(Cell q in getNeighbors(p))
                    {
                        double g = 1 - colorDistance(p.clr, q.clr) / maxColorNorma;
                        if (g * q.theta > p.theta)
                        {
                            statesNext[x, y].mark(q.l,g * q.theta);
                            isChanged = true;
                        }                            
                    }                    
                }
            this.states = statesNext;
            return isChanged;
        }
        // преобразование из клеток в изображение Bitmap
        public void convertBitmap(ref Bitmap bmp)
        {
            foreach (Cell p in this.states)                
                try
                {
                    bmp.SetPixel(p.pt.X, p.pt.Y, p.l == LABEL.BACKGROUND ? toGrayLevel(p.clr) : p.l == LABEL.NONE ? Color.White : p.clr);
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine(ae.Message + " (" + p.pt.X + "," + p.pt.Y + ")");
                    return;
                }
        }

        public static double colorDistance(Color p, Color q)
        {
            return Math.Sqrt(Math.Pow(Math.Abs(p.R - q.R), 2) + Math.Pow(Math.Abs(p.G - q.G), 2) + Math.Pow(Math.Abs(p.B - q.B), 2));
        }

        public static double colorLength(Color p)
        {
            return Math.Sqrt(Math.Pow(Math.Abs(p.R), 2) + Math.Pow(Math.Abs(p.G), 2) + Math.Pow(Math.Abs(p.B), 2));
        }
        // преобразование цветного в полутоновое
        public static Color toGrayLevel(Color c)
        {
            return Color.FromArgb((int)Math.Round(.3 * c.R), (int)Math.Round(.59 * c.G), (int)Math.Round(.11 * c.B));
        }
        // возвращает массив "соседей" cell: is8connected==FALSE - окрестность фон Неймана,TRUE - окрестность Мура
        List<Cell> getNeighbors(Cell cell, bool is8connected = true)
        {            
            List<Cell> neighbors = new List<Cell>();  // мн-во "соседей"
            //if (cell.X == 0) // клетка у левой границы
            //{
            //    this.neighbors.Add(states[cell.X, cell.Y + 1]);
            //    this.neighbors.Add(states[cell.X + 1, cell.Y]);
            //    this.neighbors.Add(states[cell.X + 1, cell.Y + 1]);
            //    if (cell.Y > 0) // клетка НЕ у верхней границы
            //    {
            //        this.neighbors.Add(states[cell.X, cell.Y - 1]);
            //        this.neighbors.Add(states[cell.X + 1, cell.Y - 1]);
            //    }                
            //}
            // горизонтальные
            neighbors.Add(states[cell.pt.X - 1, cell.pt.Y]);
            neighbors.Add(states[cell.pt.X + 1, cell.pt.Y]);
            // вертикальные
            neighbors.Add(states[cell.pt.X, cell.pt.Y - 1]);
            neighbors.Add(states[cell.pt.X, cell.pt.Y + 1]);
            if (is8connected)
            {
                // левые диагональные
                neighbors.Add(states[cell.pt.X - 1, cell.pt.Y - 1]);
                neighbors.Add(states[cell.pt.X - 1, cell.pt.Y + 1]);
                // правые диагональные
                neighbors.Add(states[cell.pt.X + 1, cell.pt.Y - 1]);
                neighbors.Add(states[cell.pt.X + 1, cell.pt.Y + 1]);
            }
            return neighbors;
        }

        Cell[,] states;
        double maxColorNorma;
    }
}
