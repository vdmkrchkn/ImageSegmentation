using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Math;

namespace Segmentation
{
    public enum LABEL { NONE, OBJECT, BACKGROUND };
    // тип клетка
    struct Cell
    {
        public Cell(Point p, Color c)
        {
            pt = p;
            l = LABEL.NONE;
            theta = 0;
            clr = c;            
        }        
        // маркировка клетки
        public void mark(LABEL label,double strength)
        {
            l = label;
            theta = strength;
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
            int w = bmp.Width, h = bmp.Height;
            states = new Cell[w, h];
            // начальное состояние клеток
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)                
                {
                    // инициализация S
                    states[x, y] = new Cell(new Point(x, y), bmp.GetPixel(x, y));
                    //if(h - 1  > y && y > 0  && w - 1 > x && x > 0)                        
                }
            maxColorNorma = colorLength(Color.White); // max{||C||_2}
        }        
        // маркировка выбранных клеток
        public void userAction(Dictionary<LABEL, List<Point>> seed)
        {
            foreach (var pair in seed)            
                foreach (var pt in pair.Value)
                    states[pt.X, pt.Y].mark(pair.Key, 1);                        
        }        
        //
        public bool evolution()
        {
            bool isChanged = false;
            Cell[,] statesNext = new Cell[states.GetLength(0), states.GetLength(1)];            
            for (int x = 1; x < states.GetLength(0) - 1; ++x)
                for (int y = 1; y < states.GetLength(1) - 1; ++y) 
                {
                    Cell p = states[x, y];                    
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
            states = statesNext;
            return isChanged;
        }
        // преобразование из клеток в изображение Bitmap
        public void convertBitmap(ref Bitmap bmp)
        {
            foreach (Cell cell in states)                
                try
                {
                    bmp.SetPixel(cell.pt.X, cell.pt.Y,
                        cell.l == LABEL.BACKGROUND ? toGrayLevel(cell.clr) :
                            cell.l == LABEL.NONE ? Color.White : cell.clr);
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine($"{ae.Message}: {cell.pt}");
                    return;
                }
        }

        public static double colorDistance(Color p, Color q)
        {
            return Sqrt(Pow(Abs(p.R - q.R), 2) + Pow(Abs(p.G - q.G), 2) + Pow(Abs(p.B - q.B), 2));
        }

        public static double colorLength(Color p)
        {
            return Sqrt(Pow(Abs(p.R), 2) + Pow(Abs(p.G), 2) + Pow(Abs(p.B), 2));
        }
        // преобразование цветного в полутоновое
        public static Color toGrayLevel(Color c)
        {
            return Color.FromArgb((int)Round(.3 * c.R), (int)Round(.59 * c.G), (int)Round(.11 * c.B));
        }
        /// <summary>
        /// получение "соседей" клетки
        /// </summary>
        /// <param name="cell">рассматриваемая клетка</param>
        /// <param name="is8connected">вид окрестности: false - фон Неймана, true - Мура</param>
        /// <returns>массив соседей</returns>
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
