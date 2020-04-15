﻿using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyService.IndexedDB;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    /// <remarks>
    /// There is no way to get e.g. a hash of all track IDs from Spotify, so we have to improvise a way to tell if the Library has been updated since we last cached it. 
    /// The method of checking the number of saved tracks is obviously quite crude, but should work most of the time (people don't frequently remove tracks from their libraries).
    /// </remarks>
    public class SavedTrackManager : CachedDataProviderBase<SavedTrack>
    {
        private readonly SpotifyWebAPI api;
        private readonly IndexedDbCache<SavedTrack> storageCache;

        private const string storeName = nameof(SavedTrack);

        public SavedTrackManager(SpotifyWebAPI spotifyWebApi, IndexedDbCache<SavedTrack> indexedDbCache)
        {
            api = spotifyWebApi;
            storageCache = indexedDbCache;
        }

        protected override async Task<bool> IsMemoryCacheValid() =>
            await RealAndMemoryChachedSavedTrackCountsMatch();

        protected override async Task<bool> IsStorageCacheValid() =>
            await RealAndIndexedDbChachedSavedTrackCountsMatch();

        protected override async Task ClearStorageCache() =>
            await storageCache.Clear(storeName);

        protected override async Task<IEnumerable<SavedTrack>> LoadStorageCache(Action<int, int> progressCallback, string market = "") =>
            await storageCache.Load(storeName, progressCallback);

        protected override async Task<IEnumerable<SavedTrack>> LoadRemoteResource(Action<int, int> progressCallback, string market = "")
        {
            return await Utility.DownloadPagedResources(
                (o, p) => api.GetSavedTracksAsync(offset: o, limit: p, market: market),
                progressCallback,
                async (tracks) => { await storageCache.Save(storeName, tracks); });
        }

        private async Task<int> GetRealSavedTrackCount(string market = "") =>
            (await api.GetSavedTracksAsync(10, 0, market)).Total;

        private async Task<int> GetIndexedDbTrackCount() =>
            await storageCache.GetCount(storeName);

        private async Task<bool> RealAndMemoryChachedSavedTrackCountsMatch(string market = "")
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = lastRetrieval?.Result?.Count() ?? 0;
            return (real == cached);
        }

        private async Task<bool> RealAndIndexedDbChachedSavedTrackCountsMatch(string market = "")
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = await GetIndexedDbTrackCount();
            return (real == cached);
        }
    }
}