﻿using Newtonsoft.Json;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackState
    {
        [JsonProperty("repeat_mode")]
        public int RepeatMode { get; set; }

        [JsonProperty("shuffle")]
        public bool ShuffleState { get; set; }

        [JsonProperty("context")]
        public Context? Context { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("position")]
        public int ProgressMs { get; set; }

        [JsonProperty("paused")]
        public bool Paused { get; set; }

        [JsonProperty("track_window")]
        public WebPlaybackTrackWindow? TrackWindow { get; set; }

        public PlaybackContext ApplyTo(PlaybackContext context)
        {
            context.RepeatState =
                (RepeatMode == 0
                    ? RepeatState.Off
                    : (RepeatMode == 1
                        ? RepeatState.Context
                        : RepeatState.Track));

            if (!(Context is null))
                context.Context = Context;
            context.ShuffleState = ShuffleState;
            context.Timestamp = Timestamp;
            context.ProgressMs = ProgressMs;
            context.IsPlaying = !Paused;

            if (!(TrackWindow?.CurrentTrack?.Type is null))
                context.CurrentlyPlayingType = TrackWindow.CurrentTrack.Type;

            TrackWindow?.CurrentTrack?.ApplyTo(context.Item);

            return context;
        }
    }
}
