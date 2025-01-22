using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AutoDocumentation
{
    public static class OpenAIHelper
    {
        private static readonly string ApiKey;
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
         static OpenAIHelper()
        {
            ApiKey= EnvironmentConfig.GetApiKey();
        }
       

        public  static async Task<(string documentation, string LineByLineComment)> GenerateDocumentationAsync(string code)
        {

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var requestBody = new
                {
                    model = "gpt-4",
                    messages = new[]
                   {
                        new { role = "system", content = "You are a coding assistant." },
                        new { role = "user", content = $"Generate a summary of what the following C# code does, followed by detailed line-by-line comments. Do not include any unnecessary text.\n\nSummary:\n\nLine-by-Line Comments:\n\n{code}" }
                    },
                    max_tokens = 1000
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(ApiUrl, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        // Extract and return the response text
                        string full_Response = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? string.Empty;
                        string[] sections = full_Response.Split(new[] { "Summary:", "Line-by-Line Comments:" }, StringSplitOptions.RemoveEmptyEntries);
                        string fullDocumentation = sections.Length > 0 ? sections[0].Trim() : string.Empty;
                        string lineByLineComments = sections.Length > 1 ? sections[1].Trim() : string.Empty;

                        return (fullDocumentation, lineByLineComments);
                    }
                    else
                    {
                        throw new HttpRequestException($"OpenAI API Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP Request failed: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
