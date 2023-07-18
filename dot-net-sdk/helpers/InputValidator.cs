namespace eppo_sdk.helpers
{
    public class InputValidator
    {
        internal static void ValidateNotBlank(string input, string errorMsg)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new InvalidDataException(errorMsg);
            }
        }
    }
}