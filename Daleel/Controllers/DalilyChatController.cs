using Daleel.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Daleel.Controllers
{
    public class DalilyChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DalilyChatController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GeminiLiveModel = "gemini-3.1-flash-live-preview";
        private const string GeminiTextModel = "gemini-3-flash-preview";
        private const string GeminiLiveEndpoint = "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={0}";
        private const string GeminiTextEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";
        private const string SystemPrompt = "You are the Daleel (دليل) AI Assistant — an intelligent guide representing the Daleel Global Vision e-commerce platform. You help users understand the platform's features, services, pricing, integrations, and capabilities. STRICT RULES: 1) The platform name is ALWAYS 'Daleel' (دليل). NEVER call it 'Dalilek', 'Dalily', or any other variation. 2) You must NEVER mention Google, Gemini, DeepMind, LLMs, or any external creators. If asked who made you, strictly state you were developed by the Daleel technical team. 3) ONLY answer questions using the website context provided below. NEVER invent or hallucinate information. 4) If the user asks something outside the scope of Daleel, politely decline and steer them back to the platform. 5) Answer inquiries professionally, concisely, and focus on helping users understand the e-commerce features. Answer in the same language the user uses.";

        private static string BuildPrompt(string? pageContext)
        {
            if (string.IsNullOrWhiteSpace(pageContext))
                return SystemPrompt;
            return SystemPrompt + "\n\n--- WEBSITE CONTEXT (use this to answer the user) ---\n" + pageContext.Trim();
        }

        public DalilyChatController(IConfiguration configuration, ILogger<DalilyChatController> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // OLD TEXT FLOW: HTTP POST -> Gemini REST API (preserved below as disabled legacy code).

        // TEXT FLOW: HTTP POST -> session-aware FastAPI RAG
        [HttpPost("api/dalily-chat/text")]
        public async Task<IActionResult> TextChat([FromBody] TextChatRequest request)
        {
            if (request?.History == null || request.History.Count == 0)
                return BadRequest(new { error = "Conversation history is required." });

            var userText = request.History
                .LastOrDefault(message => message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                ?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(userText))
                return BadRequest(new { error = "A user message is required." });

            string? sessionId = null;
            if (!string.IsNullOrWhiteSpace(request.SessionId))
            {
                if (Guid.TryParse(request.SessionId, out var parsedSessionId))
                    sessionId = parsedSessionId.ToString();
                else
                    _logger.LogWarning("[TextChat] Ignoring invalid session id and starting a new session.");
            }

            // When the question belongs to one of our pages, the widget opens that page after answering.
            // If the RAG service is unavailable we still send the visitor there instead of failing.
            var target = ChatPageRouter.Resolve(userText);
            IActionResult Fail(IActionResult error) => target == null
                ? error
                : Ok(new { text = RedirectOnlyText(target, userText), navigate = ToNavigateDto(target), sessionId });

            var baseUrl = _configuration["RagApi:BaseUrl"];
            var chatPath = _configuration["RagApi:ChatPath"] ?? "api/v1/index/chat";
            var configuredUserId = _configuration["RagApi:UserId"];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var ragBaseUri) ||
                !Guid.TryParse(configuredUserId, out var userId))
            {
                return Fail(StatusCode(503, new { error = "RAG service is not configured." }));
            }

            var subDomainName = _configuration["RagApi:SubDomainName"];
            var limit = int.TryParse(_configuration["RagApi:Limit"], out var configuredLimit)
                ? Math.Clamp(configuredLimit, 1, 100)
                : 3;

            var payload = new
            {
                text = userText,
                domain_name = _configuration["RagApi:DomainName"] ?? "Daleel",
                sub_domain_name = string.IsNullOrWhiteSpace(subDomainName) ? null : subDomainName,
                user_id = userId.ToString(),
                session_id = sessionId,
                limit
            };

            try
            {
                var client = _httpClientFactory.CreateClient("RagApi");
                var endpoint = new Uri(ragBaseUri, chatPath.TrimStart('/'));
                var json = JsonSerializer.Serialize(payload);
                using var response = await client.PostAsync(
                    endpoint,
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[TextChat] RAG chat API error: {Status} {Body}", response.StatusCode, body);
                    return Fail(StatusCode(502, new { error = "RAG service returned an error." }));
                }

                using var ragResponse = JsonDocument.Parse(body);
                var root = ragResponse.RootElement;

                if (root.TryGetProperty("signal", out var signal) &&
                    !string.Equals(signal.GetString(), "rag_answer_success", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("[TextChat] RAG API returned signal {Signal}", signal.GetString());
                    return Fail(StatusCode(502, new { error = "RAG service could not generate an answer." }));
                }

                if (!root.TryGetProperty("answer", out var answerElement) ||
                    string.IsNullOrWhiteSpace(answerElement.GetString()) ||
                    !root.TryGetProperty("session_id", out var sessionElement) ||
                    !Guid.TryParse(sessionElement.GetString(), out var returnedSessionId))
                {
                    _logger.LogError("[TextChat] RAG API response did not contain an answer and valid session id.");
                    return Fail(StatusCode(502, new { error = "RAG service returned an invalid response." }));
                }

                var messageId = root.TryGetProperty("message_id", out var messageElement)
                    ? messageElement.GetString()
                    : null;

                return Ok(new
                {
                    text = answerElement.GetString(),
                    sessionId = returnedSessionId.ToString(),
                    messageId,
                    navigate = ToNavigateDto(target)
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[TextChat] Could not connect to the RAG chat API");
                return Fail(StatusCode(503, new { error = "RAG service is unavailable." }));
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[TextChat] RAG chat API request timed out");
                return Fail(StatusCode(504, new { error = "RAG service timed out." }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextChat] Error");
                return Fail(StatusCode(500, new { error = "An unexpected error occurred." }));
            }
        }

        [HttpGet("api/dalily-chat/session/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            var baseUrl = _configuration["RagApi:BaseUrl"];
            var sessionPath = _configuration["RagApi:SessionPath"] ?? "api/v1/sessions";
            var configuredUserId = _configuration["RagApi:UserId"];

            if (!Guid.TryParse(sessionId, out var parsedSessionId))
                return BadRequest(new { error = "Invalid session id." });

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var ragBaseUri) ||
                !Guid.TryParse(configuredUserId, out var userId))
            {
                return StatusCode(503, new { error = "RAG service is not configured." });
            }

            try
            {
                var relativeUrl = $"{sessionPath.Trim('/')}/{parsedSessionId}?user_id={Uri.EscapeDataString(userId.ToString())}&page=1&page_size=50";
                var endpoint = new Uri(ragBaseUri, relativeUrl);
                var client = _httpClientFactory.CreateClient("RagApi");
                using var response = await client.GetAsync(endpoint);
                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                    return NotFound(new { error = "Chat session was not found." });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[GetSession] RAG API error: {Status} {Body}", response.StatusCode, body);
                    return StatusCode(502, new { error = "RAG service returned an error." });
                }

                using var sessionResponse = JsonDocument.Parse(body);
                var root = sessionResponse.RootElement;
                var messages = new List<object>();
                if (root.TryGetProperty("messages", out var messageArray) &&
                    messageArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var message in messageArray.EnumerateArray())
                    {
                        messages.Add(new
                        {
                            messageId = message.TryGetProperty("message_id", out var messageId) ? messageId.GetString() : null,
                            role = message.TryGetProperty("role", out var role) ? role.GetString() : string.Empty,
                            content = message.TryGetProperty("content", out var content) ? content.GetString() : string.Empty
                        });
                    }
                }

                return Ok(new
                {
                    sessionId = parsedSessionId.ToString(),
                    title = root.TryGetProperty("title", out var title) ? title.GetString() : null,
                    messages
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[GetSession] Could not connect to the RAG API");
                return StatusCode(503, new { error = "RAG service is unavailable." });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[GetSession] RAG API request timed out");
                return StatusCode(504, new { error = "RAG service timed out." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetSession] Error");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

#if false
        // LEGACY STATELESS RAG ANSWER FLOW — intentionally preserved and disabled.
        [HttpPost("api/dalily-chat/text")]
        public async Task<IActionResult> TextChatWithStatelessRagLegacy([FromBody] TextChatRequest request)
        {
            if (request?.History == null || request.History.Count == 0)
                return BadRequest(new { error = "Conversation history is required." });

            var userText = request.History
                .LastOrDefault(message => message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                ?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(userText))
                return BadRequest(new { error = "A user message is required." });

            var baseUrl = _configuration["RagApi:BaseUrl"];
            var answerPath = _configuration["RagApi:AnswerPath"] ?? "api/v1/index/answer";
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var ragBaseUri))
                return StatusCode(503, new { error = "RAG service is not configured." });

            try
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(userText), "text");
                form.Add(new StringContent(_configuration["RagApi:DomainName"] ?? "Daleel"), "domain_name");
                form.Add(new StringContent(_configuration["RagApi:Limit"] ?? "3"), "limit");
                form.Add(new StringContent(_configuration["RagApi:Language"] ?? "ar"), "language");

                var subDomainName = _configuration["RagApi:SubDomainName"];
                if (!string.IsNullOrWhiteSpace(subDomainName))
                    form.Add(new StringContent(subDomainName), "sub_domain_name");

                var client = _httpClientFactory.CreateClient("RagApi");
                var endpoint = new Uri(ragBaseUri, answerPath.TrimStart('/'));
                var response = await client.PostAsync(endpoint, form);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[TextChat] RAG API error: {Status} {Body}", response.StatusCode, body);
                    return StatusCode(502, new { error = "RAG service returned an error." });
                }

                using var ragResponse = JsonDocument.Parse(body);
                var root = ragResponse.RootElement;

                if (root.TryGetProperty("signal", out var signal) &&
                    !string.Equals(signal.GetString(), "rag_answer_success", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("[TextChat] RAG API returned signal {Signal}", signal.GetString());
                    return StatusCode(502, new { error = "RAG service could not generate an answer." });
                }

                if (!root.TryGetProperty("answer", out var answerElement) ||
                    string.IsNullOrWhiteSpace(answerElement.GetString()))
                {
                    _logger.LogError("[TextChat] RAG API response did not contain an answer.");
                    return StatusCode(502, new { error = "RAG service returned an empty answer." });
                }

                return Ok(new { text = answerElement.GetString() });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[TextChat] Could not connect to the RAG API");
                return StatusCode(503, new { error = "RAG service is unavailable." });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[TextChat] RAG API request timed out");
                return StatusCode(504, new { error = "RAG service timed out." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextChat] Error");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
#endif

        // PAGE ROUTING: used by the voice flow, which gets the visitor's words as a transcript.
        [HttpPost("api/dalily-chat/route")]
        public IActionResult RoutePage([FromBody] RouteChatRequest request)
        {
            var target = ChatPageRouter.Resolve(request?.Text);
            return Ok(new { navigate = ToNavigateDto(target) });
        }

        private static object? ToNavigateDto(ChatPageTarget? target) => target == null
            ? null
            : new { key = target.Key, url = target.Url, titleAr = target.TitleAr, titleEn = target.TitleEn };

        private static string RedirectOnlyText(ChatPageTarget target, string question)
        {
            var isArabic = question.Any(c => c >= '\u0600' && c <= '\u06FF');
            return isArabic
                ? $"ستجد كل التفاصيل في صفحة **{target.TitleAr}** — سأنقلك إليها الآن."
                : $"You'll find everything about this on the **{target.TitleEn}** page — taking you there now.";
        }

#if false
        // LEGACY GEMINI TEXT FLOW — intentionally preserved and disabled.
        // Do not remove: this can be re-enabled if a Gemini text fallback is needed later.
        [HttpPost("api/dalily-chat/text")]
        public async Task<IActionResult> TextChat([FromBody] TextChatRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(503, new { error = "AI service not configured." });

            if (request?.History == null || request.History.Count == 0)
                return BadRequest(new { error = "Conversation history is required." });

            var effectivePrompt = BuildPrompt(request.PageContext);

            try
            {
                var url = string.Format(GeminiTextEndpoint, GeminiTextModel, apiKey);
                var payload = new
                {
                    contents = request.History.Select(m => new
                    {
                        role = m.Role,
                        parts = new[] { new { text = m.Text } }
                    }).ToArray(),
                    systemInstruction = new { parts = new[] { new { text = effectivePrompt } } },
                    generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
                };

                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                var resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[TextChat] Gemini error: {Status} {Body}", resp.StatusCode, body);
                    return StatusCode(502, new { error = "AI service returned an error.", detail = body });
                }

                var geminiResp = JsonSerializer.Deserialize<JsonElement>(body);
                var text = "";
                if (geminiResp.TryGetProperty("candidates", out var cands))
                {
                    var first = cands.EnumerateArray().FirstOrDefault();
                    if (first.TryGetProperty("content", out var c) && c.TryGetProperty("parts", out var p))
                        foreach (var part in p.EnumerateArray())
                            if (part.TryGetProperty("text", out var t)) text += t.GetString();
                }
                return Ok(new { text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextChat] Error");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ── VOICE FLOW: WebSocket → Gemini Live API (AUDIO only) ──

#endif

        // VOICE FLOW remains on Gemini Live and is unchanged.
        [Route("/ws/dalily-chat")]
        public async Task VoiceWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest) { HttpContext.Response.StatusCode = 400; return; }
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) { HttpContext.Response.StatusCode = 503; return; }

            using var clientWs = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await VoiceProxy(clientWs, apiKey);
        }

        private async Task VoiceProxy(WebSocket clientWs, string apiKey)
        {
            using var geminiWs = new ClientWebSocket();
            var cts = new CancellationTokenSource();
            try
            {
                // ═══════════════════════════════════════════════════════════
                // STEP 0: Read init_context from client (first message)
                // ═══════════════════════════════════════════════════════════
                string? pageContext = null;
                var initBuf = new byte[1024 * 32];
                var initResult = await clientWs.ReceiveAsync(new ArraySegment<byte>(initBuf), cts.Token);
                if (initResult.MessageType == WebSocketMessageType.Text)
                {
                    var initMsg = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(initBuf, 0, initResult.Count));
                    if (initMsg.TryGetProperty("type", out var tp) && tp.GetString() == "init_context"
                        && initMsg.TryGetProperty("context", out var ctx))
                    {
                        pageContext = ctx.GetString();
                        _logger.LogInformation("[DIAG] Received init_context ({Len} chars)", pageContext?.Length ?? 0);
                    }
                }

                var geminiUri = string.Format(GeminiLiveEndpoint, apiKey);
                _logger.LogWarning("[DIAG] Connecting to Gemini Live at: {Uri}", geminiUri.Replace(apiKey, "***"));
                await geminiWs.ConnectAsync(new Uri(geminiUri), cts.Token);
                _logger.LogWarning("[DIAG] ✅ Connected to Gemini Live. State: {S}", geminiWs.State);

                // ═══════════════════════════════════════════════════════════
                // SETUP MESSAGE
                // ═══════════════════════════════════════════════════════════
                var setupPayload = new
                {
                    setup = new
                    {
                        model = $"models/{GeminiLiveModel}",
                        generationConfig = new
                        {
                            responseModalities = new[] { "AUDIO" },
                            speechConfig = new
                            {
                                voiceConfig = new
                                {
                                    prebuiltVoiceConfig = new
                                    {
                                        voiceName = "Aoede"
                                    }
                                }
                            }
                        },
                        systemInstruction = new
                        {
                            parts = new[] { new { text = BuildPrompt(pageContext) } }
                        }
                    }
                };

                var setupJson = JsonSerializer.Serialize(setupPayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });
                _logger.LogWarning("[DIAG] Sending Setup JSON:\n{Json}", setupJson);

                await SendJson(geminiWs, setupPayload, cts.Token);
                _logger.LogWarning("[DIAG] ✅ Setup sent. Gemini WS State: {S}", geminiWs.State);

                // Wait for setup response
                _logger.LogWarning("[DIAG] Waiting for Gemini setup response...");
                var setupResp = await ReceiveJson(geminiWs, cts.Token);

                if (setupResp == null)
                {
                    _logger.LogError("[DIAG] ❌ Setup response was NULL. Gemini WS State: {S}, CloseStatus: {CS}",
                        geminiWs.State, geminiWs.CloseStatus);
                    await SendError(clientWs, "Voice AI did not respond.");
                    return;
                }

                var respText = setupResp.RootElement.ToString();
                _logger.LogWarning("[DIAG] ✅ Gemini setup response:\n{R}", respText);

                if (setupResp.RootElement.TryGetProperty("error", out var errProp))
                {
                    _logger.LogError("[DIAG] ❌ Gemini returned ERROR: {E}", errProp.ToString());
                    await SendError(clientWs, $"Gemini error: {errProp}");
                    return;
                }

                // Notify frontend
                await SendText(clientWs, JsonSerializer.Serialize(new { type = "voiceReady" }), cts.Token);
                _logger.LogWarning("[DIAG] ✅ Sent voiceReady to client. Starting relay loops.");

                var t1 = AudioRelay(clientWs, geminiWs, cts);
                var t2 = GeminiRelay(geminiWs, clientWs, cts);
                var completed = await Task.WhenAny(t1, t2);
                _logger.LogWarning("[DIAG] Relay loop exited: {Loop}", completed == t1 ? "AudioRelay" : "GeminiRelay");
                cts.Cancel();
                try { await Task.WhenAll(t1, t2); } catch (OperationCanceledException) { }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "[DIAG] ❌ WebSocket error. Gemini: {GS}, Client: {CS}", geminiWs.State, clientWs.State);
                await SendError(clientWs, "Voice connection failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DIAG] ❌ Unexpected error");
                await SendError(clientWs, "Voice connection failed.");
            }
            finally
            {
                _logger.LogWarning("[DIAG] Cleanup. Client: {C}, Gemini: {G}", clientWs.State, geminiWs.State);
                if (clientWs.State == WebSocketState.Open) try { await clientWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
                if (geminiWs.State == WebSocketState.Open) try { await geminiWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
            }
        }

        private async Task AudioRelay(WebSocket client, ClientWebSocket gemini, CancellationTokenSource cts)
        {
            var buf = new byte[1024 * 16];
            try
            {
                while (!cts.Token.IsCancellationRequested && client.State == WebSocketState.Open && gemini.State == WebSocketState.Open)
                {
                    var r = await client.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                    if (r.CloseStatus.HasValue) break;
                    if (r.MessageType != WebSocketMessageType.Text) continue;
                    var msg = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(buf, 0, r.Count));
                    if (msg.TryGetProperty("type", out var tp) && tp.GetString() == "audio" && msg.TryGetProperty("data", out var d))
                        await SendJson(gemini, new { realtimeInput = new { audio = new { data = d.GetString(), mimeType = "audio/pcm;rate=16000" } } }, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }

        private async Task GeminiRelay(ClientWebSocket gemini, WebSocket client, CancellationTokenSource cts)
        {
            var buf = new byte[1024 * 64];
            try
            {
                while (!cts.Token.IsCancellationRequested && gemini.State == WebSocketState.Open && client.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult r;
                    do { r = await gemini.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token); ms.Write(buf, 0, r.Count); } while (!r.EndOfMessage);
                    if (r.CloseStatus.HasValue) break;
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    var srv = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(ms.ToArray()));
                    if (srv.TryGetProperty("setupComplete", out _)) continue;
                    if (!srv.TryGetProperty("serverContent", out var sc)) continue;

                    if (sc.TryGetProperty("modelTurn", out var mt) && mt.TryGetProperty("parts", out var parts))
                        foreach (var part in parts.EnumerateArray())
                            if (part.TryGetProperty("inlineData", out var id))
                                await SendText(client, JsonSerializer.Serialize(new { type = "audio", data = id.GetProperty("data").GetString(), mimeType = id.TryGetProperty("mimeType", out var m) ? m.GetString() : "audio/pcm;rate=24000" }), cts.Token);

                    if (sc.TryGetProperty("outputTranscription", out var ot) && ot.TryGetProperty("text", out var otT))
                        await SendText(client, JsonSerializer.Serialize(new { type = "voiceTranscript", sender = "ai", text = otT.GetString() }), cts.Token);
                    if (sc.TryGetProperty("inputTranscription", out var it) && it.TryGetProperty("text", out var itT))
                        await SendText(client, JsonSerializer.Serialize(new { type = "voiceTranscript", sender = "user", text = itT.GetString() }), cts.Token);
                    if (sc.TryGetProperty("turnComplete", out var tc) && tc.GetBoolean())
                        await SendText(client, JsonSerializer.Serialize(new { type = "turnComplete" }), cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }

        // Helpers
        static async Task SendJson(ClientWebSocket ws, object p, CancellationToken ct) { var b = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })); await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, ct); }
        static async Task<JsonDocument?> ReceiveJson(ClientWebSocket ws, CancellationToken ct)
        {
            var buf = new byte[1024 * 16];
            using var ms = new MemoryStream();
            WebSocketReceiveResult r;
            do
            {
                r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                ms.Write(buf, 0, r.Count);
            } while (!r.EndOfMessage);

            if (r.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"[DIAG] ❌ Gemini sent CLOSE frame. Status: {r.CloseStatus}, Desc: {r.CloseStatusDescription}");
                return null;
            }
            if (r.MessageType == WebSocketMessageType.Text || r.MessageType == WebSocketMessageType.Binary)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return await JsonDocument.ParseAsync(ms, cancellationToken: ct);
            }
            Console.WriteLine($"[DIAG] ⚠ Unexpected message type from Gemini: {r.MessageType}");
            return null;
        }
        static async Task SendText(WebSocket ws, string m, CancellationToken ct) { if (ws.State != WebSocketState.Open) return; await ws.SendAsync(Encoding.UTF8.GetBytes(m), WebSocketMessageType.Text, true, ct); }
        static async Task SendError(WebSocket ws, string e) { try { if (ws.State == WebSocketState.Open) await ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "error", text = e })), WebSocketMessageType.Text, true, CancellationToken.None); } catch { } }
    }

    public class TextChatRequest
    {
        [JsonPropertyName("history")] public List<ChatMessage> History { get; set; } = new();
        [JsonPropertyName("pageContext")] public string? PageContext { get; set; }
        [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    }
    public class RouteChatRequest
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }
    public class ChatMessage { [JsonPropertyName("role")] public string Role { get; set; } = "user"; [JsonPropertyName("text")] public string Text { get; set; } = ""; }
}
