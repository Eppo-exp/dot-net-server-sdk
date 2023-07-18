namespace eppo_sdk.exception
{
    public class EppoClientIsNotInitializedException : Exception
    {
        public EppoClientIsNotInitializedException(string message) : base(message) { }
    }
}