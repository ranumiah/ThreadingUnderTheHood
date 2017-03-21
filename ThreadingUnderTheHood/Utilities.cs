using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThreadingUnderTheHood
{
    class Utilities
    {
        #region Enum To Title
        /// <summary>
        /// Converts and enum to a presentable title.
        /// </summary>
        /// <param name="enumToConvert">The enum to be converted.</param>
        /// <returns>A presentable title.</returns>
        public static string EnumToTitle(Enum enumToConvert)
        {
            return System.Text.RegularExpressions.Regex.Replace(enumToConvert.ToString(), "[A-Z]", " $0").Trim();
        }
        #endregion

        #region Highlight Button
        /// <summary>
        /// Highlights buttons so that they stands out, or sets them back to normal so that they don't stand out.
        /// </summary>
        /// <param name="highlight">Whether to highlight the buttons.</param>
        /// <param name="buttons">Buttons to be processed.</param>
        public static void HighlightButtons(bool highlight, params Button[] buttons)
        {
            foreach (Button button in buttons)
            {
                button.Foreground = Brushes.Black;
                button.FontWeight = highlight ? FontWeights.Bold : FontWeights.Normal;
            }
        }
        #endregion

        #region Memory Utilization
        /// <summary>
        /// Retrieves the amount of memory allocated to the process in megabytes.
        /// </summary>
        /// <param name="processToEvaluate">The process to evaluate.</param>
        /// <returns>The memory allocated to the process in megabytes.</returns>
        public static long MemoryUtilization_inMegaBytes(Process processToEvaluate)
        {
            return processToEvaluate.PrivateMemorySize64 / (1024 * 1024);
        }
        #endregion
    }
}
