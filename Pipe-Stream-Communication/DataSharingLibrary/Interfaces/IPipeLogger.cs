namespace DataSharingLibrary.Interfaces
{
    public interface IPipeLogger
    {
        /// <summary>
        ///     Zpracuj chybu
        /// </summary>
        void Error(object message);

        /// <summary>
        ///     Zpracuj debug
        /// </summary>
        void Debug(object message);

        /// <summary>
        ///     Zpracuj info
        /// </summary>
        void Info(object message);
    }
}
