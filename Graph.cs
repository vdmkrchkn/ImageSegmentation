using System;
using System.Drawing;
//
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.MaximumFlow;

namespace Segmentation
{
    class Graph : ISegmentAlgorithm
    {
        public Graph(Bitmap bmp)
        {
            // тестирование потока на графе
            var g = new AdjacencyGraph<int, TaggedEdge<int, int>>();
            for (int i = 0; i < 6; i++)
                g.AddVertex(i);
            // инициализируем веса ребер (см Кормен и ко, с. 736, рис. 26.1)                                                                                               
            g.AddEdge(new TaggedEdge<int, int>(0, 1, 16));
            g.AddEdge(new TaggedEdge<int, int>(0, 2, 13));
            g.AddEdge(new TaggedEdge<int, int>(1, 2, 10));
            g.AddEdge(new TaggedEdge<int, int>(1, 3, 12));
            g.AddEdge(new TaggedEdge<int, int>(2, 1, 4));
            g.AddEdge(new TaggedEdge<int, int>(2, 4, 14));
            g.AddEdge(new TaggedEdge<int, int>(3, 2, 9));
            g.AddEdge(new TaggedEdge<int, int>(3, 5, 20));
            g.AddEdge(new TaggedEdge<int, int>(4, 3, 7));
            g.AddEdge(new TaggedEdge<int, int>(4, 5, 4));
            foreach (var edge in g.Edges)
                Console.WriteLine(edge + ": " + edge.Tag);
            // A function with maps an edge to its capacity
            Func<TaggedEdge<int, int>, double> capacityFunc = (edge => edge.Tag);
            // A function which takes a vertex and returns the edge connecting to its predecessor
            // in the flow network
            TryFunc<int, TaggedEdge<int, int>> flowPredecessors;
            // A function used to create new edges during the execution of the algorithm.
            // These edges are removed before the computation returns
            EdgeFactory<int, TaggedEdge<int, int>> edgeFactory = (source, target) =>
                new TaggedEdge<int, int>(source, target, 0);
            var reversedEdgeAugmentor = new
               ReversedEdgeAugmentorAlgorithm<int, TaggedEdge<int, int>>(g, edgeFactory);
            reversedEdgeAugmentor.AddReversedEdges();
            // computing the maximum flow (23) using Edmonds Karp
            double flow = AlgorithmExtensions.MaximumFlowEdmondsKarp<int, TaggedEdge<int, int>>(
                g,
                capacityFunc,
                0, 5,
                out flowPredecessors,
                edgeFactory);
            Console.WriteLine("maxflow = {0}", flow);
            foreach (var e in reversedEdgeAugmentor.ReversedEdges)
                Console.WriteLine(e.Key + "," + e.Value);
        }
        // преобразование в изображение Bitmap
        public void convertBitmap(ref Bitmap bmp) { }
        //
        public bool evolution()
        {
            return true;
        }
    }
}
