using System.Text;
using System.Text.RegularExpressions;

namespace Daleel.Services
{
    /// <summary>A public page the assistant can send the visitor to.</summary>
    public sealed record ChatPageTarget(string Key, string Url, string TitleAr, string TitleEn);

    /// <summary>
    /// Maps a visitor's chat question to the public page that answers it, so the
    /// assistant can open that page automatically. Matching is keyword based and
    /// understands Modern Standard Arabic, Egyptian dialect and English.
    /// </summary>
    public static class ChatPageRouter
    {
        // Below this score the question is too vague to justify leaving the current page.
        private const int MinScore = 2;

        private sealed record Rule(ChatPageTarget Target, (string Phrase, int Weight)[] Keywords);

        private static readonly Rule[] Rules =
        {
            new(new("about", "/about", "من نحن", "About Us"), new[]
            {
                ("من نحن", 4), ("مين انتم", 4), ("مين انتو", 4), ("من انتم", 4), ("عن الشركه", 4), ("عن دليل", 4),
                ("ما هو دليل", 4), ("ايه هو دليل", 4), ("يعني ايه دليل", 4), ("تعريف", 2),
                ("بيعمل", 2), ("بتعمل ايه", 2), ("يعمل ايه", 2), ("وظيفه", 2), ("فكره", 2),
                ("السيستم", 1), ("النظام", 1), ("الشركه", 1), ("الموقع", 1),
                ("رسالتكم", 3), ("رؤيتكم", 3), ("اهدافكم", 3), ("قصتكم", 3), ("فريق", 2), ("خبره", 2),
                ("about", 3), ("who are you", 4), ("what is daleel", 4), ("what does", 2), ("your company", 3),
                ("mission", 3), ("vision", 2), ("story", 2), ("team", 2)
            }),
            new(new("platforms", "/platforms", "المنصات والحلول", "Platforms & Solutions"), new[]
            {
                ("منصات", 4), ("المنصه", 3), ("حلول", 3), ("خدمات", 4), ("خدماتكم", 4), ("منتجات", 4),
                ("بتقدموا ايه", 4), ("تقدمون", 3), ("مميزات", 3), ("امكانيات", 3), ("ذكاء الاعمال", 3),
                ("تحليلات", 2), ("اداره العملاء", 3), ("crm", 3),
                ("platform", 4), ("solution", 3), ("service", 4), ("product", 4), ("feature", 3),
                ("what do you offer", 4), ("analytics", 2)
            }),
            new(new("pricing", "/Home/Pricing", "الأسعار والباقات", "Pricing"), new[]
            {
                ("سعر", 4), ("اسعار", 4), ("بكام", 4), ("تكلفه", 4), ("تكاليف", 4), ("باقه", 4), ("باقات", 4),
                ("اشتراك", 3), ("خطه", 2), ("فلوس", 3), ("مجاني", 2),
                ("price", 4), ("pricing", 4), ("cost", 4), ("how much", 4), ("plan", 3), ("subscription", 3), ("free trial", 3)
            }),
            new(new("contact", "/contact", "اتصل بنا", "Contact Us"), new[]
            {
                ("تواصل", 4), ("اتصل", 4), ("اكلمكم", 4), ("كلمكم", 4), ("نكلمكم", 4), ("رقم", 3), ("ارقام", 3), ("تليفون", 4),
                ("هاتف", 4), ("موبايل", 3), ("واتساب", 4), ("ايميل", 4), ("بريد", 3), ("عنوان", 3), ("مكانكم", 4),
                ("مقركم", 4), ("فين", 1), ("مكتب", 2),
                ("contact", 4), ("phone", 4), ("call", 2), ("email", 4), ("address", 3), ("location", 3),
                ("reach you", 4), ("get in touch", 4), ("office", 2)
            }),
            new(new("demo", "/Home/ScheduleDemo", "حجز عرض توضيحي", "Schedule a Demo"), new[]
            {
                ("ديمو", 5), ("عرض توضيحي", 5), ("احجز", 4), ("حجز", 3), ("موعد", 3), ("اجتماع", 3), ("اجرب", 3), ("تجربه", 2),
                ("demo", 5), ("schedule", 3), ("book a", 3), ("meeting", 3), ("try it", 3)
            }),
            new(new("trust", "/trust", "الثقة والحوكمة", "Trust & Governance"), new[]
            {
                ("ثقه", 4), ("حوكمه", 4), ("خصوصيه", 4), ("امان", 3), ("امن البيانات", 4), ("حمايه البيانات", 4),
                ("بياناتي", 3), ("امتثال", 4), ("سياده", 3), ("شفافيه", 3),
                ("trust", 4), ("governance", 4), ("privacy", 4), ("security", 4), ("secure", 3),
                ("compliance", 4), ("gdpr", 4), ("my data", 3)
            }),
            new(new("blog", "/blog", "الأخبار والرؤى", "News & Insights"), new[]
            {
                ("اخبار", 4), ("مقالات", 4), ("مقال", 3), ("مدونه", 4), ("رؤى", 3), ("جديد", 1),
                ("blog", 4), ("news", 4), ("article", 4), ("insight", 3), ("latest", 2)
            }),
            new(new("partners", "/partnerregistration", "تسجيل الشركاء", "Partner Registration"), new[]
            {
                ("شريك", 4), ("شركاء", 4), ("شراكه", 4), ("موزع", 3), ("وكيل", 3),
                ("partner", 4), ("reseller", 4), ("affiliate", 3)
            }),
            new(new("shop", "/shop", "المتجر", "Shop"), new[]
            {
                ("متجر", 4), ("المتجر", 4), ("اشتري", 3), ("شراء", 3),
                ("shop", 4), ("store", 4), ("buy", 3), ("purchase", 3)
            }),
            new(new("home", "/", "الرئيسية", "Home"), new[]
            {
                ("الرئيسيه", 4), ("الصفحه الرئيسيه", 5), ("home page", 5), ("homepage", 5)
            })
        };

        // Explicit "take me there" phrasing makes a weak match good enough.
        private static readonly string[] NavigationVerbs =
        {
            "وديني", "ودينى", "خدني", "روح", "افتح", "افتحلي", "اعرض", "وريني", "فين صفحه", "صفحه",
            "take me", "go to", "open", "show me", "navigate", "page"
        };

        private static readonly Regex Diacritics = new("[ً-ٰٟـ]", RegexOptions.Compiled);
        private static readonly Regex NonWord = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

        /// <summary>Returns the best matching page, or null when the question isn't about a specific page.</summary>
        public static ChatPageTarget? Resolve(string? question)
        {
            if (string.IsNullOrWhiteSpace(question)) return null;

            var text = " " + Normalize(question) + " ";
            var boost = NavigationVerbs.Any(v => ContainsWord(text, v)) ? 1 : 0;

            Rule? best = null;
            var bestScore = 0;
            foreach (var rule in Rules)
            {
                var score = 0;
                foreach (var (phrase, weight) in rule.Keywords)
                {
                    if (ContainsWord(text, phrase)) score += weight;
                }
                if (score > bestScore)
                {
                    best = rule;
                    bestScore = score;
                }
            }

            return best != null && bestScore + boost >= MinScore ? best.Target : null;
        }

        // Arabic clitics that may be glued to the start of a word (و، ف، ب، ل، ك، ال and combinations).
        private static readonly string[] ArabicPrefixes = { "", "ال", "و", "وال", "ف", "فال", "ب", "بال", "ل", "لل", "ك", "كال" };

        /// <summary>
        /// True when <paramref name="phrase"/> starts a word in <paramref name="paddedText"/>, so "plan"
        /// matches "plans" but not "explain". Arabic words may carry a leading clitic.
        /// </summary>
        private static bool ContainsWord(string paddedText, string phrase)
        {
            var p = Normalize(phrase);
            if (p.Length == 0) return false;
            if (p[0] < 0x0600 || p[0] > 0x06FF) return paddedText.Contains(" " + p, StringComparison.Ordinal);
            return ArabicPrefixes.Any(prefix => paddedText.Contains(" " + prefix + p, StringComparison.Ordinal));
        }

        /// <summary>Lower-cases and folds Arabic letter variants so spelling differences still match.</summary>
        internal static string Normalize(string input)
        {
            var s = Diacritics.Replace(input.ToLowerInvariant(), string.Empty);
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                sb.Append(c switch
                {
                    'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                    'ة' => 'ه',
                    'ى' => 'ي',
                    'ؤ' => 'و',
                    'ئ' => 'ي',
                    _ => c
                });
            }
            return NonWord.Replace(sb.ToString(), " ").Trim();
        }
    }
}
