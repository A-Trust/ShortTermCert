using ATrustIdentRecord;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Security.Cryptography;
using TestClient.DataContract;

namespace TestClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        var config = configBuilder.Build();

        int numHashes = config.GetValue<int>("NumHashes", 3);
        int numSignatureCall = config.GetValue<int>("NumSignatureCall", 1);
        var certpath = config.GetValue<string>("Certpath");
        var certpwd = config.GetValue<string>("Certpwd");
        var identRecordUrl = config.GetValue<string>("IdentRecordUrl") ?? "https://hs-abnahme.a-trust.at/aktivierung/v4";

        (var res, var startUrl) = await GenerateAndUploadIdentRecord(certpath, 
            certpwd, 
            identRecordUrl);
        if (!res)
        {

            Console.WriteLine("error upload ident record");
            return;
        }
        Console.WriteLine("-- Identrecord uploaded");

        var idx = startUrl.LastIndexOf("/api/");
        idx = startUrl.IndexOf("/", idx + 5);
        string baseUrl = startUrl.Substring(0, idx);


        var issueResponse = await IssueRequest(startUrl);
        if (issueResponse is null)
        {
            Console.WriteLine("error, issueResponse is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(issueResponse.startUrl))
        {
            Console.WriteLine("error, issueResponse url is missing");
            return;
        }

        var ticketId = issueResponse.ticketId;
        Console.WriteLine("-- issue started");
        Console.WriteLine("TicketId: " + ticketId);
        Process.Start(new ProcessStartInfo(issueResponse.startUrl) { UseShellExecute = true });
        Console.WriteLine("Continue with Enter");
        Console.ReadKey();

        Console.WriteLine("-- certificate issued");

        //get certificate 
        await GetAndPrintCertificate(baseUrl, ticketId);
        Console.WriteLine("-- certificate printed");

        // start signature            
        for (int i = 0; i < numSignatureCall; i++)
        {

            await PrintSessionInfo(baseUrl, ticketId);
            List<HashDataResponse>? docIds = null;
            (startUrl, docIds) = await StartSignature(baseUrl, ticketId, numHashes);
            if (docIds is null)
            {

                Console.WriteLine("error add hash");
                return;
            }
            if (startUrl is null)
            {
                Console.WriteLine("error start signature");
                return;
            }

            Console.WriteLine("-- signature started");
            Console.WriteLine("TicketId: " + ticketId);
            Process.Start(new ProcessStartInfo(startUrl) { UseShellExecute = true });
            Console.WriteLine("Continue with Enter");
            Console.ReadKey();


            Console.WriteLine("-- signature finished");

            if (!await GetAndPrintSignatures(baseUrl, ticketId))
            {
                return;
            }
        }
        // end signature 


        await PrintSessionInfo(baseUrl, ticketId);
        Console.WriteLine("delete session");
        await RestClient.DeleteAsync($"{baseUrl}/session/{ticketId}");
        return;
    }

    private static async Task<bool> GetAndPrintSignatures(string baseUrl, string? ticketId)
    {
        var respSig = await RestClient.DeleteJsonAsync<GetSignatureResponse>(
           $"{baseUrl}/signature/{ticketId}");

        if (respSig?.signatures is null)
        {
            Console.WriteLine("error missing signatures");
            return false;
        }

        foreach (var entry in respSig.signatures)
        {

            Console.WriteLine($"signaturefor {entry.docId} = {entry.signature}");
        }

        return true;
    }

    private static async Task<(string?, List<HashDataResponse>?)> StartSignature(string baseUrl, string? ticketId, int numHashes)
    {
        StartSignatureRequest req = new StartSignatureRequest()
        {
            errorUrl = "https://hs-abnahme.a-trust.at/error/2",
            redirectUrl = "https://hs-abnahme.a-trust.at/next/2",
            hashes = new List<HashDataRequest>()
        };

        for (int i = 0; i < numHashes; i++)
        {
            // generate random hash
            byte[] randomData = RandomNumberGenerator.GetBytes(32);

            var hashData = new HashDataRequest()
            {
                hash = Convert.ToBase64String(SHA256.HashData(randomData)),
                name = "a.sign Premium AGB",
                uri = "https://www.a-trust.at/docs/agb/a-sign-Premium/a-sign_premium_agb.pdf"
            };

            req.hashes.Add(hashData);
        }



        var respStart = await RestClient.PostJsonAsync<StartSignatureResponse, StartSignatureRequest>(
            $"{baseUrl}/signature/{ticketId}",
            req);

        return (respStart?.startUrl, respStart?.docIds);
    }


    private static async Task GetAndPrintCertificate(string baseUrl, string? ticketId)
    {
        var certdata = await RestClient.GetAsync($"{baseUrl}/Certificate/{ticketId}");
        if (null != certdata)
        {
            Console.WriteLine($"certificate: {Convert.ToBase64String(certdata)}");
        }
        else
        {
            Console.WriteLine($"no cert data");
        }
    }

    private static async Task<IssueResponse?> IssueRequest(string startUrl)
    {
        var initRequest = new IssueRequest()
        {
            errorUrl = "https://hs-abnahme.a-trust.at/error",
            redirectUrl = "https://hs-abnahme.a-trust.at/next",
        };

        return await RestClient.PostJsonAsync<IssueResponse, IssueRequest>(
            startUrl,
            initRequest);
    }

    private static async Task<(bool, string)> GenerateAndUploadIdentRecord(
        string? certpath, 
        string? certpwd,
        string identRecordUrl)
    {
        string sigendRecord = string.Empty;

        if (string.IsNullOrWhiteSpace(certpath) || string.IsNullOrWhiteSpace(certpwd))
        {
            Console.WriteLine("no signing certificate - use saved ident record");

            string path = Path.Combine(AppContext.BaseDirectory, "Resources", "IdentRecord.xml");
            sigendRecord = File.ReadAllText(path);
        }
        else
        {
            var signer = new ATrustIdentRecord.SoftwareSigner();
            signer.LoadFromFile(certpath, certpwd);

            var idr = new IdentRecord()
            {
                GivenName = "XXX_Max",
                FamilyName = "XXX_Musterman",
                DateOfBirthYear = 1976,
                DateOfBirthMonth = 1,
                DateOfBirthDay = 1,
                //DateOfBirth = new DateTime(1967,1,1),  // alternative input
                IdMethod = ATrustIdentRecord.Type.eIdMethod.TrustedDatabase,
                IdType = "REIS",
                IdAuthority = "test authority",
                IdIssueDate = DateTime.Now.AddYears(-1),
                IdNation = "AT",
                IdNumber = "123456789",
                Process = ATrustIdentRecord.Type.eProcess.ShortLived,
            };
            sigendRecord = idr.Sign(signer);
        }

        var cert = await IdentRecordApi.LoadEncryptionCertificate(identRecordUrl);
        var encrypted = IdentRecordEncryption.Encrypt(sigendRecord, cert);
        return await IdentRecordApi.Upload(encrypted, identRecordUrl);
    }

    private static async Task PrintSessionInfo(string baseUrl, string? ticketId)
    {
        if (ticketId is null)
        {
            return;
        }

        var res = await RestClient.GetJsonAsync<SessionInfo>($"{baseUrl}/Session/{ticketId}");

        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(res, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }
}
