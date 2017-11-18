using System;
using System.Drawing;
//
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.MaximumFlow;

namespace Segmentation
{
    public class Graph : ISegmentAlgorithm
    {
        public Graph(Bitmap bmp)
        {
            // инициализация графа
            graph = new AdjacencyGraph<int, TaggedEdge<int, int>>();
            int w = bmp.Width, h = bmp.Height;
            // добавление вершин - пикселей изображения
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    graph.AddVertex(y * w + x);
            // добавление вершины-источника S
            graph.AddVertex(w * h);
            // добавление вершины-стока T
            graph.AddVertex(w * h + 1);
            //
            for (int v = 0; v < graph.VertexCount - 2; ++v)
            {
                // соединение ребрами вершин с S
                graph.AddEdge(new TaggedEdge<int, int>(v, w * h, -1));
                // соединение вершин с T
                graph.AddEdge(new TaggedEdge<int, int>(v, w * h + 1, 1));
            }

        }
        // преобразование в изображение Bitmap
        public void convertBitmap(ref Bitmap bmp) { }
        //
        public bool evolution() => true;
        //
        public AdjacencyGraph<int, TaggedEdge<int, int>> graph  {get;}
    }
}
