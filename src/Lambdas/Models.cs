namespace Lambdas;

public class Game
{
  public string? Identifier { get; set; }
  public string? Date { get; set; }
  public string? DateTime { get; set; }
  public string? CountryCode { get; set; }
  public int Division { get; set; }
  public string? LastUpdatedAt { get; set; }
  public long? ExpiresAt { get; set; }
  public Team? HomeTeam { get; set; }
  public Team? AwayTeam { get; set; }
  public List<Odds> Odds { get; set; } = new List<Odds>();


  public static Game FromStringArray(string[] input)
  {
    var homeTeam = new Team
    {
      Identifier = Utils.Slugify(input[3]),
      Name = input[3],
    };

    var awayTeam = new Team
    {
      Identifier = Utils.Slugify(input[4]),
      Name = input[4],
    };

    var game = new Game
    {
      Identifier = $"{Utils.ExtractCountryCode(input[0])}#{homeTeam.Identifier}#{awayTeam.Identifier}",
      Date = Utils.ConvertToDate(input[1]),
      DateTime = Utils.ConvertToDateTime(input[1], input[2]),
      CountryCode = Utils.ExtractCountryCode(input[0]),
      Division = Utils.ExtractDivision(input[0]),
      HomeTeam = homeTeam,
      AwayTeam = awayTeam,
      Odds = new List<Odds> {
        new() {
          Identifier = "b365",
          Home = Utils.TryParseDecimal(input[11]),
          Draw = Utils.TryParseDecimal(input[12]),
          Away = Utils.TryParseDecimal(input[13]),
        }
      },
      LastUpdatedAt = System.DateTime.UtcNow.ToString("o"),
      ExpiresAt = Utils.DateToExpiration(input[1]),
    };

    return game;
  }

}

public class Team
{
  public string? Identifier { get; set; }
  public string? Name { get; set; }
}

public class Odds
{
  public string? Identifier { get; set; }
  public decimal? Home { get; set; }
  public decimal? Draw { get; set; }
  public decimal? Away { get; set; }
}