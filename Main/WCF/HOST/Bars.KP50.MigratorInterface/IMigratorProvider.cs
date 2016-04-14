using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.KP50.Migration
{
    /// <summary>
    /// Интерфейс провайдера мигратора
    /// </summary>
    public interface IMigratorProvider
    {
        /// <summary>Зарегистрировать отчет</summary>
        /// <param name="migrator">Мигратор</param>
        void RegisterMigrator(IBaseMigrator migrator);
    }
}
