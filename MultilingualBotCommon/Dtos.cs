using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultilingualBotCommon
{
    public class LuisResponse
    {
        public string query { get; set; }
        public Intent topScoringIntent { get; set; }
        public Intent[] intents { get; set; }
        public Entity[] entities { get; set; }
    }

    public class Intent
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public float score { get; set; }
    }

    public class TextAnalyticRequest
    {
        public List<TextAnalyticDocument> documents = new List<TextAnalyticDocument>();
    }

    public class TextAnalyticDocument
    {
        public string id { get; set; }
        public string language { get; set; }
        public string text { get; set; }
    }

    public class TextAnalyticLanguageResponse
    {
        public List<TextAnalyticLanguageDocument> documents = new List<TextAnalyticLanguageDocument>();
    }

    public class TextAnalyticLanguageDocument
    {
        public string id { get; set; }
        public List<TextAnalyticDetectedLanguage> detectedLanguages { get; set; }
    }

    public class TextAnalyticDetectedLanguage
    {
        public string name { get; set; }
        public string iso6391Name { get; set; }
        public string score { get; set; }
    }

    public class NewsResponses
    {
        public string SourceName { get; set; }
        public string UserMessage { get; set; }
        public string SourceImageLink { get; set; }
        public string SourceHomepageLink { get; set; }

        public List<NewsResponse> Responses = new List<NewsResponse>();
    }

    public class NewsResponse
    {
        public string title { get; set; }
        public string description { get; set; }
        public string articleLink { get; set; }
        public string imageLink { get; set; }
    }


}
