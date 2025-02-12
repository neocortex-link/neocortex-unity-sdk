namespace Neocortex
{
    public static class StringExtensions
    {
        public static string CorrectRTL(this string str)
        {
            return ArabicSupport.ArabicFixer.Fix(str, true);
        }
    }
}