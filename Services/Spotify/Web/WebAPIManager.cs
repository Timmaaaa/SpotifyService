﻿using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpotifyService.IndexedDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web
{
    /// <remarks>
    /// This class is not intented to follow an actual proxy pattern, because most of the functionality offered by SpotifyWebAPI is never used by this service.
    /// The chief goal of this class is to provide in-memory, LocalStorage and IndexedDB caching as well as to automatically supply parameters to SpotifyWebAPI to enable e.g. Track Relinking.
    /// </remarks>
    public class WebAPIManager
    {
        private SpotifyWebAPI api;
        private IndexedDBManager indexedDB;

#pragma warning disable CS8618 // Non-initialized use of this class is not considered a valid use-case.
        public WebAPIManager(IndexedDBManager injectedIndexedDBManager)
#pragma warning restore CS8618
        {
            indexedDB = injectedIndexedDBManager;
        }

        /// <summary>
        /// Call this method before attempting to interact with this class in any way.
        /// </summary>
        public void Initialize(SpotifyWebAPI spotifyWebAPI)
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
            if (privateProfile is null || privateProfile.HasError())
                privateProfile = await api.GetPrivateProfileAsync();

            return privateProfile;
        }

        public async Task<PlaybackContext> GetPlayback() =>
            await api.GetPlaybackAsync(await GetMarket());

        public async Task<ErrorResponse?> ResumePlayback() =>
            await api.ResumePlaybackAsync(offset: "");

        public async Task<ErrorResponse?> SetPlayback(string? contextURI, string? trackURI)
        {
            if (contextURI is null && trackURI is null)
                return new ErrorResponse();
            else if (contextURI is null && !(trackURI is null))
                return await api.ResumePlaybackAsync(deviceId: "", contextUri: "", (new string[] { trackURI }).ToList(), offset: "");
            else
                return await api.ResumePlaybackAsync(deviceId: "", contextUri: contextURI, null, offset: trackURI);
        }

        public async Task<ErrorResponse?> SetPlayback(IEnumerable<string> URIs) =>
            await api.ResumePlaybackAsync(deviceId: "", contextUri: "", URIs.ToList(), offset: "");

        public async Task<ErrorResponse?> PausePlayback() =>
            await api.PausePlaybackAsync();

        public async Task<ErrorResponse?> SkipPlaybackToNext() =>
            await api.SkipPlaybackToNextAsync();

        public async Task<ErrorResponse?> SkipPlaybackToPrevious() =>
            await api.SkipPlaybackToPreviousAsync();

        public async Task<ErrorResponse?> SeekPlayback(int positionMs) =>
            await api.SeekPlaybackAsync(positionMs);

        public async Task<AvailabeDevices> GetDevices() =>
            await api.GetDevicesAsync();

        public async Task<ErrorResponse?> TransferPlayback(string deviceID, bool play = false) =>
            await api.TransferPlaybackAsync(deviceID, play);

        public async Task<ErrorResponse?> SetShuffle(bool shuffle) =>
            await api.SetShuffleAsync(shuffle);

        public async Task<ErrorResponse?> SetRepeatMode(RepeatState state) =>
            await api.SetRepeatModeAsync(state);

        public async Task<ErrorResponse?> SetVolume(int volumePercent) =>
            await api.SetVolumeAsync(volumePercent);

        public async Task<IEnumerable<SavedTrack>> GetSavedTracks(Action<int, int> progressCallback)
        {
            // TODO: don't start this again if its running already

            var cachedTracks = await GetCachedSavedTracks(progressCallback);
            if (!(cachedTracks is null) && cachedTracks.Count() > 0)
                return cachedTracks;

            return await DownloadSavedTracks(progressCallback);
        }

        public async Task<int> GetSavedTrackCount()
        {
            var (real, _) = await GetRealAndChachedSavedTrackCount();
            return real;
        }

        #region Comfort

        private async Task<string> GetMarket()
        {
            return (await GetPrivateProfile()).Country;
        }

        private async Task<IEnumerable<SavedTrack>> DownloadSavedTracks(Action<int, int> progressCallback)
        {
            var result = new List<SavedTrack>();
            const int rateLimitDelayMs = 200, pageSize = 50;
            int offset = 0;
            while (true)
            {
                var page = await api.GetSavedTracksAsync(pageSize, offset, await GetMarket());
                if (!(page.Items is null) && page.Items.Count > 0)
                {
                    result.AddRange(page.Items);
                    progressCallback(offset + page.Items.Count, page.Total);
                    await SaveSavedTracks(page.Items);
                }

                if (!page.HasNextPage())
                    break;

                offset += pageSize;

                await Task.Delay(rateLimitDelayMs);
            }

            return result;
        }

        private async Task SaveSavedTracks(IEnumerable<SavedTrack> tracks)
        {
            foreach (var track in tracks)
                await indexedDB.AddRecord(new StoreRecord<SavedTrack>
                {
                    Storename = nameof(SavedTrack),
                    Data = track
                });
        }

        private async Task<IEnumerable<SavedTrack>> GetCachedSavedTracks(Action<int, int> progressCallback)
        {
            /// There is no way to get e.g. a hash of all track IDs from Spotify, so we have to improvise a way to tell if the Library has been updated since we last cached it. 
            ///  This method is obviously quite crude, but should work for /most/ intents and purposes (people don't frequently remove tracks from their libraries).
            var (realCount, cachedCount) = await GetRealAndChachedSavedTrackCount();
            if(realCount != cachedCount)
            {
                await indexedDB.ClearStore(nameof(SavedTrack));
                return new List<SavedTrack>();
            }

            var records = new List<SavedTrack>();
            const int count = 100;
            int offset = 0;

            while (offset < cachedCount)
            {
                var paginatedRecords =
                    await indexedDB.GetPaginatedRecords<SavedTrack>(
                        new StoreIndexQuery<object> { Storename = nameof(SavedTrack), IndexName = null /* = primary key */ },
                        offset,
                        count);

                records.AddRange(paginatedRecords);
                progressCallback(records.Count, cachedCount);

                if (paginatedRecords.Count() < count)
                    break;

                offset += count;
            }

            return records;
        }

        private async Task<(int, int)> GetRealAndChachedSavedTrackCount()
        {
            var real = (await (api.GetSavedTracksAsync(10, 0, await GetMarket()))).Total;
            var cached = await indexedDB.GetCount(nameof(SavedTrack));
            return (real, cached);
        }

        #endregion
    }
}
