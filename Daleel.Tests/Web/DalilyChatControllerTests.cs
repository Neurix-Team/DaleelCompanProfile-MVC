using Daleel.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Daleel.Tests.Web
{
    public class DalilyChatControllerTests
    {
        private const string SessionId = "c1c8ba10-2e50-4536-a280-7d2c1b64e4ad";
        private const string MessageId = "7a1b8e5c-2039-461f-a6ac-9036fe2cdea4";

        private readonly Mock<ILogger<DalilyChatController>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        public DalilyChatControllerTests()
        {
            _mockLogger = new Mock<ILogger<DalilyChatController>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        }

        [Fact]
        public async Task TextChat_MissingRagConfiguration_Returns503ServiceUnavailable()
        {
            // Arrange
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "RagApi:BaseUrl", null }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var controller = CreateController(config);

            var request = new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "Hello" } }
            };

            // Act
            var result = await controller.TextChat(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(503);
        }

        [Fact]
        public async Task TextChat_RagUnavailableButQuestionMatchesPage_StreamsNavigationTarget()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "RagApi:BaseUrl", null } })
                .Build();
            var controller = CreateController(config);

            var request = new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "قولي السيستم ده بيعمل اي" } }
            };

            // Act
            var result = await controller.TextChat(request);

            // Assert
            result.Should().BeOfType<EmptyResult>();
            var events = ReadEvents(controller);
            events.Select(e => e.Name).Should().Equal("token", "done");
            events[0].Data.GetProperty("text").GetString().Should().Contain("من نحن");
            events[1].Data.GetProperty("navigate").GetProperty("url").GetString().Should().Be("/about");
        }

        [Fact]
        public async Task TextChat_EmptyHistory_Returns400BadRequest()
        {
            // Arrange
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var controller = CreateController(config);

            var request = new TextChatRequest
            {
                History = new List<ChatMessage>() // Empty history
            };

            // Act
            var result = await controller.TextChat(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task TextChat_ValidMessage_SendsJsonRequestAndStreamsAnswer()
        {
            var handler = new RecordingHttpMessageHandler(SseResponse(
                ("session", $$"""{"session_id": "{{SessionId}}"}"""),
                ("token", """{"text": "Daleel is a platform."}"""),
                ("token", """{"text": " It helps stores."}"""),
                ("done", $$"""{"signal": "rag_answer_success", "route": "rag", "retrieval_used": true, "session_id": "{{SessionId}}", "message_id": "{{MessageId}}", "search_query": "who", "answer": "Daleel is a platform."}""")));
            var controller = CreateController(BuildRagConfiguration(), handler);

            var result = await controller.TextChat(new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "Who are you?" } }
            });

            result.Should().BeOfType<EmptyResult>();
            controller.Response.ContentType.Should().StartWith("text/event-stream");

            var events = ReadEvents(controller);
            events.Select(e => e.Name).Should().Equal("session", "token", "token", "done");
            events[0].Data.GetProperty("sessionId").GetString().Should().Be(SessionId);
            string.Concat(events.Where(e => e.Name == "token").Select(e => e.Data.GetProperty("text").GetString()))
                .Should().Be("Daleel is a platform. It helps stores.");
            events[3].Data.GetProperty("sessionId").GetString().Should().Be(SessionId);
            events[3].Data.GetProperty("messageId").GetString().Should().Be(MessageId);
            // the stored answer, trimmed by the RAG service when the output cap cut the last sentence
            events[3].Data.GetProperty("answer").GetString().Should().Be("Daleel is a platform.");
            events[3].Data.TryGetProperty("search_query", out _).Should().BeFalse();

            handler.RequestUri.Should().Be("http://localhost:8080/api/v1/index/chat/stream");
            handler.ContentType.Should().Be("application/json; charset=utf-8");

            using var requestJson = JsonDocument.Parse(handler.Body);
            requestJson.RootElement.GetProperty("text").GetString().Should().Be("Who are you?");
            requestJson.RootElement.GetProperty("domain_name").GetString().Should().Be("Daleel");
            requestJson.RootElement.GetProperty("sub_domain_name").ValueKind.Should().Be(JsonValueKind.Null);
            requestJson.RootElement.GetProperty("user_id").GetString()
                .Should().Be("ac2527d5-e9e7-46cf-a567-7c207a55e6a7");
            requestJson.RootElement.GetProperty("session_id").ValueKind.Should().Be(JsonValueKind.Null);
            requestJson.RootElement.GetProperty("limit").GetInt32().Should().Be(3);
        }

        [Fact]
        public async Task TextChat_ExistingSession_SendsSameSessionId()
        {
            var handler = new RecordingHttpMessageHandler(SseResponse(
                ("session", $$"""{"session_id": "{{SessionId}}"}"""),
                ("token", """{"text": "Continued answer"}"""),
                ("done", $$"""{"signal": "rag_answer_success", "session_id": "{{SessionId}}", "message_id": "{{MessageId}}"}""")));
            var controller = CreateController(BuildRagConfiguration(), handler);

            await controller.TextChat(new TextChatRequest
            {
                SessionId = SessionId,
                History = new List<ChatMessage> { new() { Role = "user", Text = "Continue" } }
            });

            using var requestJson = JsonDocument.Parse(handler.Body);
            requestJson.RootElement.GetProperty("session_id").GetString().Should().Be(SessionId);
        }

        [Fact]
        public async Task TextChat_RagErrorMidStream_KeepsSessionAndStreamsError()
        {
            var handler = new RecordingHttpMessageHandler(SseResponse(
                ("session", $$"""{"session_id": "{{SessionId}}"}"""),
                ("token", """{"text": "Partial answer."}"""),
                ("error", """{"signal": "rag_answer_error"}""")));
            var controller = CreateController(BuildRagConfiguration(), handler);

            await controller.TextChat(new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "Hello" } }
            });

            var events = ReadEvents(controller);
            events.Select(e => e.Name).Should().Equal("session", "token", "error");
            events[0].Data.GetProperty("sessionId").GetString().Should().Be(SessionId);
            events[2].Data.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
            events[2].Data.GetProperty("error").GetString().Should().NotContain("rag_answer_error");
        }

        [Fact]
        public async Task TextChat_StreamEndsWithoutDone_StreamsError()
        {
            var handler = new RecordingHttpMessageHandler(SseResponse(
                ("token", """{"text": "Cut off"}""")));
            var controller = CreateController(BuildRagConfiguration(), handler);

            await controller.TextChat(new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "Hello" } }
            });

            ReadEvents(controller).Select(e => e.Name).Should().Equal("token", "error");
        }

        [Fact]
        public async Task TextChat_RagRejectsRequestBeforeStreaming_Returns502()
        {
            var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"signal": "session_not_found"}""", Encoding.UTF8, "application/json")
            });
            var controller = CreateController(BuildRagConfiguration(), handler);

            var result = await controller.TextChat(new TextChatRequest
            {
                SessionId = SessionId,
                History = new List<ChatMessage> { new() { Role = "user", Text = "Hello" } }
            });

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(502);
        }

        [Fact]
        public async Task GetSession_ValidSession_ReturnsMappedHistory()
        {
            var config = BuildRagConfiguration();
            var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "session_id": "c1c8ba10-2e50-4536-a280-7d2c1b64e4ad",
                      "title": "Daleel",
                      "messages": [
                        { "message_id": "4179b2ef-4475-45ca-a06f-36a5da5c5474", "role": "user", "content": "Hello" },
                        { "message_id": "7a1b8e5c-2039-461f-a6ac-9036fe2cdea4", "role": "assistant", "content": "Welcome" }
                      ]
                    }
                    """, Encoding.UTF8, "application/json")
            });
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient("RagApi"))
                .Returns(new HttpClient(handler));
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);

            var result = await controller.GetSession("c1c8ba10-2e50-4536-a280-7d2c1b64e4ad");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            using var responseJson = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            responseJson.RootElement.GetProperty("messages").GetArrayLength().Should().Be(2);
            responseJson.RootElement.GetProperty("messages")[1].GetProperty("role").GetString()
                .Should().Be("assistant");
            handler.RequestUri.Should().Be(
                "http://localhost:8080/api/v1/sessions/c1c8ba10-2e50-4536-a280-7d2c1b64e4ad?user_id=ac2527d5-e9e7-46cf-a567-7c207a55e6a7&page=1&page_size=50");
        }

        private DalilyChatController CreateController(IConfiguration config, HttpMessageHandler? handler = null)
        {
            if (handler != null)
            {
                _mockHttpClientFactory
                    .Setup(factory => factory.CreateClient("RagApi"))
                    .Returns(new HttpClient(handler));
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            return new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static HttpResponseMessage SseResponse(params (string Name, string Data)[] events)
        {
            var body = string.Concat(events.Select(e => $"event: {e.Name}\ndata: {e.Data}\n\n"));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
            };
        }

        private static List<(string Name, JsonElement Data)> ReadEvents(ControllerBase controller)
        {
            var body = Encoding.UTF8.GetString(((MemoryStream)controller.Response.Body).ToArray());
            return body
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(block =>
                {
                    var lines = block.Split('\n');
                    var name = lines.Single(line => line.StartsWith("event: ")).Substring("event: ".Length);
                    var data = lines.Single(line => line.StartsWith("data: ")).Substring("data: ".Length);
                    return (name, JsonDocument.Parse(data).RootElement.Clone());
                })
                .ToList();
        }

        private static IConfiguration BuildRagConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                { "RagApi:BaseUrl", "http://localhost:8080/" },
                { "RagApi:ChatStreamPath", "api/v1/index/chat/stream" },
                { "RagApi:SessionPath", "api/v1/sessions" },
                { "RagApi:DomainName", "Daleel" },
                { "RagApi:UserId", "ac2527d5-e9e7-46cf-a567-7c207a55e6a7" },
                { "RagApi:Limit", "3" }
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public RecordingHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            public string? RequestUri { get; private set; }
            public string? ContentType { get; private set; }
            public string Body { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri?.ToString();
                ContentType = request.Content?.Headers.ContentType?.ToString();
                Body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return _response;
            }
        }
    }
}
