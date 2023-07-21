namespace eppo_sdk.helpers
{
    public class InputValidator
    {
        public static bool ValidateNotBlank(string input, string errorMsg)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new InvalidDataException(errorMsg);
            }

            return true;
        }
    }
}