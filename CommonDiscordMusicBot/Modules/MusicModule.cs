using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;
using CommonDiscordMusicBot.Services;
using Serilog;

namespace CommonDiscordMusicBot.Modules
{
    public sealed class MusicModule : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private readonly MusicService _musicService;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

        public MusicModule(DiscordSocketClient client, LavaNode lavaNode, MusicService musicService)
        {
            _lavaNode = lavaNode;
            _musicService = musicService;
            _client = client;
        }

        public Task InitializeAsync()
        {
            Log.Information("{0} Connecting to lavalink", DateTime.UtcNow);

            _client.Ready += OnReadyAsync;
            return Task.CompletedTask;
        }

        private async Task OnReadyAsync()
        {
            try
            {
                await _lavaNode.ConnectAsync();
            }
            catch (Exception exception)
            {
                Log.Debug(exception.Message);
            }

        }
        [Command("play"), Alias("p")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            var voiceState = Context.User as IVoiceState;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return;
            }

            if (voiceState?.VoiceChannel is not null && !_lavaNode.HasPlayer(Context.Guild))
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            var searchResponse = await _lavaNode.SearchAsync(Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube, searchQuery);

            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await ReplyAsync($"Can't find: `{searchQuery}`.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                player.Queue.Enqueue(searchResponse.Tracks);
                var track = searchResponse.Tracks.FirstOrDefault();

                var embed = new EmbedBuilder()
                    .AddField("Enqueued", $"[{searchResponse.Playlist.Name}]({searchResponse.Playlist})")
                    .WithAuthor($"Loaded playlist with {searchResponse.Tracks.Count} tracks")
                    .WithColor(Color.Magenta)
                    .WithThumbnailUrl(await track.FetchArtworkAsync());

                await ReplyAsync(embed: embed.Build());
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Queue.Enqueue(track);

                var artwork = await track.FetchArtworkAsync();
                var embed = new EmbedBuilder()
                    .AddField("Enqueued", $"[{track?.Title}]({track?.Url})")
                    .WithAuthor($"{track?.Author}")
                    .WithThumbnailUrl(artwork)
                    .WithColor(Color.Magenta);

                await ReplyAsync(embed: embed.Build());
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Queue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(x =>
            {
                x.Track = lavaTrack;
                x.ShouldPause = false;
            });
        }

        [Command("Pause")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            try
            {
                await player.PauseAsync();
                await ReplyAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                Log.Debug(exception.Message);
                await ReplyAsync("Error occurred");
            }
        }

        [Command("Resume")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                return;
            }

            try
            {
                await player.ResumeAsync();
                await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                Log.Debug(exception.Message);
                await ReplyAsync("Error occurred");
            }
        }

        [Command("Skip"), Alias("s")]
        public async Task SkipAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            try
            {
                var (oldTrack, currentTrack) = await player.SkipAsync();
                var artwork = await currentTrack.FetchArtworkAsync();
                var embed = new EmbedBuilder();
                embed.AddField("Now playing", $"[{currentTrack.Title}]({currentTrack.Url})")
               .WithAuthor(currentTrack?.Author)
               .WithThumbnailUrl(artwork)
               .WithColor(Color.Magenta);

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception exception)
            {
                Log.Debug("Exception handled:{0}", exception.Message);
                await ReplyAsync("Error occured with track (age restriction?)");
            }
        }

        [Command("Seek")]
        public async Task SeekAsync(TimeSpan timeSpan)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            try
            {
                await player.SeekAsync(timeSpan);
            }
            catch (Exception exception)
            {
                Log.Information("Attempt of execute command with wrong argument \n exception handled {0}", exception.Message);
            }
        }

        [Command("Volume"), Alias("vol")]
        public async Task SetVolumeAsync(ushort volume)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            try
            {
                await player.UpdateVolumeAsync(volume);
                await ReplyAsync($"Volume: {volume}.");
            }
            catch (Exception exception)
            {
                Log.Debug("Exception handled: {0}", exception.Message);
            }
        }

        [Command("NowPlaying"), Alias("np")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            var track = player.Track;

            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder();
            embed.AddField("Now playing", $"[{track.Title}]({track.Url}) ")
           .WithAuthor(track?.Author)
           .WithThumbnailUrl(artwork)
           .WithColor(Color.Magenta)
           .WithFooter($"{track?.Position}/{track?.Duration}");

            await ReplyAsync(embed: embed.Build());
        }

        [Command ("Queue"), Alias ("Q")]
        public Task ShowQueueAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return ReplyAsync("Empty");
            }

            return ReplyAsync(player.PlayerState != PlayerState.Playing
                           ? "Nothing in queue."
                           : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
        }

        [Command("Leave"), Alias("ds")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to any voice channels!");
                return;
            }

            var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("Not sure which voice channel to disconnect from.");
                return;
            }

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                await ReplyAsync($"I've left {voiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Join")]
        public async Task JoinAsync()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
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

        [Command("Genius", RunMode = RunMode.Async)]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }

            await SendLyricsAsync(lyrics);
        }

        [Command("OVH", RunMode = RunMode.Async)]
        public async Task ShowOvhLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromOvhAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }

            await SendLyricsAsync(lyrics);
        }

        private async Task SendLyricsAsync(string lyrics)
        {
            var splitLyrics = lyrics.Split(Environment.NewLine);
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics)
            {
                if (line.Contains('['))
                {
                    stringBuilder.Append(Environment.NewLine);
                }

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
    }
}

