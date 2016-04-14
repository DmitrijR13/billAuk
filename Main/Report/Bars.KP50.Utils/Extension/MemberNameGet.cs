namespace Bars.KP50.Utils
{
    /// <summary>
    /// Тип получения полного имени свойства
    /// </summary>
    public enum MemberNameGet
    {
        /// <summary>
        /// Имя последнего свойства в выражении.
        /// Например для выражения x => x.Object.Id
        /// будет возвращен Id
        /// </summary>
        Last,
        /// <summary>
        /// Склейка имен свойств в выражении.
        /// Например для выражения x => x.Object.Id
        /// будет возвращен ObjectId
        /// </summary>
        Concat,
        /// <summary>
        /// Склейка имен свойств в выражении с разделителем.
        /// Например для выражения x => x.Object.Id
        /// будет возвращен Object.Id
        /// </summary>
        Dotted
    }
}