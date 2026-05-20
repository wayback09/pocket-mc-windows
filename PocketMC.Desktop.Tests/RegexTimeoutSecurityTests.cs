using System.Reflection;
using System.Text.RegularExpressions;
using PocketMC.Desktop.Features.Intelligence;
using PocketMC.Desktop.Features.Networking;

namespace PocketMC.Desktop.Tests;

public sealed class RegexTimeoutSecurityTests
{
    [Theory]
    [InlineData(typeof(SessionLogPreprocessor))]
    [InlineData(typeof(SimpleVoiceChatDetector))]
    public void ParserRegexes_UseFiniteMatchTimeouts(Type parserType)
    {
        foreach (Regex regex in GetRegexFields(parserType))
        {
            Assert.NotEqual(Regex.InfiniteMatchTimeout, regex.MatchTimeout);
        }
    }

    private static IEnumerable<Regex> GetRegexFields(Type type)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            object? value = field.GetValue(null);
            if (value is Regex regex)
            {
                yield return regex;
            }
            else if (value is IEnumerable<Regex> regexes)
            {
                foreach (Regex item in regexes)
                {
                    yield return item;
                }
            }
        }
    }
}
