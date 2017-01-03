using System.Drawing;

namespace Segmentation
{
    public interface ISegmentAlgorithm
    {                
        // преобразование в изображение Bitmap
        void convertBitmap(ref Bitmap bmp);
        /// <summary>
        /// Переход к новому состоянию.
        /// </summary>
        /// <returns>произошло ли изменение</returns>
        bool evolution();
    }

    public class SegmentAlgorithmClient
    {
        /// <summary>
        /// Контекст стратегии
        /// </summary>
        public ISegmentAlgorithm iSegAlgo { get; set; }

        public SegmentAlgorithmClient(ISegmentAlgorithm iSegAlgo)
        {
            this.iSegAlgo = iSegAlgo;
        }
#region Операции
        public void convertBitmap(ref Bitmap bmp)
        {
            iSegAlgo.convertBitmap(ref bmp);
        }

        public bool evolution()
        {
            return iSegAlgo.evolution();
        }
#endregion
    }
}
