using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Template
{
    public class Instance
    {
        public static List<IFormat> LoadedFormats;
        public List<Task<IFormat>> tasks;
        protected Instance()
        {
            LoadedFormats = new List<IFormat>();
            tasks = new List<Task<IFormat>>();
        }

        protected sealed class SingletonCreator
        {
            private static readonly Instance instance = new Instance();
            public static Instance ObjInstance { get { return instance; } }
        }

        /// <summary>
        /// Экземпляр объекта Проверщика форматов
        /// </summary>
        public static Instance objectInstance
        {
            get { return SingletonCreator.ObjInstance; }
        }

        public IFormat GetFormatProgress(int taskID)
        {
            var firstOrDefault = tasks.FirstOrDefault(x => x.Id == taskID);
            return firstOrDefault != null ? firstOrDefault.Result : null;
        }

        public int AddTask(FormatDescription format, string FileName, string Path)
        {
            var template = new FormatTemplate(format.Version, format.FormatName, 0, FileName, Path) { Dict = format.Dict };
            tasks.Add((Task<IFormat>)Task.Factory.StartNew(() =>
            {
                var dt = new object();
                var ret = template.Load(ref dt);
                if (!ret.result) return;
                ret = template.Check(ref dt);
                if (!ret.result) return;
                ret = template.CreateProtocol(ref dt);
            }));
            return tasks.Last().Id;

        }
    }
}
