namespace dot_net_sdk.exception
{
    internal class InputValidator
    {
        internal static bool validateNotBlank(string input, string errorMsg)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new InvalidDataException(errorMsg);
            }

            return true;
        }
    }
}