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
