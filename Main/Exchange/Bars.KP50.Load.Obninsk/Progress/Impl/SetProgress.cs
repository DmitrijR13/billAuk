using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.Progress.Interfaces;

namespace Bars.KP50.Load
{
    public class SetProgress:IProgressWork<ProgressEventArgs>
    {

        /// <summary>
        ///  общий процент выполнения
        /// </summary>
        public decimal SumPercentCompleted { get; set; }
        // количество строк, соответствующих проценту  percentGroupRows
        private decimal countRowByPercents;
        // процент соответствующий количеству строк countRowByPercents
        private decimal percentGroupRows = 0.03m;
        // true - процент будет обновляться после вставки одной строки, false- процент будет обновляться после количества строк countRowByPercents
        private bool isUpdateProgressByOneRow;
        /// <summary>
        /// процент выполнения одной строки
        /// </summary>
        private decimal percentOneRow;
        public event EventHandler<ProgressEventArgs> RaiseProgressEvent;
        /// <summary>
        /// Оповещение подписчиков на событие
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseProgressEvent(ProgressEventArgs e)
        {
            EventHandler<ProgressEventArgs> handler = RaiseProgressEvent;

            if (handler != null)
            {
                handler(this, e);
            }
        }
        #region методы инициализации и простановки процентов для одной строки 
        /// <summary>
        /// Инициализация, т.е. расчет процента выполнения одной строки
        /// </summary>
        /// <param name="totalCount"></param>
        public void Init(int totalCount)
        {
           if (totalCount<=0) return;
            SumPercentCompleted = 0;
            percentOneRow = 1m / totalCount;
            countRowByPercents = Math.Round(totalCount * percentGroupRows);
            isUpdateProgressByOneRow = countRowByPercents == 0;
        }

        /// <summary>
        /// Устанавливает проценты
        /// </summary>
        public void IncrementProgress(int countInsertedRows)
        {
            if (!isUpdateProgressByOneRow)
            {
                if (countInsertedRows < countRowByPercents) return;
                if (countInsertedRows % countRowByPercents != 0) return;
                SumPercentCompleted += percentGroupRows;
            }
            else
            {
                SumPercentCompleted += percentOneRow;
            }
   
            if (SumPercentCompleted > 1)
            {
                SumPercentCompleted = 1;
            }
            OnRaiseProgressEvent(new ProgressEventArgs(SumPercentCompleted));
        }
        #endregion
    }

    public class ProgressEventArgs : EventArgs
    {

        public ProgressEventArgs(decimal progress)
        {
            Progress = Math.Round(progress, 4);
        }
        public decimal Progress { get; set; }

    }

}
