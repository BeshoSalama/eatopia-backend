using System.Text;
using System.Text.Json;

namespace Eatopia.Api.Serialization;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                var hasPrev = i > 0;
                var hasNext = i + 1 < name.Length;

                var prevIsLowerOrDigit = hasPrev && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var prevIsUpperAndNextIsLower = hasPrev && char.IsUpper(name[i - 1]) && hasNext && char.IsLower(name[i + 1]);

                if (prevIsLowerOrDigit || prevIsUpperAndNextIsLower)
                    sb.Append('_');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
