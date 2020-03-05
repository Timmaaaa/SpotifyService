﻿using Newtonsoft.Json;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackArtist
    {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
        #pragma warning restore CS8618

        public SimpleArtist ApplyTo(SimpleArtist artist)
        {
            artist.Name = Name;
            artist.Uri = Uri;

            return artist;
        }
    }
}