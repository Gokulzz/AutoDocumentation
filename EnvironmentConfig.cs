using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDocumentation
{
    public static class EnvironmentConfig
    {
        public static string GetApiKey()
        {
            return Environment.GetEnvironmentVariable("OpenAI_ApiKey")
                   ?? throw new InvalidOperationException("OpenAI API key not found in environment variables.");
        }

        public static string GetApiUrl()
        {
            return Environment.GetEnvironmentVariable("OpenAI_ApiUrl")
                   ?? throw new InvalidOperationException("OpenAI API URL not found in environment variables.");
        }
    }
}
