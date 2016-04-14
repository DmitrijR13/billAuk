namespace Globals.SOURCE.Config
{
    /// <summary>Интерфейс провайдера конфигураций</summary>
    public interface IConfigProvider
    {
        /// <summary>Получить настройки приложения</summary>
        /// <returns>Возвращает экземпляр <see cref="AppConfig"/></returns>
        AppConfig GetConfig();
    }
}