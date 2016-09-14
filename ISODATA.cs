﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GrowCut
{
    class ISODATA
    {
        public ISODATA(Cluster[] groups, List<SamplePoint> points, int k, int minNumThres, 
            float std_deviationThres, float minDistanceThres, int maxMergeNumsThres, int maxIters)
        {
            this.groups = groups;
            this.points = points;
            this.dimension = points[0].Size;
            this.k = k;
            this.minNumThres = minNumThres;
            this.std_deviationThres = std_deviationThres;
            this.minDistanceThres = minDistanceThres;
            this.maxMergeNumsThres = maxMergeNumsThres;
            this.maxIters = maxIters;
        }
        // преобразование из выборки в изображение Bitmap
        public void convertBitmap(ref Bitmap bmp)
        {
            foreach (SamplePoint p in this.points)
                try
                {                    
                    bmp.SetPixel(p.Index / bmp.Height, p.Index % bmp.Height, p.Cluster == 0 ? Automaton.toGrayLevel(p.Color) : p.Color);
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine(ae.Message + " (" + p.Index + ")");
                    return;
                }
        }
        //
        public void run()
        {
            for (int nIters = 1; nIters <= this.maxIters; ++nIters)
            {
                Console.WriteLine("--------Итерация №-" + nIters + "-------");
                // шаг 1
                assignGroups();
                //   
                //printGroups();
                // шаг 2  
                purgeGroups();
                // шаг 3  
                updateMeans();
                //   
                if (nIters == maxIters)
                    minDistanceThres = 0.0f;   // обнуление компактности
                else if (groups.GetLength(0) <= (this.k / 2) || (nIters % 2 == 1 && groups.GetLength(0) < 2 * this.k))
                {
                    // шаг 7
                    bool splitted = splitGroups();
                    if (splitted)                                                
                        continue;                    
                }
                // процедура слияния кластеров                     
                mergeGroups();                
            }
            Console.WriteLine("-------------------------SUCCESS----------------------------");
            assignGroups();   
            //   
            //printGroups();   
        }
        // определение соответствия точка-кластер
        private void assignGroups() 
        {
            Console.WriteLine("шаг 1");
            // замер времени
            foreach(SamplePoint p in this.points) 
            {                      
                int nearest = -1; // индекс ближайшей точки   
                double minDistance = double.MaxValue;   
                // нахождение кластера, ближайшего к точке, и расстояние до него
                for (int i = 0; i < this.groups.Count(); i++) 
                {   
                    double distance = SamplePoint.distance(p, groups[i].Center);   
                    if (distance < minDistance) 
                    {   
                        minDistance = distance;   
                        nearest = i;   
                    }
                }
                //Console.WriteLine(p + " ==> " + groups[nearest] + ", d = " + minDistance);  
                p.Cluster = nearest;  
            }
            //
        }
        // удаление кластеров, размер которых не соответствует порогу сходимости
        private void purgeGroups() 
        {
            Console.WriteLine("шаг 2");
            Dictionary<int, List<SamplePoint>> mapGroup2Samples = getMapGroup2Samples();      
            // 
            List<Cluster> eligibleGroups = new List<Cluster>();   
            for (int i = 0; i < groups.GetLength(0); ++i) 
            {   
                List<SamplePoint> groupPoints = mapGroup2Samples[i];   
                if (groupPoints.Count >= minNumThres)    
                    eligibleGroups.Add(groups[i]);   
                else 
                {   
                    Console.WriteLine("Удаление кластера" + groups[i]);   
                    // для всех точек удаляемого кластера
                    foreach(SamplePoint point in groupPoints)   
                        point.Cluster = -1; // ставим недопустимый # кластера
                    //--k;
                }
            }  
            // 
            for (int i = 0; i < eligibleGroups.Count; i++) 
            {   
                // 
                Cluster group = eligibleGroups[i];   
                List<SamplePoint> groupPoints = mapGroup2Samples[group.Number];   
                foreach(SamplePoint point in groupPoints) 
                {   
                    point.Cluster = i;   
                }   
                group.Number = i;   
            }      
            this.groups = new Cluster[eligibleGroups.Count];
            this.groups = eligibleGroups.ToArray();   
        }
        // определение соответствия кластер-набор точек
        private Dictionary<int, List<SamplePoint>> getMapGroup2Samples() 
        {   
            Dictionary<int, List<SamplePoint>> mapGroup2Samples = new Dictionary<int, List<SamplePoint>>();   
            foreach(SamplePoint point in this.points) 
            {
                if(mapGroup2Samples.ContainsKey(point.Cluster))
                    mapGroup2Samples[point.Cluster].Add(point);
                else
                {
                    var pl = new List<SamplePoint>();
                    pl.Add(point);
                    mapGroup2Samples.Add(point.Cluster, pl);
                }                
            }   
            return mapGroup2Samples;   
        } 
        // локализация и корректировка центров кластеров
        private void updateMeans() 
        {
            Console.WriteLine("шаг 3");
            //StringBuffer sbuf = new StringBuffer("ёьРВѕЫАаµДѕщЦµµгОЄЈє");      
            int pointCount = 0;   
            double totalDistance = 0.0f;      
            //   
            Dictionary<int, List<SamplePoint>> mapGroup2Samples = getMapGroup2Samples();
            for (int i = 0; i < groups.GetLength(0); i++) 
            {   
                List<SamplePoint> groupPoints = mapGroup2Samples[i];      
                if (groupPoints.Count != 0) 
                {   
                    float[] values = new float[this.dimension];   
                    //    
                    foreach(SamplePoint point in groupPoints) 
                    {   
                        float[] vv = point.Values;   
                        for (int j = 0; j < this.dimension; j++) {   
                            values[j] += vv[j];   
                        }   
                    }   
   
                    //   
                    for (int j = 0; j < this.dimension; j++) {   
                        values[j] /= groupPoints.Count;   
                    }   
   
                    SamplePoint meanPoint = new SamplePoint(-1, values);   
                    meanPoint.Cluster = i;   
   
                    groups[i].Center = meanPoint;   
   
                    double groupDistance = 0.0f;   
                    foreach (SamplePoint point in groupPoints)   
                        groupDistance += SamplePoint.distance(point, meanPoint);                          
                    double meanDistance = groupDistance / groupPoints.Count;   
                    groups[i].mDistance = meanDistance;      
                    //sbuf.append(groups[i] + ", АаДЪЖЅѕщѕаАл(" + meanDistance + "), ");      
                    pointCount += groupPoints.Count;   
                    totalDistance += groupDistance;   
                }   
            }   
   
            //System.out.println(sbuf.toString());   
            totalMeanDistance = (pointCount != 0) ? totalDistance / pointCount : 0.0;   
            //System.out.println("ЧЬЖЅѕщѕаАл = " + totalMeanDistance);   
        }
        //
        private bool splitGroups()
        {      
            Console.WriteLine("шаг 7");   
            bool splitted = false;      
            List<Cluster> groupList = new List<Cluster>();   
            //    
            Dictionary<int, List<SamplePoint>> mapGroup2Samples = getMapGroup2Samples();   
            for (int i = 0; i < groups.GetLength(0); i++) 
            {   
                //
                List<SamplePoint> groupPoints = mapGroup2Samples[i];   
                float[] meanVals = groups[i].Center.Values;
                float[] sum = new float[this.dimension];
   
                foreach (SamplePoint point in groupPoints) 
                {   
                    float[] values = point.Values;
                    for (int j = 0; j < this.dimension; j++)
                        sum[j] += (values[j] - meanVals[j]) * (values[j] - meanVals[j]);
                }   
                //string sbuf = "groups[" + (i + 1) + "]µД±кЧјІоПтБїОЄЈє(";   
                float[] std_deviationVector = new float[this.dimension];   
                int max = -1;   
                float maxItem = float.MinValue;
                for (int j = 0; j < this.dimension; j++)
                {
                    float std_deviation = (float)Math.Sqrt(sum[j] / groupPoints.Count);
                    std_deviationVector[j] = std_deviation;   
                    if (std_deviation > maxItem) 
                    {   
                        max = j;
                        maxItem = std_deviation;
                    }   
                    // logging  
                    //sbuf += std_deviation;
                    //if (j < this.dimension - 1)   
                    //    sbuf += ", ";
                }   
                //sbuf += "), Чоґу·ЦБїОЄЈє" + maxItem + ", ";
                //Console.WriteLine(sbuf);
   
                bool flag = false;   
                if (maxItem > std_deviationThres) 
                {   
                    if (groups.GetLength(0) <= this.k / 2 || (groups[i].mDistance > totalMeanDistance && groupPoints.Count > 2 * minNumThres)) 
                    {   
                        //  
                        splitted = true;   
                        flag = true;   
   
                        float delta = 0.5f * maxItem;   
                        float[] meanVals1 = meanVals.Clone() as float[];
                        meanVals1[max] += delta;   
                        SamplePoint meanPoint1 = new SamplePoint(-1, meanVals1);   
                        Cluster group1 = new Cluster(groupList.Count,meanPoint1);                            
                        groupList.Add(group1);   
   
                        float[] meanVals2 = meanVals.Clone() as float[];   
                        meanVals2[max] -= delta;   
                        SamplePoint meanPoint2 = new SamplePoint(-1, meanVals2);   
                        Cluster group2 = new Cluster(groupList.Count,meanPoint2);                        
                        groupList.Add(group2);
   
                        //Console.WriteLine((groups[i] + " ·ЦБСОЄЈє" + group1 + "---єН---" + group2));
                    }   
                }   
                if (!flag) 
                {   
                    groups[i].Number = groupList.Count;   
                    groupList.Add(groups[i]);   
                }
            }
            if (splitted) 
            {   
                for (int i = 0; i < groupList.Count; i++) 
                    groupList[i].Number = i;
                this.groups = new Cluster[groupList.Count];
                this.groups = groupList.ToArray();                
            } 
            else   
                Console.WriteLine("Не расщепляется");            
            return splitted;   
        }   
        // слияние кластеров
        private void mergeGroups() 
        {                  
            if (groups.GetLength(0) < 2) // минимум должна быть пара кластеров   
                return;                     
            Console.WriteLine("Шаг 10 - слияние кластеров");
            // вычисление расстояний между всеми парами кластеров   
            List<ClusterDistance> groupDistances = new List<ClusterDistance>(); // набор D   
            for (int i = 0; i < groups.GetLength(0) - 1; i++)
            {   
                Cluster iGroup = groups[i];   
                for (int j = i + 1; j < groups.GetLength(0); j++) 
                {   
                    Cluster jGroup = groups[j];   
                    double distance = SamplePoint.distance(iGroup.Center, jGroup.Center);   
                    //Console.WriteLine("From " + i + " to " + j + " distance " + distance);   
                    if (distance < this.minDistanceThres) 
                    {   
                        ClusterDistance groupDistance = new ClusterDistance(i, j, distance);   
                        groupDistances.Add(groupDistance);   
                    }   
                }   
            }   
            int size = Math.Min(groupDistances.Count, this.maxMergeNumsThres);   
            if (size < 1)
                return; 
            // шаг 11 - ранжирование D в порядке возрастания   
            groupDistances.Sort();   
            //StringBuffer sbuf = new StringBuffer("ѕЫАаѕаАлЕЕРтЈє");   
            //for (ClusterDistance distance : groupDistances) {   
            //    sbuf.append("D(" + distance.from + ", " + distance.to + ")="   
            //            + distance.distance + "  ");   
            //}   
            //System.out.println(sbuf);                           
            // шаг 12
            Dictionary<int, List<SamplePoint>> mapGroup2Samples = getMapGroup2Samples();
            // новый набор кластеров
            List<Cluster> groupList = new List<Cluster>(groups.ToList());
            for (int i = 0; i < size; i++) 
            {   
                ClusterDistance groupDistance = groupDistances[i];   
                Cluster group1 = groups[groupDistance.from];   
                Cluster group2 = groups[groupDistance.to];   
                int n1 = mapGroup2Samples[group1.Number].Count;
                int n2 = mapGroup2Samples[group2.Number].Count;   
                int total = n1 + n2;
                if (groupList.Contains(group1) && groupList.Contains(group2)) 
                {   
                    // удаление сливаемых кластеров
                    groupList.Remove(group1);
                    groupList.Remove(group2);   
                    // вычисление нового центра кластера z*
                    float[] meanValues = new float[this.dimension];   
                    float[] meanValues1 = group1.Center.Values;
                    float[] meanValues2 = group2.Center.Values;
                    for (int j = 0; j < this.dimension; j++)   
                        meanValues[j] = (meanValues1[j] * n1 + meanValues2[j] * n2) / total;
                    
                    SamplePoint meanPoint = new SamplePoint(-1, meanValues);   
                    Cluster group = new Cluster(groupList.Count,meanPoint);                    
                    groupList.Add(group);
                    //Console.WriteLine(group1 + "---+---" + group2 + " = " + group);   
                }   
            }   
   
            for (int i = 0; i < groupList.Count; i++)
                groupList[i].Number = i;
            this.groups = new Cluster[groupList.Count];
            this.groups = groupList.ToArray();
        }   
	
        int dimension;              // размерность вектора признаков        
        Cluster[] groups;           // набор кластеров
        List<SamplePoint> points;   // выборка данных для кластеризации        
        int k;                      // кол-во кластеров        
        int minNumThres;            // порог сходимости, с которым сравнивается кол-во точек в кластере        
        float std_deviationThres;   // параметр, характеризующий среднеквадратичное отклонение        
        float minDistanceThres;     // параметр компактности кластеров        
        int maxMergeNumsThres;      // максимальное количество пар центров кластеров, которые можно объединить        
        double totalMeanDistance;
        int maxIters;               // допустимое число циклов итерации
    }

    class Cluster
    {
        public Cluster(int number, SamplePoint center)
        {
            this.number = number;
            this.center = center;
        }

        public SamplePoint Center
        {
            get { return this.center; }
            set { this.center = value; }
        }        

        public int Number
        {
            get { return this.number; }
            set { this.number = value; }
        }        

        public double mDistance
        {
            get { return meanDistance; }
            set { this.meanDistance = value; }
        }        

        public override string ToString()
        {
            return "Cluster " + number + " : c = " + center;            
        }

        int number;         // номер
        SamplePoint center; // центр
        double meanDistance; // хз
    }

    class ClusterDistance : IComparable<ClusterDistance> 
    {
        public ClusterDistance(int from, int to, double distance) 
        {   
            this.from = from;   
            this.to = to;   
            this.distance = distance;   
        }   
   
        public int CompareTo(ClusterDistance that) 
        {
            if (this.distance < that.distance)
                return -1;
            else if (this.distance == that.distance)
                return 0;
            else
                return 1;               
        }

        public int from, to; 
        double distance;
    }

    class SamplePoint 
    {                    
        public SamplePoint(int index, float[] values) 
        {   
            this.index = index;   
            this.values = values;   
        }

        public SamplePoint(int index, Color clr)
        {
            this.index = index;
            this.values = new float[] { clr.R, clr.G, clr.B };
        }

        public int Cluster 
        {
            get { return this.clusterIdx; }
            set { this.clusterIdx = value; }
        }              
   
        public int Index
        {
            get { return index; }
            set { this.index = value; }
        }              
   
        public float[] Values
        {
            get { return this.values; }
            set { this.values = value; }
        }              
   
        public int Size
        {
            get { return values.GetLength(0); }
        }

        public Color Color
        {
            get { return Color.FromArgb((int)values[0], (int)values[1], (int)values[2]); }
        }
              
        public override string ToString() {   
            string sbuf = "x" + index + " (";              
            for (int i = 0; i < Size; i++) 
            {   
                sbuf += values[i];   
                if (i != Size - 1)   
                    sbuf += ", "; 
            }
            sbuf += ")";   
            return sbuf;
        }   
   
        public static double distance(SamplePoint dp1, SamplePoint dp2)
        {               
            float[] values1 = dp1.Values;   
            float[] values2 = dp2.Values;
            double result = 0;   
            for (int i = 0; i < dp1.Size; i++)
                result += Math.Pow(Math.Abs(values1[i] - values2[i]),2);            
            return Math.Sqrt(result);   
        }   
        
        int index;      
        float[] values; // вектор признаков   
        int clusterIdx;
    }   
}