using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GrowCut
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
}
