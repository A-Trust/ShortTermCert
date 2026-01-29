namespace TestClient.DataContract;

public sealed class StartSignatureResponse
{
    public string? startUrl { get; set; }
    public List<HashDataResponse>? docIds { get; set; }

}
