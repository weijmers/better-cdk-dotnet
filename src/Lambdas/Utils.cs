using System.Text.RegularExpressions;

public static class Utils
{
  public static string ConvertToDate(string date)
  {
    var iso8601Date = Regex.Replace(date, @"^([\d]{2})\/([\d]{2})\/([\d]{4})$", "$3-$2-$1");
    return iso8601Date;
  }

  public static string ConvertToDateTime(string date, string time)
  {
    var iso8601Date = ConvertToDate(date);
    if (!Regex.IsMatch(time, @"^[\d]{2}:[\d]{2}$"))
    {
      return $"{iso8601Date} 00:00";
    }

    return $"{iso8601Date} {time}";
  }

  public static long DateToExpiration(string date, int daysToExpiration = 30)
  {
    var parsedDate = DateTimeOffset.Parse(ConvertToDate(date));
    var unixSeconds = parsedDate.ToUnixTimeSeconds();
    return unixSeconds + (60 * 60 * 24 * daysToExpiration);
  }

  public static string Slugify(string input)
  {
    return Regex.Replace(Regex.Replace(input.ToLower(), @"[^\w ]+", ""), @"\s+", "-");
  }

  public static string ExtractCountryCode(string input)
  {
    return Slugify(input.Substring(0, input.Length - 1));
  }

  public static int ExtractDivision(string input)
  {
    var countryCode = ExtractCountryCode(input);
    var division = input.Substring(input.Length - 1);

    var parsedDivision =
      division == "C" ?
        4 : int.Parse(division);

    if (countryCode == "e")
    {
      return parsedDivision + 1;
    }

    return parsedDivision;
  }

  public static decimal TryParseDecimal(string input, decimal defaultValue = -1)
  {
    if (decimal.TryParse(input, out var result))
    {
      return result;
    }

    return defaultValue;
  }
}