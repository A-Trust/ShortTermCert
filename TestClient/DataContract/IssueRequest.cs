namespace TestClient.DataContract;

public sealed class IssueRequest
{
    public string? redirectUrl { get; set; }
    public string? errorUrl { get; set; }
}
