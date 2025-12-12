using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

class Program
{
    private static readonly string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

    static async Task Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("❌ ضع مفتاح OPENAI_API_KEY كـ environment variable");
            return;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ImageGenApp \"your image prompt here\"");
            return;
        }

        string prompt = args[0];

        var url = "https://api.openai.com/v1/images/generations";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            prompt = prompt,
            n = 1,
            size = "1024x1024"
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("⏳ Generating image...");

        var response = await client.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("❌ API Error:");
            Console.WriteLine(responseString);
            return;
        }

        var result = JObject.Parse(responseString);

        // Try base64 JSON first
        var b64 = result["data"]?[0]?["b64_json"]?.ToString();

        if (!string.IsNullOrEmpty(b64))
        {
            byte[] bytes = Convert.FromBase64String(b64);
            File.WriteAllBytes("generated.png", bytes);
            Console.WriteLine("✅ Saved: generated.png");
            return;
        }

        // Otherwise maybe URL
        var urlImage = result["data"]?[0]?["url"]?.ToString();

        if (!string.IsNullOrEmpty(urlImage))
        {
            Console.WriteLine("Image URL: " + urlImage);
            var imgBytes = await client.GetByteArrayAsync(urlImage);
            File.WriteAllBytes("generated.png", imgBytes);
            Console.WriteLine("✅ Saved: generated.png");
            return;
        }

        Console.WriteLine("⚠ Unexpected response: " + result.ToString());
    }
}
