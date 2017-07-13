using MultilingualBotCommon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace MultilingualBot.Utilities
{
    public static class Utils
    {
        public static async Task<LuisResponse> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            LuisResponse data = new LuisResponse();
            using (HttpClient client = new HttpClient())
            {
                string requestUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/58a3a568-120d-4bd7-84bc-fe2c58e42894?subscription-key=164a83ce971642bdb3663420627a5c73&verbose=true&timezoneOffset=0&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(requestUri);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    data = JsonConvert.DeserializeObject<LuisResponse>(JsonDataResponse);
                }
            }

            return data;
        }

        public static async Task<TextAnalyticLanguageResponse> GetTextLanaguage(string Text)
        {
            Text = Uri.EscapeDataString(Text);

            TextAnalyticRequest request = new TextAnalyticRequest();
            request.documents.Add(new TextAnalyticDocument() { id = "1", text = Text });

            TextAnalyticLanguageResponse data = new TextAnalyticLanguageResponse();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "4fbf5d7bfcef441cabac799bb9556d96");

                string requestUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/languages";
                HttpResponseMessage msg = await client.PostAsJsonAsync<TextAnalyticRequest>(requestUri, request);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    data = JsonConvert.DeserializeObject<TextAnalyticLanguageResponse>(JsonDataResponse);
                }
            }

            return data;
        }

        public static async Task<string> TranslateText(string Text, string sourceLanguage, string destinationLanguage)
        {
            string text = Text;
            string from = sourceLanguage;
            string to = destinationLanguage;
            string uri = "https://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to;
            string translation = "";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            string subKey = System.Configuration.ConfigurationManager.AppSettings["TranslatorKey"];

            var authTokenSource = new AzureAuthToken(subKey);
            string authToken;
            authToken = await authTokenSource.GetAccessTokenAsync();
            
            httpWebRequest.Headers.Add("Authorization", authToken);
            using (WebResponse response = await httpWebRequest.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            {
                DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
                translation = (string)dcs.ReadObject(stream);
            }

            return translation;
        }

        public static async Task TranslateTexts(List<string> texts, string sourceLanguage, string destinationLanguage)
        {
            string subKey = System.Configuration.ConfigurationManager.AppSettings["TranslatorKey"];
            var from = sourceLanguage;
            var to = destinationLanguage;

            var uri = "https://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
            var body = "<TranslateArrayRequest>" +
                           "<AppId />" +
                           "<From>{0}</From>" +
                           "<Options>" +
                           " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">{1}</ContentType>" +
                               "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                           "</Options>" +
                           "<Texts>";

            foreach (string text in texts)
            {
                body += "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">" + text + "</string>";
            }

            body += "</Texts>" +
                           "<To>{2}</To>" +
                       "</TranslateArrayRequest>";

            string requestBody = string.Format(body, from, "text/plain", to);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "text/xml");

                var authTokenSource = new AzureAuthToken(subKey);
                string authToken;
                authToken = await authTokenSource.GetAccessTokenAsync();

                request.Headers.Add("Authorization", authToken);
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Console.WriteLine("Request status is OK. Result of translate array method is:");
                        var doc = XDocument.Parse(responseBody);
                        var ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
                        var sourceTextCounter = 0;
                        foreach (XElement xe in doc.Descendants(ns + "TranslateArrayResponse"))
                        {
                            foreach (var node in xe.Elements(ns + "TranslatedText"))
                            {
                                if (sourceTextCounter < texts.Count)
                                    texts[sourceTextCounter] = node.Value;
                            }
                            sourceTextCounter++;
                        }
                        break;
                    default:
                        Console.WriteLine("Request status code is: {0}.", response.StatusCode);
                        Console.WriteLine("Request error message: {0}.", responseBody);
                        break;
                }
            }
        }

        public static async Task<NewsResponses> GetNews(string region, string inputLanguage, string outputLanguage)
        {
            NewsResponses responses = new NewsResponses();
            responses.SourceHomepageLink = "https://www.nytimes.com";
            responses.SourceName = "The New York Times";
            responses.SourceImageLink = "https://upload.wikimedia.org/wikipedia/commons/7/77/The_New_York_Times_logo.png";
            string requestUri = "http://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml";

            if (region.ToLower() == "spain")
            {
                responses.SourceName = "EL PAÍS";
                responses.SourceHomepageLink = "https://elpais.com/";
                responses.SourceImageLink = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3e/El_Pais_logo_2007.svg/1024px-El_Pais_logo_2007.svg.png";
                requestUri = "http://ep00.epimg.net/rss/tags/ultimas_noticias.xml";
            }
            else if (region.ToLower() == "france")
            {
                responses.SourceHomepageLink = "http://www.lemonde.fr/";
                responses.SourceName = "Le Monde.fr";
                responses.SourceImageLink = "http://s1.dmcdn.net/CAp4m/240x240-WTD.jpg";
                requestUri = "http://www.lemonde.fr/rss/une.xml";
            }

            else if (region.ToLower() != "us")
            {
                responses.UserMessage = "Sorry, I don't know that location!  I'm still learning, but in the meantime here is the news from the New York Times.  Check back soon for your location, I get updated very often!";
            }

            using (HttpClient client = new HttpClient())
            {
                string rssContent = await client.GetStringAsync(requestUri);

                if (!String.IsNullOrEmpty(rssContent))
                {
                    List<string> translationTexts = new List<string>();

                    XNamespace media = "http://search.yahoo.com/mrss/";
                    XDocument doc = XDocument.Parse(rssContent);

                    List<XElement> items = doc.Descendants("item").ToList<XElement>();

                    foreach(XElement item in items)
                    {
                        string title = item.Descendants("title").FirstOrDefault<XElement>().Value;
                        string description = item.Descendants("description").FirstOrDefault<XElement>().Value;
                        string link = item.Descendants("link").FirstOrDefault<XElement>().Value;
                        string imageLink = "";

                        XElement imageElem = item.Descendants(media + "content").FirstOrDefault<XElement>();

                        if (imageElem != null)
                            imageLink = imageElem.Attribute("url").Value;

                        //if (outputLanguage != "en")
                        //{
                        //    title = await TranslateText(title, "en", outputLanguage);
                        //    description = await TranslateText(description, "en", outputLanguage);
                        //}

                        // add to translation collection
                        translationTexts.Add(title);
                        translationTexts.Add(description);

                        responses.Responses.Add(new NewsResponse()
                        {
                            title = title,
                            description = description,
                            articleLink = link,
                            imageLink = imageLink
                        });

                        if (responses.Responses.Count == 5)
                        {
                            // jump out after 10, is enough news for one response
                            break;
                        }
                    }

                    if (inputLanguage != outputLanguage)
                    {
                        await Utilities.Utils.TranslateTexts(translationTexts, inputLanguage, outputLanguage);

                        int textCounter = 0;

                        foreach (NewsResponse response in responses.Responses)
                        {
                            response.title = translationTexts[textCounter];
                            response.description = translationTexts[textCounter + 1];

                            textCounter = textCounter + 2;
                        }
                    }
                }
            }

            return responses;
        }
    }
}