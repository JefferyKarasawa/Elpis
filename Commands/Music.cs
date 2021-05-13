using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace Template.Commands
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);


        public Music(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }
        [Command("Play", RunMode = RunMode.Async)]
        [Summary("Play the requested song. \n Usage: <prefix>play <song url> Example: !play https://www.youtube.com/<song>")]

        public async Task PlayAsync([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            var queries = searchQuery.Split(' ');
            foreach (var query in queries)
            {
                var searchResponse = await _lavaNode.SearchAsync(query);
                if (searchResponse.LoadStatus == LoadStatus.LoadFailed ||
                    searchResponse.LoadStatus == LoadStatus.NoMatches)
                {
                    await ReplyAsync($"I wasn't able to find anything for `{query}`.");
                    return;
                }

                var player = _lavaNode.GetPlayer(Context.Guild);

                if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
                {
                    if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                    {
                        foreach (var track in searchResponse.Tracks)
                        {
                            player.Queue.Enqueue(track);
                        }

                        await ReplyAsync($"Queued {searchResponse.Tracks.Count} tracks.");
                    }
                    else
                    {
                        var track = searchResponse.Tracks[0];
                        player.Queue.Enqueue(track);
                        await ReplyAsync($"Queued: {track.Title}");
                    }
                }
                else
                {
                    var track = searchResponse.Tracks[0];

                    if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                    {
                        for (var i = 0; i < searchResponse.Tracks.Count; i++)
                        {
                            if (i == 0)
                            {
                                await player.PlayAsync(track);
                                await ReplyAsync($"Now Playing: {track.Title}");
                            }
                            else
                            {
                                player.Queue.Enqueue(searchResponse.Tracks[i]);
                            }
                        }

                        await ReplyAsync($"Queued {searchResponse.Tracks.Count} tracks.");
                    }
                    else
                    {



                        await player.PlayAsync(track);
                        await ReplyAsync($"Now Playing: {track.Title}");
                    }
                }
            }
        }

        // Let's make a command that make's the bot join a voice channel. You must be in a voice channel for the sakes of making it easier for the bot to join a voice channel.

        [Command("Join", RunMode = RunMode.Async)]
        [Summary("Join the voice channel you are currently in. \n Usage: <prefix>join Example: !join")]

        public async Task JoinAsync()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        // Let's make a command that grabs lyrics from a website and posts it in discord channel.

        [Command("Genius", RunMode = RunMode.Async)]
        [Summary("Grabs lyrics of current song and post it on discord channel. \n Usage: <prefix>Genius Example !Genius")]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }

            var splitLyrics = lyrics.Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics)
            {
                if (Range.Contains(stringBuilder.Length))
                {
                    await ReplyAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            await ReplyAsync($"```{stringBuilder}```");
        }
        
        // Let's make a command to skip to the next song.

        [Command("Skip", RunMode = RunMode.Async)]
        [Summary("Skip to the next song in the playlist or queue. \n Usage: <prefix>skip Example: !skip")]

        public async Task Skip()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {

                await ReplyAsync("You need to be in the same channel as me! Let me in!");
                return;
            }

            if (player.Queue.Count == 0)
            {
                await ReplyAsync("There are no songs in the queue! Add some songs!");
                return;
            }

            await player.SkipAsync();
            await ReplyAsync($"Song skipped! Now playing **{player.Track.Title}**");


        }

        // Let's make a command to pause the current song

        [Command("Pause", RunMode = RunMode.Async)]
        [Summary("Pause the song that is currently playing. \n Usage: <prefix>pause Example: !pause")]

        public async Task Pause()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {

                await ReplyAsync("You need to be in the same channel as me! Let me in!");
                return;
            }

            if (player.PlayerState == PlayerState.Paused || player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Music is already paused!");
            }

            await player.PauseAsync();
            await ReplyAsync($"**{player.Track.Title}** has been paused");


        }

        // Let's make a command to resume the paused song

        [Command("Resume", RunMode = RunMode.Async)]
        [Summary("Resumes the paused song. \n Usage: <prefix>resume Example: !resume")]

        public async Task Resume()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {

                await ReplyAsync("You need to be in the same channel as me! Let me in!");
                return;
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await ReplyAsync("Music is already playing!");
            }

            await player.ResumeAsync();
            await ReplyAsync($"Resumed **{player.Track.Title}**");

        }

        // Let's make a command to stop the song currently playing on the bot.

        [Command("Stop")]
        [Summary("Stops the song currently playing. \n Usage: <prefix>stop Example: !stop")]

        public async Task Stop()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {

                await ReplyAsync("You need to be in the same channel as me! Let me in!");
                return;
            }

            if (player.PlayerState == PlayerState.Paused)
            {
                await ReplyAsync("Music is already paused!");
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("There is no music playing right now!");
            }

            await player.StopAsync();
            await ReplyAsync($"**{player.Track.Title}** has been stopped");


        }

        // Let's make a command to make the bot leave the voice channel

        [Command("Leave")]
        [Summary("Make the bot leave the voice channel. \n Usage: <prefix>leave Example: !leave")]

        public async Task LeaveAsync()
        {
            
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            // Check if user is in the same voice channel
            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {

                await ReplyAsync("You need to be in the same channel as me! Let me in!");
                return;
            }
            //Check if player is paused
            if (player.PlayerState == PlayerState.Paused)
            {
                await ReplyAsync("Song is only paused, you sure you want me to leave? !stop to make me stop");
            }
            //Check if player is stopped
            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Music stop check, making bot leave!");
            }
            
            // Make bot leave voice channel and announce
            
            await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
            await ReplyAsync($"Leaving {voiceState.VoiceChannel.Name}!");
            

        }


    }
}
