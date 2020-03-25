﻿using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Player.Models;
using SpotifyAPI.Web.Models;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private WebPlaybackSDKManager player;

        private string localDeviceID = "";
        private bool isPlaybackLocal = false;


        private async Task InitializePlayer(WebPlaybackSDKManager injectedPlayer)
        {
            player = injectedPlayer;

            await player.Initialize(
                GetAuthToken,
                OnError,
                OnPlaybackChanged,
                OnLocalPlayerReady);

            PlaybackChanged += OnDevicePotentiallyChanged;
        }

        private void OnPlaybackChanged(WebPlaybackState? state)
        {
            if (!(lastKnownPlayback is null) && !(state is null))
            {
                // state.ApplyTo(lastKnownPlayback); // TODO: better heuristics or permanent removal
                FirePlaybackContextChanged(lastKnownPlayback);
            }
        }

        private async Task OnLocalPlayerReady(string deviceID)
        {
            localDeviceID = deviceID;
            await TransferPlayback(deviceID);
            isPlaybackLocal = true;
            Log("Playback automatically transferred to local device.");
        }

        private async Task OnDevicePotentiallyChanged(PlaybackContext playback)
        {
            if (!(playback?.Device?.Id is null))
            {
                bool playbackContextIndicatesLocalPlayback =
                    playback.Device.Id.Equals(localDeviceID, StringComparison.InvariantCulture);

                if (isPlaybackLocal && !playbackContextIndicatesLocalPlayback)
                {
                    isPlaybackLocal = false;
                    Log("Playback transferred to remote device.");
                }
                else if (!isPlaybackLocal && playbackContextIndicatesLocalPlayback)
                {
                    isPlaybackLocal = true;
                    Log("Playback transferred back to local device.");
                }
            }

            await Task.CompletedTask; // TODO: two event signatures
        }
    }
}
