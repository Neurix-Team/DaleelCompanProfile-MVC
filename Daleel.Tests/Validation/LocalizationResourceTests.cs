using System.Globalization;
using System.Resources;

namespace Daleel.Tests.Validation
{
    public class LocalizationResourceTests
    {
        private static readonly ResourceManager Resources =
            new("Daleel.Resources.SharedResource", typeof(SharedResource).Assembly);

        [Theory]
        [InlineData("CmsEditLink")]
        [InlineData("CmsInactive")]
        [InlineData("CmsNewLink")]
        [InlineData("CmsNoTeamMembersFound")]
        [InlineData("CmsSave")]
        [InlineData("CmsUpdate")]
        [InlineData("CrmEditTask")]
        [InlineData("CrmLeadAssignedTo")]
        [InlineData("CrmLeadCompany")]
        [InlineData("CrmLeadName")]
        [InlineData("CrmLeadSource")]
        [InlineData("CrmLeadStatus")]
        [InlineData("SharedRoleAdministrator")]
        [InlineData("SharedRoleStaff")]
        [InlineData("SharedRoleUser")]
        public void CrmAndCmsViewKey_HasEnglishAndArabicTranslations(string key)
        {
            foreach (var cultureName in new[] { "en", "ar" })
            {
                var value = Resources.GetString(key, CultureInfo.GetCultureInfo(cultureName));

                value.Should().NotBeNullOrWhiteSpace();
                value.Should().NotBe(key);
            }
        }

        [Fact]
        public void BrandIndexIntro_IsAccurateForAnyNumberOfBrands()
        {
            Resources.GetString("CmsBrandsIndexIntro", CultureInfo.GetCultureInfo("en"))
                .Should().Be("Manage Daleel brand profiles and their public identity data.");
            Resources.GetString("CmsBrandsIndexIntro", CultureInfo.GetCultureInfo("ar"))
                .Should().Be("إدارة الملفات التعريفية للعلامات التجارية التابعة لدليل وبيانات هويتها العامة.");
        }
    }
}
