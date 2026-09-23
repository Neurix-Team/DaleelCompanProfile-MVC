using Daleel.Services;

namespace Daleel.Tests.Web
{
    public class ChatPageRouterTests
    {
        [Theory]
        [InlineData("قولي السيستم ده بيعمل اي", "/about")]
        [InlineData("مين انتوا", "/about")]
        [InlineData("مَن نحن؟", "/about")]
        [InlineData("What is Daleel?", "/about")]
        [InlineData("ايه الخدمات اللي بتقدموها", "/platforms")]
        [InlineData("What services do you offer?", "/platforms")]
        [InlineData("الباقة بكام؟", "/Home/Pricing")]
        [InlineData("How much does it cost?", "/Home/Pricing")]
        [InlineData("عاوز رقم تليفون اكلمكم عليه", "/contact")]
        [InlineData("How can I contact you?", "/contact")]
        [InlineData("عايز احجز ديمو", "/Home/ScheduleDemo")]
        [InlineData("إزاي بتحموا خصوصية بياناتي", "/trust")]
        [InlineData("فين آخر الأخبار", "/blog")]
        [InlineData("عاوز ابقى شريك معاكم", "/partnerregistration")]
        [InlineData("وديني المتجر", "/shop")]
        public void Resolve_QuestionAboutAPage_ReturnsThatPage(string question, string expectedUrl)
        {
            ChatPageRouter.Resolve(question)!.Url.Should().Be(expectedUrl);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Hello")]
        [InlineData("ازيك")]
        [InlineData("شكرا جدا")]
        [InlineData("Can you explain that again?")] // "explain" must not match "plan"
        public void Resolve_GeneralMessage_ReturnsNull(string question)
        {
            ChatPageRouter.Resolve(question).Should().BeNull();
        }

        [Fact]
        public void Resolve_PricingAndAboutWords_PrefersTheStrongerPricingMatch()
        {
            ChatPageRouter.Resolve("السيستم ده سعره كام؟ وأسعار الباقات إيه؟")!.Key.Should().Be("pricing");
        }
    }
}
