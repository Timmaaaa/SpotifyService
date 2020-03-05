﻿using Caerostris.Services.Spotify.Web;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class FlatSavedTrack
    {
        #pragma warning disable CS8618 // Always initialized with one
        public SavedTrack SavedTrack { get; set; }
        #pragma warning restore CS8618

        public DateTime AddedAt => SavedTrack.AddedAt;
        
        public string Name => SavedTrack.Track.Name;

        public string Album => SavedTrack.Track.Album.Name;

        public string Artists => SavedTrack.Track.GetArtists();

        public int DurationMS => SavedTrack.Track.DurationMs;
    }
}