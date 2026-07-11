//using System.Text;
//using System.Text.Json;

//namespace SmartBabySitter.Services
//{
//    public class GeminiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _configuration;

//        public GeminiService(HttpClient httpClient, IConfiguration configuration)
//        {
//            _httpClient = httpClient;
//            _configuration = configuration;
//        }

//        public async Task<string> GetRecommendation(string prompt)
//        {
//            var apiKey = _configuration["Gemini:ApiKey"];

//            var url =
//                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";


//            var requestBody = new
//            {
//                contents = new[]
//                {
//            new
//            {
//                parts = new[]
//                {
//                    new
//                    {
//                        text = prompt
//                    }
//                }
//            }
//        }
//            };


//            var json = JsonSerializer.Serialize(requestBody);


//            var content = new StringContent(
//                json,
//                Encoding.UTF8,
//                "application/json"
//            );


//            var response = await _httpClient.PostAsync(url, content);


//            var responseText = await response.Content.ReadAsStringAsync();


//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception(responseText);
//            }


//            using var doc = JsonDocument.Parse(responseText);


//            return doc.RootElement
//                .GetProperty("candidates")[0]
//                .GetProperty("content")
//                .GetProperty("parts")[0]
//                .GetProperty("text")
//                .GetString();
//        }
//    }
//}