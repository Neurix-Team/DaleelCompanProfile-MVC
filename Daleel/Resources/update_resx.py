import xml.etree.ElementTree as ET
import sys
import os

def update_resx(filepath, translations):
    # Parse existing resx
    tree = ET.parse(filepath)
    root = tree.getroot()
    
    # Get existing keys to avoid duplicates
    existing_keys = set()
    for data in root.findall('data'):
        existing_keys.add(data.get('name'))
        
    # Append new keys
    for key, val in translations.items():
        if key not in existing_keys:
            data = ET.SubElement(root, 'data')
            data.set('name', key)
            data.set('xml:space', 'preserve')
            value = ET.SubElement(data, 'value')
            value.text = val
            
    # Save back to file
    tree.write(filepath, encoding='utf-8', xml_declaration=True)

en_dict = {
    # Phase 1
    "WelcomeToDaleel": "Welcome to Daleel",
    "GlobalVision": "Global Vision.",
    "LocalPrecision": "Local Precision.",
    "HeroDescription": "Navigate the complexities of international trade and information with the world's most advanced strategic insights platform.",
    "ScrollToExplore": "Scroll to explore",
    "DataIntelligence": "Data Strategy",
    "TenMillionSources": "10M+ Sources.",
    "OneTruth": "One Truth.",
    "HarvestDescription": "We harvest real-time data from 140 jurisdictions — news feeds, regulatory filings, trade logs, and satellite imagery — turning raw information into strategic advantages.",
    "Countries": "Countries",
    "Monitoring": "Monitoring",
    "Verified": "Verified",
    "NeuralFusionEngine": "Neural Fusion Engine",
    "AIThatSees": "AI That Sees",
    "WhatYouCant": "What You Can't.",
    "AIDescription": "Our proprietary Neural Fusion Engine cross-correlates geopolitical events with trade flows, sentiment shifts, and macro indicators — uncovering hidden patterns invisible to the human eye.",
    "PatternRecognition": "Pattern Recognition",
    "SentimentAnalysis": "Sentiment Analysis",
    "RiskCorrelation": "Risk Correlation",
    "StrategicCommand": "Strategic Command",
    "ClarityFrom": "Clarity From",
    "Complexity": "Complexity.",
    "DashboardDescription": "Our analysts distill fused insights into actionable strategic briefs — complete with scenario modeling, risk heat maps, and opportunity scoring — delivered in real time.",
    "OpportunityScore": "Opportunity Score",
    "RiskExposure": "Risk Exposure",
    "ReadyToLead": "Ready to Lead?",
    "ReadyToLeadDesc": "Join 150,000+ trade partners across 140 jurisdictions who trust Daleel to power their global strategy.",
    "ExploreSolutions": "Explore Platforms",
    "DownloadBriefing": "Download Briefing",

    # Phase 2
    "EnterpriseGradeIntelligence": "Enterprise Grade Strategy",
    "PrecisionStrategy": "Precision Strategy",
    "ForTheGlobalMarket": "for the Global Market.",
    "PrecisionStrategyDesc": "Daleel Global Vision provides the navigational clarity required to dominate complex international landscapes through data-driven precision.",
    "WatchBriefing": "Watch Briefing",
    "GlobalInsightAI": "Global Insight AI",
    "Online": "Online",
    "AnalyzeTradeCorridors": "Analyze trade corridors...",
    "MarketVolatility": "Market Volatility",
    "SupplyChain": "Supply Chain",
    "VerifiedArticles": "Verified Articles",
    "TradePartners": "Trade Partners",
    "TrustScore": "Trust Score",
    "AIMonitoring": "AI Monitoring",

    # Phase 3
    "OurCorePillars": "Our Core Pillars",
    "TheDaleel": "The Daleel",
    "Framework": "Framework",
    "FrameworkDesc": "Three interconnected pillars that form the foundation of global strategic decision support.",
    "GlobalInformation": "Global Information",
    "GlobalInformationDesc": "Comprehensive information harvesting across all major trade corridors. A single source of truth for decision makers navigating 140+ jurisdictions.",
    "RealTimeNews": "Real-time news monitoring",
    "RegulatoryFiling": "Regulatory filing tracking",
    "MultiLanguageProcessing": "Multi-language processing",
    "GlobalTrade": "Global Trade",
    "GlobalTradeDesc": "Seamless cross-border transaction analysis and optimization, powered by logistics strategy and AI-driven geopolitical risk modeling.",
    "SupplyChainOpt": "Supply chain optimization",
    "TariffCompliance": "Tariff & compliance mapping",
    "RiskCorridor": "Risk corridor analysis",
    "PurposefulMedia": "Purposeful Media",
    "PurposefulMediaDesc": "Strategic narrative shaping and communication strategies that ensure your vision resonates clearly in the global digital marketplace.",
    "NarrativeIntelligence": "Narrative analysis",
    "DigitalReputation": "Digital reputation mapping",

    # Phase 4
    "WhatSetsUsApart": "What Sets Us Apart",
    "WhyDaleelIsDifferent": "Why DALEEL is Different",
    "WhyDaleelDesc": "While others react to the market, we anticipate it. Our strategic infrastructure is built for the speed and complexity of the modern global economy.",
    "RealTimeIntelligence": "Real-Time Insights",
    "RealTimeIntDesc": "Our AI processes 10M+ data points per day across 140 jurisdictions — delivering actionable insights in minutes, not weeks.",
    "AvgInsightDelivery": "Average Insight Delivery",
    "TrustFirstArchitecture": "Trust-First Architecture",
    "TrustFirstDesc": "Every data point is verified through our proprietary trust-scoring system. No noise. No misinformation. Only verified insights you can stake decisions on.",
    "VerifiedTrustScore": "Verified Trust Score",
    "CrossDomainFusion": "Cross-Domain Fusion",
    "CrossDomainDesc": "We don't just aggregate — we fuse. Trade data meets geopolitical analysis meets media sentiment to reveal patterns no single-domain platform can see.",
    "InfoTradeMedia": "Information · Trade · Media",
    "TrueGlobalCoverage": "True Global Coverage",
    "GlobalCoverageDesc": "From MENA corridors to Southeast Asian supply chains — our global network spans every major trade region with local expertise and cultural context.",
    "JurisdictionsCovered": "Jurisdictions Covered",
    "PredictiveNotReactive": "Predictive, Not Reactive",
    "PredictiveDesc": "Our AI doesn't just report what happened — it forecasts what's coming. Risk signals are detected 72 hours before they hit mainstream radar.",
    "EarlyWarningAdvantage": "Early Warning Advantage",
    "EnterpriseSecurity": "Enterprise-Grade Security",
    "EnterpriseSecurityDesc": "SOC 2 compliant, end-to-end encrypted, with role-based access control. Your strategic data stays yours — protected at every layer.",
    "CertifiedEncrypted": "Certified & Encrypted",
    "ReadyToExperience": "Ready to experience the difference?",
    "ScheduleDemo": "Schedule a Demo",
    "ViewPricingPlans": "View Pricing Plans",

    # Phase 5
    "IntelligenceFeed": "Strategic Feed",
    "GlobalPerspectives": "Global Perspectives",
    "StrategicInsightsDesc": "Strategic insights from our world-class analysts.",
    "ViewAllInsights": "View All Insights",
    "WhitePaper": "White Paper",
    "WhitePaperTitle": "The 2025 Global Economic Pivot: Navigation Strategies",
    "WhitePaperDesc": "How emerging economies are reshaping trade corridors and what it means for institutional investors.",
    "MinRead12": "12 min read",
    "May152024": "May 15, 2024",
    "Report": "Report",
    "May122024": "May 12, 2024",
    "ReportTitle": "Supply Chain Resiliency: Mapping New Routes in South East Asia",
    "ReportDesc": "How shifting geopolitical dynamics are creating new logistics corridors and what enterprises need to prepare for.",
    "MinRead8": "8 min read",
    "Analysis": "Analysis",
    "May082024": "May 08, 2024",
    "AnalysisTitle": "AI in Governance: Predictive Modeling for Regulatory Compliance",
    "AnalysisDesc": "Machine learning models are transforming how governments anticipate policy impacts and enforce compliance at scale.",
    "MinRead6": "6 min read",
    "TradeAlert": "Trade Alert",
    "TradeAlertTitle": "MENA Trade Volumes Surge 23% in Q1 2024",
    "RiskAlert": "Risk Alert",
    "RiskAlertTitle": "Emerging Sanctions Impact on Commodity Flows",
    "MediaWatch": "Media Watch",
    "MediaWatchTitle": "Digital Narrative Shifts in Global ESG Reporting",
    "Opportunity": "Opportunity",
    "OpportunityTitle": "New Free Trade Zones Creating Investment Corridors"
}

ar_dict = {
    # Phase 1
    "WelcomeToDaleel": "مرحباً بك في دليل",
    "GlobalVision": "رؤية عالمية.",
    "LocalPrecision": "دقة محلية.",
    "HeroDescription": "تعامل مع تعقيدات التجارة الدولية والمعلومات من خلال المنصة الاستراتيجية الأكثر تقدماً في العالم.",
    "ScrollToExplore": "مرر للاستكشاف",
    "DataIntelligence": "استراتيجية البيانات",
    "TenMillionSources": "١٠ ملايين+ مصدر.",
    "OneTruth": "حقيقة واحدة.",
    "HarvestDescription": "نحن نجمع البيانات في الوقت الفعلي من 140 ولاية قضائية - موجزات الأخبار والإيداعات التنظيمية وسجلات التجارة وصور الأقمار الصناعية - لتحويل المعلومات الخام إلى مزايا استراتيجية.",
    "Countries": "دولة",
    "Monitoring": "مراقبة",
    "Verified": "مُوثق",
    "NeuralFusionEngine": "محرك الدمج العصبي",
    "AIThatSees": "الذكاء الاصطناعي يرى",
    "WhatYouCant": "ما لا يمكنك رؤيته.",
    "AIDescription": "يقوم محرك الدمج العصبي الخاص بنا بربط الأحداث الجيوسياسية مع التدفقات التجارية وتحولات المشاعر والمؤشرات الكلية - ليكشف عن الأنماط الخفية غير المرئية للعين البشرية.",
    "PatternRecognition": "التعرف على الأنماط",
    "SentimentAnalysis": "تحليل المشاعر",
    "RiskCorrelation": "الارتباط بالمخاطر",
    "StrategicCommand": "القيادة الاستراتيجية",
    "ClarityFrom": "الوضوح من",
    "Complexity": "التعقيد.",
    "DashboardDescription": "يقوم محللونا باستخلاص الرؤى المدمجة في ملخصات استراتيجية قابلة للتنفيذ - كاملة مع نمذجة السيناريوهات وخرائط المخاطر وتقييم الفرص - يتم تسليمها في الوقت الفعلي.",
    "OpportunityScore": "نقاط الفرصة",
    "RiskExposure": "التعرض للمخاطر",
    "ReadyToLead": "هل أنت مستعد للقيادة؟",
    "ReadyToLeadDesc": "انضم إلى أكثر من 150,000 شريك تجاري عبر 140 ولاية قضائية يثقون في دليل لتشغيل استراتيجيتهم العالمية.",
    "ExploreSolutions": "اكتشف المنصات",
    "DownloadBriefing": "تحميل الموجز",

    # Phase 2
    "EnterpriseGradeIntelligence": "استراتيجية بمستوى المؤسسات",
    "PrecisionStrategy": "استراتيجية دقيقة",
    "ForTheGlobalMarket": "للسوق العالمي.",
    "PrecisionStrategyDesc": "يوفر دليل رؤية ملاحية واضحة مطلوبة للسيطرة على المشاهد الدولية المعقدة من خلال الدقة المدفوعة بالبيانات.",
    "WatchBriefing": "شاهد الموجز",
    "GlobalInsightAI": "الذكاء الاصطناعي للرؤية العالمية",
    "Online": "متصل",
    "AnalyzeTradeCorridors": "تحليل مسارات التجارة...",
    "MarketVolatility": "تقلبات السوق",
    "SupplyChain": "سلسلة التوريد",
    "VerifiedArticles": "مقالة موثقة",
    "TradePartners": "شريك تجاري",
    "TrustScore": "نقاط الثقة",
    "AIMonitoring": "مراقبة ذكية",

    # Phase 3
    "OurCorePillars": "ركائزنا الأساسية",
    "TheDaleel": "إطار عمل",
    "Framework": "دليل",
    "FrameworkDesc": "ثلاث ركائز مترابطة تشكل أساس الرؤية الاستراتيجية العالمية.",
    "GlobalInformation": "المعلومات العالمية",
    "GlobalInformationDesc": "جمع شامل للمعلومات عبر جميع ممرات التجارة الرئيسية. مصدر واحد للحقيقة لصناع القرار الذين يتنقلون عبر أكثر من 140 ولاية قضائية.",
    "RealTimeNews": "مراقبة الأخبار لحظياً",
    "RegulatoryFiling": "تتبع الإيداعات التنظيمية",
    "MultiLanguageProcessing": "معالجة متعددة اللغات",
    "GlobalTrade": "التجارة العالمية",
    "GlobalTradeDesc": "تحليل سلس للمعاملات عبر الحدود وتحسينها، مدعوماً بتحليلات لوجستية متطورة ونمذجة للمخاطر الجيوسياسية المعتمدة على الذكاء الاصطناعي.",
    "SupplyChainOpt": "تحسين سلسلة التوريد",
    "TariffCompliance": "تخطيط الرسوم الجمركية والامتثال",
    "RiskCorridor": "تحليل ممرات المخاطر",
    "PurposefulMedia": "الإعلام الهادف",
    "PurposefulMediaDesc": "تشكيل السرد الاستراتيجي واستراتيجيات الاتصال التي تضمن صدى رؤيتك بوضوح في السوق الرقمي العالمي.",
    "NarrativeIntelligence": "تحليل السرد",
    "DigitalReputation": "تخطيط السمعة الرقمية",

    # Phase 4
    "WhatSetsUsApart": "ما يميزنا",
    "WhyDaleelIsDifferent": "لماذا دليل مختلف؟",
    "WhyDaleelDesc": "بينما يتفاعل الآخرون مع السوق، نحن نتوقعه. تم بناء البنية التحتية الاستراتيجية لدينا من أجل السرعة والتعقيد في الاقتصاد العالمي الحديث.",
    "RealTimeIntelligence": "رؤى في الوقت الفعلي",
    "RealTimeIntDesc": "يعالج الذكاء الاصطناعي لدينا أكثر من 10 ملايين نقطة بيانات يومياً عبر 140 ولاية قضائية - ليقدم رؤى قابلة للتنفيذ في دقائق وليس أسابيع.",
    "AvgInsightDelivery": "متوسط تسليم الرؤى",
    "TrustFirstArchitecture": "بنية الثقة أولاً",
    "TrustFirstDesc": "يتم التحقق من كل نقطة بيانات من خلال نظام تقييم الثقة الخاص بنا. لا ضوضاء. لا معلومات مضللة. فقط المعلومات الموثقة التي يمكنك اتخاذ القرارات بناءً عليه.",
    "VerifiedTrustScore": "نقاط ثقة مؤكدة",
    "CrossDomainFusion": "دمج عبر النطاقات",
    "CrossDomainDesc": "نحن لا نجمع فقط - بل ندمج. بيانات التجارة تلتقي بالتحليل الجيوسياسي ومشاعر وسائل الإعلام للكشف عن أنماط لا تستطيع أي منصة من نطاق واحد رؤيتها.",
    "InfoTradeMedia": "المعلومات · التجارة · الإعلام",
    "TrueGlobalCoverage": "تغطية عالمية حقيقية",
    "GlobalCoverageDesc": "من ممرات الشرق الأوسط وشمال أفريقيا إلى سلاسل التوريد في جنوب شرق آسيا — تمتد شبكتنا الاستراتيجية في كل منطقة تجارية رئيسية مع خبرة محلية وسياق ثقافي.",
    "JurisdictionsCovered": "الولايات القضائية المغطاة",
    "PredictiveNotReactive": "تنبؤي وليس تفاعلي",
    "PredictiveDesc": "الذكاء الاصطناعي لدينا لا ينقل فقط ما حدث — بل يتنبأ بما هو قادم. يتم اكتشاف إشارات الخطر قبل 72 ساعة من وصولها إلى الرادار السائد.",
    "EarlyWarningAdvantage": "ميزة الإنذار المبكر",
    "EnterpriseSecurity": "أمان بمستوى المؤسسات",
    "EnterpriseSecurityDesc": "متوافق مع SOC 2، مشفر من البداية للنهاية، مع تحكم في الوصول قائم على الأدوار. بياناتك الاستراتيجية تبقى لك - محمية في كل طبقة.",
    "CertifiedEncrypted": "معتمد ومشفر",
    "ReadyToExperience": "هل أنت مستعد لتجربة الفرق؟",
    "ScheduleDemo": "جدولة عرض توضيحي",
    "ViewPricingPlans": "عرض خطط التسعير",

    # Phase 5
    "IntelligenceFeed": "التغذية الاستراتيجية",
    "GlobalPerspectives": "رؤى عالمية",
    "StrategicInsightsDesc": "رؤى استراتيجية من محللينا العالميين.",
    "ViewAllInsights": "عرض جميع الرؤى",
    "WhitePaper": "ورقة بيضاء",
    "WhitePaperTitle": "المحور الاقتصادي العالمي 2025: استراتيجيات الملاحة",
    "WhitePaperDesc": "كيف تعيد الاقتصادات الناشئة تشكيل الممرات التجارية وماذا يعني ذلك للمستثمرين المؤسسيين.",
    "MinRead12": "12 دقيقة للقراءة",
    "May152024": "15 مايو 2024",
    "Report": "تقرير",
    "May122024": "12 مايو 2024",
    "ReportTitle": "مرونة سلسلة التوريد: رسم طرق جديدة في جنوب شرق آسيا",
    "ReportDesc": "كيف تؤدي الديناميكيات الجيوسياسية المتغيرة إلى إنشاء ممرات لوجستية جديدة وما يجب على المؤسسات الاستعداد له.",
    "MinRead8": "8 دقائق للقراءة",
    "Analysis": "تحليل",
    "May082024": "08 مايو 2024",
    "AnalysisTitle": "الذكاء الاصطناعي في الحوكمة: النمذجة التنبؤية للامتثال التنظيمي",
    "AnalysisDesc": "تغير نماذج التعلم الآلي كيفية توقع الحكومات لتأثيرات السياسات وتطبيق الامتثال على نطاق واسع.",
    "MinRead6": "6 دقائق للقراءة",
    "TradeAlert": "تنبيه تجاري",
    "TradeAlertTitle": "ارتفاع حجم التجارة في الشرق الأوسط وشمال أفريقيا بنسبة 23٪ في الربع الأول من 2024",
    "RiskAlert": "تنبيه بالمخاطر",
    "RiskAlertTitle": "تأثير العقوبات الناشئة على تدفقات السلع",
    "MediaWatch": "مراقبة الإعلام",
    "MediaWatchTitle": "تحولات السرد الرقمي في الإبلاغ العالمي عن ESG",
    "Opportunity": "فرصة",
    "OpportunityTitle": "مناطق التجارة الحرة الجديدة تخلق ممرات استثمارية"
}

if __name__ == "__main__":
    current_dir = os.path.dirname(os.path.abspath(__file__))
    en_file = os.path.join(current_dir, "SharedResource.en.resx")
    ar_file = os.path.join(current_dir, "SharedResource.ar.resx")
    update_resx(en_file, en_dict)
    update_resx(ar_file, ar_dict)
    print("Updated ResX files successfully.")
