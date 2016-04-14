namespace Bars.QueueCore
{
    /// <summary>Состояние работы</summary>
    public enum JobState : byte
    {
        New = 10,
        Proccess = 20,
        End = 30
    }
}