using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Template.Commands
{
    public class Fun : ModuleBase
    {
        [Command("reddit")]
        [Alias("reddit")]
        [Summary("Grab a random post from any existing subreddit. \n Usage: <prefix>reddit <subreddit> Example: !reddit dogs")]

        // Using reddit API to grab reddit memes

            public async Task Meme(string subreddit = null)
        {
            var client = new HttpClient();
            var result = await client.GetStringAsync($"https://reddit.com/r/{subreddit ?? "memes"}/random.json?limit=1");
            if(!result.StartsWith("["))
            {
                await Context.Channel.SendMessageAsync("The requested subreddit does not exist! Please choose another one");
                return;
            }
            JArray arr = JArray.Parse(result);
            JObject post = JObject.Parse(arr[0]["data"]["children"][0]["data"].ToString());
            
            //Let's show this information from reddit onto an embed

            var builder = new EmbedBuilder()
                .WithImageUrl(post["url"].ToString())
                .WithColor(new Color(33, 176, 252))
                .WithTitle(post["title"].ToString())
                .WithUrl("https://reddit.com" + post["permalink"].ToString())
                .WithFooter($"🗨 {post["num_comments"]} ⬆️ {post["ups"]}");
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(null, false, embed);

        }

        [Command("eightball")]
        [Summary("Ask the eightball a question and it will give you a random answer. Usage: <prefix>eightball <question>")]
        
        // Let's code in a magic eightball command where you ask a question and it replies with a preterminded string of responses
        public async Task Eightball([Remainder] string question = null)
        {
            // Trying to figure out how to let client put in any strings and it responds a reponse from within the array
            string[] responses = { "Yes", "No", "Maybe", "Not a chance", "Absolutely", "It's probable", "Maybe one a million!", "Is your luck good today?" };
            await ReplyAsync(responses[new Random().Next(0, responses.Count())]);
        }

        
        [Command("420")] 
        
        // Let's put in a stoner 420 command :P
        public async Task WeedMinute()
        {
            DateTime currentTime = DateTime.Now; //Current time
            DateTime weedMinuteMorning = Convert.ToDateTime("4:21:00"); //4:20am
            DateTime weedMinuteEvening = Convert.ToDateTime("16:21:00"); //4:20pm



            if (currentTime <= weedMinuteMorning)
            {
                TimeSpan timeSpan = weedMinuteMorning.Subtract(currentTime); // Calculating the time to next 4:20

                await Context.Channel.SendMessageAsync("The next weed minute will happen in " + timeSpan.ToString("hh' hours 'mm'  minutes.'"));
            }
            else if (currentTime <= weedMinuteEvening)
            {
                TimeSpan timeSpan = weedMinuteEvening.Subtract(currentTime);

                await Context.Channel.SendMessageAsync("The next weed minute will happen in " + timeSpan.ToString("hh' hours 'mm'  minutes.'"));
            }
            else
            {
                //To handle dates greater than 16:21:00, we must calculate hours remaining until 4:20 next day
                weedMinuteMorning = weedMinuteMorning.AddDays(1);
                TimeSpan timeSpan = weedMinuteMorning.Subtract(currentTime);

                await Context.Channel.SendMessageAsync("The next weed minute will happen in " + timeSpan.ToString("hh' hours 'mm'  minutes.'"));

            }
        }



    }
}
