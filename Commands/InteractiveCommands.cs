using Discord.Commands;
using Discord.Addons.Interactive;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;

namespace Template.Commands
{
    public class InteractiveCommands : InteractiveBase

    {
        private readonly CommandService _service;
        
        public InteractiveCommands(CommandService service)
        {
            _service = service;
        }

        [Command("delete")]
        public async Task<RuntimeResult> Test_DeleteAfterAsync()
        {
            await ReplyAndDeleteAsync("I will disappear in 10 seconds!", timeout: new TimeSpan(0, 0, 10));
            return Ok();
        }

        [Command("next", RunMode = RunMode.Async)]
        public async Task Test_NextMessagAsync()
        {

            await ReplyAsync("What is 2+2??");
            var response = await NextMessageAsync(true, true, new TimeSpan(0, 0, 10));
            if (response != null)
            {
                if (response.Content == "4")
                    await ReplyAsync($"Your answer is correct! 4 is the correct answer!");
                else
                    await ReplyAsync("The answer you put in is wrong, the correct answer is 4.");
            }

            else
                await ReplyAsync("You did not reply within the given time");
        }

        [Command("paginator")]
        public async Task Test_Paginator()
        {

            List<string> Pages = new();

            Pages.Add("**Help command**\n !help - This is the help command");

            PaginatedMessage paginatedMessage = new()
            {
                Pages = Pages,
                Options = new PaginatedAppearanceOptions()
                {
                    InformationText = "this is test text",
                    Info = new Emoji("")

                },
                Color = new Discord.Color(33, 126, 252),
                Title = "Interactive Paginator"
            };
            await PagedReplyAsync(paginatedMessage);

        }

        // Going to add a help command and display, organized by commands and command module files
        [Command("help")]
        public async Task Help()
        {
            List<string> Pages = new List<string>();

            foreach(var module in _service.Modules)
            {
                string page = $"**{module.Name}**\n\n";
                foreach (var command in module.Commands)
                {
                    page += $"`!{command.Name}` - {command.Summary ?? "No description has been provided."}\n";
                }
                Pages.Add(page);
            }

            await PagedReplyAsync(Pages);
        }


    }

    
}
