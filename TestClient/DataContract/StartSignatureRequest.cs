namespace TestClient.DataContract;

public sealed class StartSignatureRequest
{
    public string? redirectUrl { get; set; }
    public string? errorUrl { get; set; }
    public List<HashDataRequest> hashes { get; set; } = new();
}
