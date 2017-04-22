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
        public void mark(LABEL label, double strength)
        {
            l = label;
            theta = strength;
        }

        public Point pt;       // координаты
        public LABEL l;        // метка
        public double theta;   // сила in [0,1]
        public Color clr;      // цвет        
    }

    internal enum PixelNeighborhood { NEUMANN, MOORE };

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
            double maxColorNorma = Color.White.Length(); // max{||C||_2}          
            for (int x = 1; x < states.GetLength(0) - 1; ++x)
                for (int y = 1; y < states.GetLength(1) - 1; ++y) 
                {
                    Cell p = states[x, y];                    
                    statesNext[x, y] = p;                    
                    foreach(Cell q in getNeighbors(p))
                    {
                        double g = 1 - p.clr.Distance(q.clr) / maxColorNorma;
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
                        cell.l == LABEL.BACKGROUND ? cell.clr.toGrayLevel() :
                            cell.l == LABEL.NONE ? Color.White : cell.clr);
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine($"{ae.Message}: {cell.pt}");
                    return;
                }
        }
        /// <summary>
        ///     получение окрестность клетки
        /// </summary>
        /// <param name="cell">клетка, для которой необходимо получить окрестность</param>
        /// <param name="neighborhood">вид окрестности</param>
        /// <returns>набор смежных клеток</returns>
        IList<Cell> getNeighbors(Cell cell,
            PixelNeighborhood neighborhood = PixelNeighborhood.MOORE)
        {                        
            IList<Cell> neighborhoods = new List<Cell>();  // мн-во "соседей"
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
            neighborhoods.Add(states[cell.pt.X - 1, cell.pt.Y]);
            neighborhoods.Add(states[cell.pt.X + 1, cell.pt.Y]);
            // вертикальные
            neighborhoods.Add(states[cell.pt.X, cell.pt.Y - 1]);
            neighborhoods.Add(states[cell.pt.X, cell.pt.Y + 1]);
            if (neighborhood == PixelNeighborhood.MOORE)
            {
                // левые диагональные
                neighborhoods.Add(states[cell.pt.X - 1, cell.pt.Y - 1]);
                neighborhoods.Add(states[cell.pt.X - 1, cell.pt.Y + 1]);
                // правые диагональные
                neighborhoods.Add(states[cell.pt.X + 1, cell.pt.Y - 1]);
                neighborhoods.Add(states[cell.pt.X + 1, cell.pt.Y + 1]);
            }
            return neighborhoods;
        }
        //
        Cell[,] states;        
    }
}
