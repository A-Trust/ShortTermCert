namespace TestClient.DataContract;

public sealed class SessionInfo
{
    public string sessionValidTo { get; set; } = string.Empty;
    public CertificateData? certificate { get; set; }
    public string? sigKeyValidTo { get; set; }
}
