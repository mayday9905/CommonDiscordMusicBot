using System.Collections.Concurrent;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Serilog;

namespace CommonDiscordMusicBot.Services
{
    public sealed class MusicService
    {
        private readonly LavaNode _lavaNode;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;


        public MusicService(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
            _lavaNode.OnStatsReceived += OnStatsReceived;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackStarted += OnTrackStarted;
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosed;
        }

        private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private Task OnStatsReceived(StatsEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private async Task OnTrackStarted(TrackStartEventArgs arg)
        {
            if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            Log.Logger.Verbose("Started track {0} in {1}({2})", arg.Player.Track.Id, arg.Player.VoiceChannel.Name, arg.Player.VoiceChannel.Id);
            value.Cancel(true);
            await arg.Player.TextChannel.SendMessageAsync();
        }

        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (args.Reason != TrackEndReason.Finished)
            {
                return;
            }

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var lavaTrack))
            {
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(900));
                return;
            }
            
            await args.Player.PlayAsync(lavaTrack);
        }

        private async Task OnTrackException(TrackExceptionEventArgs arg)
        {
            Log.Logger.Information("Track {0} ended ahead of schedule, caused by {1}", arg.Player.Track.Id ,arg.Exception.Message);
            await arg.Player.SkipAsync();
        }

        private async Task OnTrackStuck(TrackStuckEventArgs arg)
        {
            Log.Logger.Information("Track {0} stucked, for:{1} seconds", arg.Player.Track.Id, arg.Threshold.TotalSeconds);
            await Task.CompletedTask;
        }

        private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            Log.Logger.Debug("Web Socket (Guild:{0}) unexpectedly closed: {1}", arg.GuildId, arg.Reason);
            return Task.CompletedTask;
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
            await player.TextChannel.SendMessageAsync("Disconnected due to inactivity");
        }
    }
}
