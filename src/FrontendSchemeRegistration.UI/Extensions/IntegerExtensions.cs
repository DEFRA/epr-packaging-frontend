namespace FrontendSchemeRegistration.UI.Extensions;

public static class IntegerExtensions
{
    private static readonly string[] EnglishDigits = new string[10]
        { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

    // Current usage (tonnage in words) is English only
    private static readonly string[] WelshDigits = new string[10]
        { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

    public static string ToDigitsAsWords(this int value, string twoLetterISOLanguageName)
    {
        string digitsAsWords = string.Empty;
        string[] digits = EnglishDigits;
        int zeroToTen;

        if (!string.IsNullOrWhiteSpace(twoLetterISOLanguageName) && twoLetterISOLanguageName.Equals("cy", StringComparison.InvariantCultureIgnoreCase))
        {
            digits = WelshDigits;
        }

        do
        {
            zeroToTen = value % 10;
            string digit = digits[zeroToTen];

            if (digitsAsWords.Length == 0)
            {
                digitsAsWords = digit;
            }
            else
            {
                digitsAsWords = string.Concat(digit, " ", digitsAsWords);
            }
            value = value / 10;
        } while (value > 0);

        return digitsAsWords;
    }
}