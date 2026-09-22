using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Daleel.BAL.Services
{
    /// <summary>
    /// Calls the Google Gemini REST API to generate AI chat responses.
    /// Automatically selects between gemini-1.5-pro (complex) and
    /// gemini-1.5-flash (fast) based on the user's prompt characteristics.
    /// </summary>
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string FastModel = "gemini-1.5-flash";
        private const string ProModel = "gemini-1.5-pro";

        private const string SystemInstruction =
            "You are 'Daleel AI', an expert strategic intelligence assistant for the Daleel Analytics platform. " +
            "You specialize in global markets, trade corridors, supply chain risk, economic intelligence, and investment strategy. " +
            "Be concise, data-aware, and professional. Respond in the same language the user writes in.";

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey not configured in appsettings.");
        }

        public async Task<string> GetChatResponseAsync(string prompt, List<ChatMessageDto> history)
        {
            // Model selection heuristic
            bool isComplex = prompt.Length > 100 ||
                             ContainsKeyword(prompt, "analyze", "analysis", "explain", "compare",
                                            "forecast", "strategy", "evaluate", "assess");

            string modelName = isComplex ? ProModel : FastModel;
            string url = $"v1beta/models/{modelName}:generateContent?key={_apiKey}";

            // Build the conversation turns (history + current prompt)
            var contents = new List<object>();
            if (history != null)
            {
                foreach (var msg in history)
                {
                    string role = msg.Role.Equals("ai", StringComparison.OrdinalIgnoreCase) ||
                                  msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase)
                                  ? "model" : "user";

                    contents.Add(new
                    {
                        role,
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Always append the current user message last
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = SystemInstruction } }
                },
                contents
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API error [{response.StatusCode}]: {error}");
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var text = doc.RootElement
                          .GetProperty("candidates")[0]
                          .GetProperty("content")
                          .GetProperty("parts")[0]
                          .GetProperty("text")
                          .GetString();

            return text ?? "I'm sorry, I couldn't generate a response at this time.";
        }

        private static bool ContainsKeyword(string prompt, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (prompt.Contains(kw, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
