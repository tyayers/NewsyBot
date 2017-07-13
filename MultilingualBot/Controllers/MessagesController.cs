using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using MultilingualBotCommon;
using System.Collections.Generic;
using System.Linq;

namespace MultilingualBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                string currentLanguageCode = "en", region = "us";
                string userText = activity.Text;

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                // Send typing activity
                Activity typingReply = activity.CreateReply();
                typingReply.Type = ActivityTypes.Typing;
                connector.Conversations.ReplyToActivityAsync(typingReply);

                // First, detect user language
                TextAnalyticLanguageResponse languageResponse = await Utilities.Utils.GetTextLanaguage(activity.Text);
                if (languageResponse.documents != null && languageResponse.documents.Count > 0)
                {
                    currentLanguageCode = languageResponse.documents[0].detectedLanguages[0].iso6391Name;
                    Activity languageReply = activity.CreateReply("Found language: " + currentLanguageCode);
                    await connector.Conversations.ReplyToActivityAsync(languageReply);
                }

                // If detected language is different than english, then translate it
                if (currentLanguageCode != "en")
                {
                    userText = await Utilities.Utils.TranslateText(userText, currentLanguageCode, "en");

                    Activity translatedReply = activity.CreateReply("Translated text: " + userText);
                    await connector.Conversations.ReplyToActivityAsync(translatedReply);
                }

                //await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                MultilingualBotCommon.LuisResponse dto = await Utilities.Utils.GetEntityFromLUIS(userText);

                if (dto.entities.Length > 0)
                {
                    region = dto.entities[0].entity;
                }

                Activity reply = activity.CreateReply("Ok! Checking if I have some news from '" + region + "'...");
                await connector.Conversations.ReplyToActivityAsync(reply);

                connector.Conversations.ReplyToActivityAsync(typingReply);

                NewsResponses news = await Utilities.Utils.GetNews(region, "", currentLanguageCode);

                if (news.UserMessage != "")
                {
                    // There is a message for the user, show it
                    Activity messageReply = activity.CreateReply(news.UserMessage);
                    await connector.Conversations.ReplyToActivityAsync(messageReply);
                }

                // Show the news source info that was returned
                Activity newsSourceReply = activity.CreateReply("");
                List<CardImage> newsSourceImages = new List<CardImage>();
                newsSourceImages.Add(new CardImage(url: news.SourceImageLink));

                List<CardAction> newsSourceCardButtons = new List<CardAction>();

                CardAction newsSourceButton = new CardAction()
                {
                    Value = news.SourceHomepageLink,
                    Type = "openUrl",
                    Title = news.SourceName
                };

                newsSourceCardButtons.Add(newsSourceButton);

                ThumbnailCard newsSourceCard = new ThumbnailCard()
                {
                    Title = $"Found news from {news.SourceName}",
                    Subtitle = $"{news.SourceName} Homepage",
                    Images = newsSourceImages,
                    Buttons = newsSourceCardButtons
                };

                Attachment newsSourceAttachment = newsSourceCard.ToAttachment();
                newsSourceReply.Attachments.Add(newsSourceAttachment);
                await connector.Conversations.ReplyToActivityAsync(newsSourceReply);

                // Now show the real news results
                Activity newsReply = activity.CreateReply("News results");
                newsReply.AttachmentLayout = "carousel";

                newsReply.Attachments = new List<Attachment>();

                foreach (NewsResponse article in news.Responses)
                {
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: article.imageLink));

                    List<CardAction> cardButtons = new List<CardAction>();

                    CardAction plButton = new CardAction()
                    {
                        Value = article.articleLink,
                        Type = "openUrl",
                        Title = news.SourceName
                    };

                    cardButtons.Add(plButton);

                    HeroCard plCard = new HeroCard()
                    {
                        Title = article.title,
                        Subtitle = article.description,
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    newsReply.Attachments.Add(plAttachment);
                }

                await connector.Conversations.ReplyToActivityAsync(newsReply);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                var client = new ConnectorClient(new Uri(message.ServiceUrl));
                IConversationUpdateActivity update = message;
                if (update.MembersAdded.Any())
                {
                    var reply = message.CreateReply();
                    var newMembers = update.MembersAdded?.Where(t => t.Id != message.Recipient.Id);
                    foreach (var newMember in newMembers)
                    {
                        reply.Text = "Hey! I'm the NewsyBot, and can show you news from around the world.  Don't worry if you don't speak the language, I'll translate it for you as well.";
                        client.Conversations.ReplyToActivityAsync(reply);
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}