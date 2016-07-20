using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToTwitter;
using System.Globalization;
using System.IO;
using System.Configuration;

namespace TweetVideo
{
    public static class TweetVideo
    {
        public async static void SendTweet(string video, string tweet)
        {
            Console.WriteLine("Test");
 
            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = ConfigurationManager.AppSettings["consumerKey"],
                    ConsumerSecret = ConfigurationManager.AppSettings["consumerSecret"],
                    AccessToken = ConfigurationManager.AppSettings["accessToken"],
                    AccessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"]
                }
            };

            var twitterCtx = new TwitterContext(auth);

            await UploadVideoAsync(video, tweet, twitterCtx);


        }
        static async Task UploadVideoAsync(string video, string tweettxt, TwitterContext twitterCtx)
        {
            var additionalOwners = new List<ulong> { 3265644348, 15411837 };

            var media = await twitterCtx.UploadMediaAsync(
                File.ReadAllBytes(video), "video/mp4");

            Status tweet = await twitterCtx.TweetAsync(tweettxt, new ulong[] { media.MediaID });

            if (tweet != null)
                Console.WriteLine("Tweet sent: " + tweet.Text);

            if (tweet == null) { Console.WriteLine("Tweet failed."); }

        }

    }

}
