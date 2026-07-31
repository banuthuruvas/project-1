namespace Auth.Models;

public class ProfileResponseRoot
{
    public ProfileResponse? result { get; set; }
}

public class ProfileResponse
{
    public string? name { get; set; }

    public ProfileResponseAttributes? attributes { get; set; }
}

public class ProfileResponseAttributes
{
    public string? email { get; set; }
    public string? dept { get; set; }
}