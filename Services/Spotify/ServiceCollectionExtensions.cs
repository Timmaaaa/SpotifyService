﻿using Blazor.Extensions.Storage;
using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Player;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register a SpotifyService and all its dependecies.
        /// </summary>
        public static IServiceCollection AddSpotify(this IServiceCollection services)
        {
            services.AddStorage();

            // The dependency injection module will take care of the Dispose() call
            services.AddSingleton<SpotifyService>();

            // Injected SpotifyService dependencies
            services.AddSingleton<ImplicitGrantAuthManager>();
            services.AddSingleton<WebPlaybackSDKManager>();

            return services;
        }
    }
}
