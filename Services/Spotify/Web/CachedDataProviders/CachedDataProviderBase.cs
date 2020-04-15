﻿using Caerostris.Services.Spotify.Web.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    /// <summary>
    /// Provides in-memory caching and a template method for loading cached and remote resources for services implementing the <see cref="ICachedDataProvider{TData}"/> interface.
    /// </summary>
    public abstract class CachedDataProviderBase<TData> : ICachedDataProvider<TData>
    {
        protected Task<IEnumerable<TData>>? lastRetrieval; // TODO: weak reference

        public async Task<IEnumerable<TData>> GetData(Action<int, int> progressCallback, string market = "") // TODO: ez nem szálbiztos
        {
            /// Serving from memory cache.
            if (!(lastRetrieval is null))
            {
                if (!lastRetrieval.IsCompleted)
                    return await lastRetrieval;

                else if (await IsMemoryCacheValid())
                    return lastRetrieval.Result;
            }

            /// Serving from storage cache.
            if (await IsStorageCacheValid())
            {
                var cachedData = await SetAsLastRetrievalAndAwait(() => LoadStorageCache(progressCallback, market));
                if (cachedData.Count() > 0)
                    return cachedData;
            }
            else
            {
                await ClearStorageCache();
            }

            /// As a last resort, fetching remote resource.
            var remoteData = await SetAsLastRetrievalAndAwait(() => LoadRemoteResource(progressCallback));
            return remoteData;
        }

        /// <summary>
        /// Validates the memory cache.
        /// </summary>
        protected abstract Task<bool> IsMemoryCacheValid();

        /// <summary>
        /// Validates the storage cache.
        /// </summary>
        protected abstract Task<bool> IsStorageCacheValid();

        /// <summary>
        /// Clears the storage cache upon invalidation.
        /// </summary>
        protected abstract Task ClearStorageCache();

        /// <summary>
        /// Loads the storage (LocalStorage, IndexedDB) cache.
        /// </summary>
        protected abstract Task<IEnumerable<TData>> LoadStorageCache(Action<int, int> progressCallback, string market = "");

        /// <summary>
        /// Downloads the remote resource and saves it to the storage cache.
        /// </summary>
        protected abstract Task<IEnumerable<TData>> LoadRemoteResource(Action<int, int> progressCallback, string market = "");

        private async Task<IEnumerable<TData>> SetAsLastRetrievalAndAwait(Func<Task<IEnumerable<TData>>> func)
        {
            lastRetrieval = func();
            return await lastRetrieval;
        }
    }
}
