﻿using Caerostris.Services.Spotify.Web;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        public async Task<string> GetUsername() =>
            (await dispatcher.GetPrivateProfile()).GetUsername();

        public async Task<string> GetUserId() =>
            (await dispatcher.GetPrivateProfile()).Id;
    }
}
