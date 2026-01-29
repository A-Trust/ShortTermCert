using System.Net;
using System.Security.Authentication;
using System.Text;

namespace TestClient;

public static class RestClient
{

    private static SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15), // Recreate every 15 minutes to resolve DNS every 15 min. 
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        Proxy = null,
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions()
        {
            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
            RemoteCertificateValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; },
        },
        MaxConnectionsPerServer = Int32.MaxValue,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        DefaultProxyCredentials = null,
    };
    private static readonly HttpClient httpClient = new HttpClient(socketsHttpHandler);



    public async static Task<T?> PostJsonAsync<T, U>(string postUrl, U? request)
        where T : class
        where U : class
    {
        try
        {
            string? content = null;
            if (null != request)
            {
                content = System.Text.Json.JsonSerializer.Serialize(request);
            }

            HttpResponseMessage? response = null;
            using (var m = new HttpRequestMessage(HttpMethod.Post, postUrl))
            {

                if (string.IsNullOrWhiteSpace(content))
                {
                    response = await httpClient.SendAsync(m);
                }
                else
                {
                    using (HttpContent c = new StringContent(content, Encoding.UTF8, "application/json"))
                    {
                        m.Content = c;
                        response = await httpClient.SendAsync(m);
                    }
                }
            }

            if (null == response)
            {
                Console.WriteLine("PostJsonAsync: error no response");
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(stream);

            }
            else
            {
                Console.WriteLine($"PostJsonAsync: postUrl ={postUrl}");
                Console.WriteLine($"PostJsonAsync: invalid statuscode={response.StatusCode}");
            }

        }
        catch (TaskCanceledException ex)
        {
            // NET Core and .NET 5 and later only: The request failed due to timeout.
            Console.WriteLine("PostJsonAsync: Protocol Timeout" + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("PostJsonAsync: exception" + ex.Message);
        }

        return null;
    }


    public static async Task<T?> GetJsonAsync<T>(string getUrl)
        where T : class
    {
        try
        {
            //HttpResponseMessage response = await httpClient.GetAsync(getUrl);

            HttpResponseMessage? response = null;
            using (var m = new HttpRequestMessage(HttpMethod.Get, getUrl))
            {
                response = await httpClient.SendAsync(m);
            }

            if (null == response)
            {
                Console.WriteLine("GetJsonAsync: error no response");
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(stream);
            }
            else
            {
                Console.WriteLine($"GetJsonAsync: invalid statuscode={response.StatusCode}");
            }
        }
        catch (TaskCanceledException ex)
        {
            // NET Core and .NET 5 and later only: The request failed due to timeout.
            Console.WriteLine("GetJsonAsync: Protocol Timeout", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetJsonAsync: exception", ex);
        }

        return null;
    }


    public static async Task<byte[]?> GetAsync(string getUrl)
    {
        try
        {
            //HttpResponseMessage response = await httpClient.GetAsync(getUrl);

            HttpResponseMessage? response = null;
            using (var m = new HttpRequestMessage(HttpMethod.Get, getUrl))
            {
                response = await httpClient.SendAsync(m);
            }

            if (null == response)
            {
                Console.WriteLine($"GetAsync: error no response");
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                Console.WriteLine($"GetAsync: invalid statuscode={response.StatusCode}");
            }
        }
        catch (TaskCanceledException ex)
        {
            // NET Core and .NET 5 and later only: The request failed due to timeout.
            Console.WriteLine($"GetAsync: Protocol Timeout", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAsync: exception", ex);
        }

        return null;
    }


    public static async Task<T?> DeleteJsonAsync<T>(string url)
        where T : class
    {
        try
        {
            HttpResponseMessage? response = null;
            using (var m = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                response = await httpClient.SendAsync(m);
            }

            if (null == response)
            {
                Console.WriteLine("DeleteJsonAsync: error no response");
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(stream);
            }
            else
            {
                Console.WriteLine($"DeleteJsonAsync: invalid statuscode={response.StatusCode}");
                return null;
            }
        }
        catch (TaskCanceledException ex)
        {
            // NET Core and .NET 5 and later only: The request failed due to timeout.
            Console.WriteLine("DeleteJsonAsync: Protocol Timeout", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("DeleteJsonAsync: exception", ex);
        }

        return null;
    }

    public static async Task<bool> DeleteAsync(string url)
    {
        try
        {
            HttpResponseMessage? response = null;
            using (var m = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                response = await httpClient.SendAsync(m);
            }

            if (null == response)
            {
                Console.WriteLine("DeleteAsync: error no response");
            }
            else if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Console.WriteLine($"DeleteAsync: invalid statuscode={response.StatusCode}");
            }
        }
        catch (TaskCanceledException ex)
        {
            // NET Core and .NET 5 and later only: The request failed due to timeout.
            Console.WriteLine("DeleteAsync: Protocol Timeout", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("DeleteAsync: exception", ex);
        }

        return false;
    }
}
