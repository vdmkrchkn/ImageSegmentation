using System;
using System.IO;
using System.Xml.Serialization;

namespace GrowCut.src.cfg
{
    public class IsoDataConfig
    {        
        /// <summary>
        ///     Пустой инициализатор для сериализации
        /// </summary>
        IsoDataConfig() { }
        /// <summary>
        ///     Инициализация 
        /// </summary>
        /// <param name="fileName">название файла конфигурации</param>
        public IsoDataConfig(string fileName)
        {
            _fileName = fileName;
        }
        /// <summary>
        ///     Инициализация параметров из xml файла        
        /// </summary>        
        public void Init()
        {
            using (Stream stream = new FileStream(_fileName, FileMode.Open))                
            {                
                XmlSerializer serializer = new XmlSerializer(typeof(IsoDataConfig));
                IsoDataConfig settings = (IsoDataConfig)serializer.Deserialize(stream);
                //
                nClusters = settings.nClusters;
                ClusterMaxSz = settings.ClusterMaxSz;
                Deviation = settings.Deviation;
                ClusterDistance = settings.ClusterDistance;
                nMergeClusters = settings.nMergeClusters;
                nIters = settings.nIters;
            }                
        }
        /// <summary>
        ///    Сохранение параметров в xml файл
        /// </summary>        
        public void Save()
        {
            using (Stream writer = new FileStream(_fileName, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(IsoDataConfig));
                serializer.Serialize(writer, this);
            }
        }
        /// <summary>
        ///     Число кластеров
        /// </summary>
        public int nClusters { get; set; } = 2;
        /// <summary>
        ///     Допустимое кол-во точек в кластере        
        /// </summary>
        public int ClusterMaxSz { get; set; } = 1;
        /// <summary>
        ///     Среднеквадратичное отклонение
        /// </summary>
        public float Deviation { get; set; } = 1;
        /// <summary>
        ///     Компактность кластера        
        /// </summary>
        public float ClusterDistance { get; set; } = 4;
        /// <summary>
        ///     Кол-во пар центров кластеров, которые можно объединить
        /// </summary>
        public int nMergeClusters { get; set; } = 2;
        /// <summary>
        ///     Число итераций
        /// </summary>
        public int nIters { get; set; } = 10;
        /// <summary>
        /// 
        /// </summary>
        string _fileName;
    }
}
