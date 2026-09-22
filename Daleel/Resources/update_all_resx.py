import xml.etree.ElementTree as ET
import os

def update_or_add_resx(filepath, translations):
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return
        
    tree = ET.parse(filepath)
    root = tree.getroot()
    
    # Create a mapping of key to data elements
    keys_map = {}
    for data in root.findall('data'):
        keys_map[data.get('name')] = data
        
    for key, val in translations.items():
        if key in keys_map:
            # Update existing
            data_elem = keys_map[key]
            value_elem = data_elem.find('value')
            if value_elem is not None:
                value_elem.text = val
            else:
                value_elem = ET.SubElement(data_elem, 'value')
                value_elem.text = val
            print(f"Updated key: {key}")
        else:
            # Add new
            data_elem = ET.SubElement(root, 'data')
            data_elem.set('name', key)
            data_elem.set('xml:space', 'preserve')
            value_elem = ET.SubElement(data_elem, 'value')
            value_elem.text = val
            print(f"Added key: {key}")
            
    # Save back to file
    tree.write(filepath, encoding='utf-8', xml_declaration=True)
    print(f"Saved: {filepath}")

# English translations
en_updates = {
    "GlobalVision": "Move forward with confidence",
    "LocalPrecision": "in a rapidly changing world.",
    "HeroDescription": "Daleel delivers trusted information, global coverage, and meaningful economic and humanitarian insight through a modern media platform powered by Neurix AI — enabling fast, accurate, and reliable reporting 24/7.\n\nDaleel… A clearer perspective on the world.",
    "AdvTruth": "Verified Information. Clearer Perspective.",
    "OneTruth": "Smarter Decisions.",
    "HarvestDescription": "Trusted data, intelligent analysis, and continuous global coverage — all within a unified platform designed to help individuals and organizations understand the world and make decisions driven by knowledge, not noise.",
    "ReadyToLead": "Ready to Navigate With Clarity?",
    "ReadyToLeadDesc": "Join a global community that trusts Daleel for reliable information, deep economic insight, and meaningful media — within a modern digital ecosystem built on human values, transparency, and ethical responsibility",
    "TheDaleel": "The Daleel",
    "Framework": "Framework",
    "FrameworkDesc": "Three interconnected pillars powering global strategic decision support.",
    "TabInformation": "Information & Decision Support",
    "GlobalInformationDesc": "A comprehensive ecosystem for gathering and analyzing information across global markets and industries — providing a trusted source of insight for decision-makers and institutions.",
    "RealTimeNews": "Real-time news and global event monitoring",
    "RegulatoryFiling": "Regulatory filing and compliance tracking",
    "MultiLanguageProcessing": "Multilingual information processing",
    "TabTrade": "Global Trade",
    "GlobalTradeDesc": "Intelligent solutions for analyzing and optimizing cross-border trade operations, powered by logistics strategy and AI-driven geopolitical risk modeling.",
    "SupplyChainOpt": "Supply chain optimization",
    "TariffCompliance": "Tariff planning and compliance strategy",
    "RiskCorridor": "Trade corridor and risk analysis",
    "TabMedia": "Media & Communications",
    "PurposefulMediaDesc": "Strategic media and communication solutions designed to help organizations and brands deliver clear, impactful narratives across the global digital landscape.",
    "SentimentAnalysis": "Sentiment and trend analysis",
    "NarrativeIntelligence": "Narrative analysis and messaging strategy",
    "DigitalReputation": "Digital reputation planning and media positioning",
    "AdvHumanitarian": "Designed around a deep understanding of the human experience.",
    "CrossDomainFusion": "Integrated Insights:",
    "CrossDomainDesc": "The most meaningful insights are not found within isolated systems, but at the intersection of trade, media, data, and global context",
    "AnalysisTitle": "AI-Driven Governance",
    "AnalysisDesc": "Machine learning models are transforming how governments anticipate policy impact, strengthen regulatory compliance, and manage risk at scale.",
    "PlatformsHeading": "The Daleel Ecosystem",
    "PlatformsDesc": "The Daleel Ecosystem develops interconnected intelligent platforms that unify economic, scientific, and cultural knowledge within an integrated digital infrastructure designed to serve people and support sustainable progress.\n\nThe ecosystem is built on a vision that believes the true value of technology is not measured solely by economic return, but by its ability to create meaningful human impact while advancing knowledge and innovation."
}

# Arabic translations
ar_updates = {
    "GlobalVision": "تقدّم بثقة",
    "LocalPrecision": "في عالم سريع التغيّر.",
    "HeroDescription": "منصة دليل هي بوضوح بوابتك إلى المعلومات الموثقة، والتغطية العالمية، والرؤى الاقتصادية والإنسانية — ضمن منظومة رقمية حديثة مدعومة بتقنيات Neurix AI لتقديم محتوى دقيق، سريع، وموثوق على مدار الساعة\n\nدليل… رؤيتك إلى عالم أكثر وضوحًا.",
    "AdvTruth": "معلومات موثقة. رؤية أوضح.",
    "OneTruth": "قرارات أذكى",
    "HarvestDescription": "بيانات موثوقة، وتحليلات ذكية، وتغطية عالمية مستمرة — ضمن منصة موحدة تساعدك على فهم العالم واتخاذ قرارات مبنية على المعرفة، لا الضوضاء.",
    "ReadyToLead": "هل أنت مستعد للإبحار بوضوح؟",
    "ReadyToLeadDesc": "انضم إلى مجتمع عالمي يثق بدليل للوصول إلى معلومات موثوقة، ورؤى اقتصادية عميقة، وإعلام هادف — ضمن منظومة رقمية حديثة قائمة على القيم الإنسانية، والشفافية، والمسؤولية الأخلاقية",
    "TheDaleel": "إطار عمل",
    "Framework": "دليل",
    "FrameworkDesc": "ثلاث ركائز مترابطة تشكّل أساس الرؤية الاستراتيجية العالمية",
    "TabInformation": "المعلومات واتخاذ القرار",
    "GlobalInformationDesc": "منظومة شاملة لجمع وتحليل المعلومات عبر الأسواق والقطاعات العالمية، توفّر مصدرًا موثوقًا لصنّاع القرار والمؤسسات.",
    "RealTimeNews": "مراقبة الأخبار والتحولات العالمية لحظيًا",
    "RegulatoryFiling": "تتبع الإيداعات والبيانات التنظيمية",
    "MultiLanguageProcessing": "تحليل ومعالجة متعددة اللغات",
    "TabTrade": "التجارة العالمية",
    "GlobalTradeDesc": "حلول ذكية لتحليل وتحسين العمليات التجارية العابرة للحدود، مدعومة بتحليلات لوجستية متطورة وتحليلات للمخاطر الجيوسياسية المعتمدة على الذكاء الاصطناعي.",
    "SupplyChainOpt": "تحسين كفاءة سلاسل الإمداد",
    "TariffCompliance": "تخطيط الامتثال والرسوم الجمركية",
    "RiskCorridor": "تحليل ممرات ومخاطر التجارة العالمية",
    "TabMedia": "الإعلام والتواصل",
    "PurposefulMediaDesc": "بناء استراتيجيات إعلامية واتصالية مؤثرة تساعد المؤسسات والعلامات التجارية على إيصال رسائلها بوضوح وفاعلية ضمن المشهد الرقمي العالمي.",
    "SentimentAnalysis": "تحليل المشاعر والاتجاهات",
    "NarrativeIntelligence": "استراتيجية السرد وصناعة الرسائل",
    "DigitalReputation": "إدارة السمعة والتخطيط الإعلامي الرقمي",
    "AdvHumanitarian": "مصمم انطلاقًا من فهم عميق للتجربة الإنسانية",
    "CrossDomainFusion": "تكامل المجالات",
    "CrossDomainDesc": "القيمة الحقيقية لا تكمن في تحليل التجارة أو الإعلام أو البيانات كلٌ على حدة، بل في فهم العلاقات التي تربط بينها.",
    "AnalysisTitle": "الذكاء الاصطناعي في الحوكمة الحديثة:",
    "AnalysisDesc": "تستخدم الحكومات والمؤسسات النماذج التنبؤية لفهم تأثير السياسات، وتعزيز الامتثال التنظيمي، وتحسين إدارة المخاطر بكفاءة غير مسبوقة",
    "PlatformsHeading": "منظومة دليل",
    "PlatformsDesc": "تعمل منظومة دليل على تطوير منصات ذكية مترابطة توحّد المعرفة الاقتصادية، والعلمية، والثقافية ضمن بنية رقمية متكاملة تهدف إلى خدمة الإنسان ودعم التقدم المستدام.\n\nوترتكز المنظومة على رؤية تؤمن بأن القيمة الحقيقية للتكنولوجيا لا تُقاس بالعائد الاقتصادي وحده، بل بقدرتها على إحداث أثر إنساني ملموس وتعزيز المعرفة والابتكار"
}

if __name__ == "__main__":
    current_dir = os.path.dirname(os.path.abspath(__file__))
    en_file = os.path.join(current_dir, "SharedResource.en.resx")
    ar_file = os.path.join(current_dir, "SharedResource.ar.resx")
    
    print("Updating English resources...")
    update_or_add_resx(en_file, en_updates)
    
    print("\nUpdating Arabic resources...")
    update_or_add_resx(ar_file, ar_updates)
    print("\nAll ResX files updated successfully!")
