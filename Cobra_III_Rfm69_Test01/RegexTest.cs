using System;
using Microsoft.SPOT;
using System.Text.RegularExpressions;

//namespace HeatingCurrentSurvey
namespace   Cobra_III_Rfm69_Test01
{
    public static class RegexTest
    {
        public static void ThrowIfNotValid(Regex pRegex, string[] pInStrings)
        {
            bool allAreValid = true;

            foreach(string testString in pInStrings)
            {
                if (!pRegex.IsMatch(testString))
                {
                    allAreValid = false;
                }
            }           
            if (!allAreValid)
            {
                throw new NotSupportedException("Regex-Test failed. Input strings (e.g. tablenames in Azure Storage) must be alphanumeric ");
            }
        }

    }
}
