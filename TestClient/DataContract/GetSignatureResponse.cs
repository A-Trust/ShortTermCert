namespace TestClient.DataContract
{
    public sealed class GetSignatureResponse
    {
        public List<SignatureResponseData> signatures { get; set; } = new();
    }
}
