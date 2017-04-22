using System.Drawing;
using static System.Math;

namespace Segmentation
{
    public static class Extensions
    {
        /// <summary>
        ///     вычисление расстояния до цвета System.Drawing.Color
        /// </summary>
        /// <param name="clrFrom">исходный цвет System.Drawing.Color</param>
        /// <param name="clrTo">конечный цвет System.Drawing.Color, до которого считаем расстояние</param>
        /// <returns>расстояние до конечного цвета</returns>
        public static double Distance(this Color clrFrom, Color clrTo)
        {
            return Sqrt(
                        Pow(Abs(clrFrom.R - clrTo.R), 2) +
                        Pow(Abs(clrFrom.G - clrTo.G), 2) +
                        Pow(Abs(clrFrom.B - clrTo.B), 2)
                        );
        }
        /// <summary>
        ///     вычисление нормы цвета System.Drawing.Color
        /// </summary>
        /// <param name="aClr">цвет System.Drawing.Color</param>
        /// <returns>норму цвета</returns>
        public static double Length(this Color aClr)
        {
            return aClr.Distance(Color.Black);            
        }
        /// <summary>
        ///     преобразование цвета System.Drawing.Color в оттенок серого
        /// </summary>
        /// <param name="c">цвет System.Drawing.Color</param>
        /// <returns>оттенок серого для заданного цвета</returns>
        public static Color toGrayLevel(this Color c)
        {
            return Color.FromArgb((int)Round(.3 * c.R),
                                  (int)Round(.59 * c.G),
                                  (int)Round(.11 * c.B)
                                  );
        }
    }
}
