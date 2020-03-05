﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Caerostris.Services.Spotify.Web
{
    /// <remarks>
    /// This class is not intented to follow an actual proxy pattern, because most of the functionality offered by SpotifyWebAPI is never used by this service.
    /// The chief goal of this class is to provide in-memory, LocalStorage and IndexedDB caching as well as to automatically supply parameters to SpotifyWebAPI to enable e.g. Track Relinking.
    /// </remarks>
    public class WebAPIManager
    {
        private SpotifyWebAPI api;

        public WebAPIManager(SpotifyWebAPI spotifyWebAPI)
        {
            api = spotifyWebAPI;
        }

        /// <summary>
        /// We consider the private profile of the user to be unchanging during the typical lifecycle of this application, and therefore it gets cached in its entirety.
        /// </summary>
        private PrivateProfile? privateProfile;

        /// <returns>The private profile of the user</returns>
        public async Task<PrivateProfile> GetPrivateProfile()
        {
            if (privateProfile is null)
                privateProfile = await api.GetPrivateProfileAsync();

            return privateProfile;
        }

        public async Task<PlaybackContext> GetPlayback() => 
            await api.GetPlaybackAsync(await GetMarket());

        public async Task<ErrorResponse> ResumePlayback() =>
            await api.ResumePlaybackAsync(offset: "");

        public async Task<ErrorResponse> PausePlayback() =>
            await api.PausePlaybackAsync();

        public async Task<ErrorResponse> SkipPlaybackToNext() =>
            await api.SkipPlaybackToNextAsync();

        public async Task<ErrorResponse> SkipPlaybackToPrevious() =>
            await api.SkipPlaybackToPreviousAsync();

        public async Task<ErrorResponse> SeekPlayback(int positionMs) =>
            await api.SeekPlaybackAsync(positionMs);

        public async Task<AvailabeDevices> GetDevices() =>
            await api.GetDevicesAsync();

        public async Task<ErrorResponse> TransferPlayback(string deviceID, bool play = false) =>
            await api.TransferPlaybackAsync(deviceID, play);

        public async Task<ErrorResponse> SetShuffle(bool shuffle) =>
            await api.SetShuffleAsync(shuffle);

        public async Task<ErrorResponse> SetRepeatMode(RepeatState state) =>
            await api.SetRepeatModeAsync(state);

        public async Task<ErrorResponse> SetVolume(int volumePercent) =>
            await api.SetVolumeAsync(volumePercent);

        public async Task<IEnumerable<SavedTrack>> GetSavedTracks() =>
            (await api.GetSavedTracksAsync(50, 0, await GetMarket())).Items; // TODO

        #region Comfort

        private async Task<string> GetMarket()
        {
            return (await GetPrivateProfile()).Country;
        }

        #endregion
    }
}
