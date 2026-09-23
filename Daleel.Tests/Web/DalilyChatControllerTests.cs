using Daleel.Controllers;
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
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);

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
        public async Task TextChat_EmptyHistory_Returns400BadRequest()
        {
            // Arrange
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Gemini:ApiKey", "valid-test-key-12345" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);

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

        // Legacy stateless multipart contract test, preserved for the disabled legacy flow.
#if false
        [Fact]
        public async Task TextChat_ValidMessage_SendsMultipartRequestAndReturnsRagAnswer()
        {
            // Arrange
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "RagApi:BaseUrl", "http://localhost:8080/" },
                { "RagApi:AnswerPath", "api/v1/index/answer" },
                { "RagApi:DomainName", "Daleel" },
                { "RagApi:Limit", "3" },
                { "RagApi:Language", "ar" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "signal": "rag_answer_success",
                      "answer": "أنا مساعد دليل"
                    }
                    """, Encoding.UTF8, "application/json")
            });
            var client = new HttpClient(handler);
            _mockHttpClientFactory.Setup(factory => factory.CreateClient("RagApi")).Returns(client);
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);
            var request = new TextChatRequest
            {
                History = new List<ChatMessage>
                {
                    new() { Role = "user", Text = "مين انتوا" }
                }
            };

            // Act
            var result = await controller.TextChat(request);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            using var responseJson = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            responseJson.RootElement.GetProperty("text").GetString().Should().Be("أنا مساعد دليل");
            handler.RequestUri.Should().Be("http://localhost:8080/api/v1/index/answer");
            handler.ContentType.Should().StartWith("multipart/form-data;");
            handler.Body.Should().Contain("name=text").And.Contain("مين انتوا");
            handler.Body.Should().Contain("name=domain_name").And.Contain("Daleel");
            handler.Body.Should().Contain("name=limit").And.Contain("3");
            handler.Body.Should().Contain("name=language").And.Contain("ar");
        }

#endif

        [Fact]
        public async Task TextChat_ValidMessage_SendsJsonRequestAndReturnsSessionAwareRagAnswer()
        {
            var config = BuildRagConfiguration();
            var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "signal": "rag_answer_success",
                      "answer": "Daleel answer",
                      "session_id": "c1c8ba10-2e50-4536-a280-7d2c1b64e4ad",
                      "message_id": "7a1b8e5c-2039-461f-a6ac-9036fe2cdea4"
                    }
                    """, Encoding.UTF8, "application/json")
            });
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient("RagApi"))
                .Returns(new HttpClient(handler));
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);

            var result = await controller.TextChat(new TextChatRequest
            {
                History = new List<ChatMessage> { new() { Role = "user", Text = "Who are you?" } }
            });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            using var responseJson = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            responseJson.RootElement.GetProperty("text").GetString().Should().Be("Daleel answer");
            responseJson.RootElement.GetProperty("sessionId").GetString()
                .Should().Be("c1c8ba10-2e50-4536-a280-7d2c1b64e4ad");
            handler.RequestUri.Should().Be("http://localhost:8080/api/v1/index/chat");
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
            var config = BuildRagConfiguration();
            var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "signal": "rag_answer_success",
                      "answer": "Continued answer",
                      "session_id": "c1c8ba10-2e50-4536-a280-7d2c1b64e4ad",
                      "message_id": "7a1b8e5c-2039-461f-a6ac-9036fe2cdea4"
                    }
                    """, Encoding.UTF8, "application/json")
            });
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient("RagApi"))
                .Returns(new HttpClient(handler));
            var controller = new DalilyChatController(config, _mockLogger.Object, _mockHttpClientFactory.Object);

            await controller.TextChat(new TextChatRequest
            {
                SessionId = "c1c8ba10-2e50-4536-a280-7d2c1b64e4ad",
                History = new List<ChatMessage> { new() { Role = "user", Text = "Continue" } }
            });

            using var requestJson = JsonDocument.Parse(handler.Body);
            requestJson.RootElement.GetProperty("session_id").GetString()
                .Should().Be("c1c8ba10-2e50-4536-a280-7d2c1b64e4ad");
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

        private static IConfiguration BuildRagConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                { "RagApi:BaseUrl", "http://localhost:8080/" },
                { "RagApi:ChatPath", "api/v1/index/chat" },
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
