using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Umbraco.Extensions
{
    internal static class NamespaceStringExtensions
    {
        /// <summary>
        /// Cleans a string to a namespace valid string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static string CleanStringForNamespace(this string input)
        {
            MatchCollection matches = Regex.Matches(input, @"(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)");

            // No matches found at all, but we have to return something
            if (matches.Count < 1)
            {
                return "";
            }

            IEnumerable<string> nameParts = matches.Cast<Match>().Select(x => x.Value);
            return string.Join(".", nameParts);
        }

    }
}
