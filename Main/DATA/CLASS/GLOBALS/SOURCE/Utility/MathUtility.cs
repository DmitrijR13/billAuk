using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.Utility
{
    public static class MathUtility
    {
        /// <summary>
        /// распределяет сумму пропорционально положительным эталонным значениям 
        /// </summary>
        /// <param name="value">Сумма к распределению</param>
        /// <param name="pattern">Эталонные значения</param>
        /// <returns>Список распределения</returns>
        public static List<decimal> DistributeSum(decimal value, List<decimal> pattern)
        {
            decimal patternPositiveSum = 0;
            int indexOfMaxPatternSum = -1;
            decimal maxPatterSum = 0;
            int count = pattern.Count;
            
            for (int i = 0; i < count; i++)
            {
                if (pattern[i] > 0)
                {
                    patternPositiveSum+=pattern[i];
                    if (maxPatterSum < pattern[i])
                    {
                        maxPatterSum = pattern[i];
                        indexOfMaxPatternSum = i;
                    }
                }
            }

            List<decimal> list = new List<decimal>();
            if (patternPositiveSum < (decimal)0.0001)
            {
                for (int i = 0; i < count; i++) list.Add(0);
                return list;
            }
 
            decimal d;
            decimal distributedSum = 0;
            for (int i = 0; i < count; i++)
            {
                if (pattern[i] > 0)
                {
                    d = Math.Round(pattern[i] / patternPositiveSum * value, 2);
                    list.Add(d);
                    distributedSum += d;
                }
                else list.Add(0);
            }

            d = value - distributedSum;
            if (d != 0)
            {
                list[indexOfMaxPatternSum] += d;
            }

            return list;
        }

        /// <summary>
        /// наибольший общий делитель
        /// </summary>
        /// <param name="n"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static int Nod(int n, int d)
        {
            int temp;
            n = Math.Abs(n);
            d = Math.Abs(d);
            while (d != 0 && n != 0)
            {
                if (n % d > 0)
                {
                    temp = n;
                    n = d;
                    d = temp % d;
                }
                else break;
            }
            if (d != 0 && n != 0) return d;
            else return 0;
        } 
    }
}
