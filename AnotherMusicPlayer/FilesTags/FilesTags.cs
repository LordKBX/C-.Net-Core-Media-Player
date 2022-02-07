﻿using System;
using System.IO;
using NAudio;
using NAudio.Wave;
using TagLib;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;

namespace AnotherMusicPlayer
{
    /// <summary> Class MediaItem, a storae structure for Music Media Basic MetaData </summary>
    public class MediaItem
    {
        public string Selected { get; set; }
        public string Path { get; set; }
        public string OriginPath { get; set; }
        public string Name { get; set; }
        public string Album { get; set; }
        public long Size { get; set; }
        public long Duration { get; set; }
        public string DurationS { get; set; }
        public string Genres { get; set; }
        public string Performers { get; set; }
        public string Composers { get; set; }
        public string Copyright { get; set; }
        public uint Disc { get; set; }
        public uint DiscCount { get; set; }
        public string AlbumArtists { get; set; }
        public string Lyrics { get; set; }
        public uint Track { get; set; }
        public uint TrackCount { get; set; }
        public uint Year { get; set; }
        public double Rating { get; set; }
        public uint PlayCount { get; set; }

        public string InnerUID { get; set; }

        public MediaItem()
        {
            Path = OriginPath
                = Selected
                = Name
                = Album
                = DurationS
                = Performers
                = Composers
                = Copyright
                = AlbumArtists
                = Lyrics
                = "";
            Duration = Size = 0;
            Disc = DiscCount = Track = TrackCount = Year = PlayCount = 0;
            Rating = 0.0;

            InnerUID = Guid.NewGuid().ToString();
        }
    }

    public partial class FilesTags
    {
        private static Dictionary<byte, double> TableRateWindows = new Dictionary<byte, double>() { { 0, 0.0 }, { 1, 1.0 }, { 64, 2.0 }, { 128, 3.0 }, { 196, 4.0 }, { 255, 5.0 } };
        private static Dictionary<double, byte> ReverseTableRateWindows = new Dictionary<double, byte>() { { 0.0, 0 }, { 1.0, 1 }, { 2.0, 64 }, { 3.0, 128 }, { 4.0, 196 }, { 5.0, 255 } };
        private static Dictionary<byte, double> TableRatePlayer = new Dictionary<byte, double>() { { 0, 0.0 }, { 2, 1.0 }, { 64, 2.0 }, { 128, 3.0 }, { 196, 4.0 }, { 255, 5.0 } };
        private static Dictionary<double, byte> ReverseTableRatePlayer = new Dictionary<double, byte>() { { 0.0, 0 }, { 1.0, 2 }, { 2.0, 64 }, { 3.0, 128 }, { 4.0, 196 }, { 5.0, 255 } };

        /// <summary> Recuperate Media MetaData(cover excluded) </summary>
        public static MediaItem MediaInfo(string FilePath, bool Selected, string OriginPath = null)
        {
            try
            {
                if (System.IO.File.Exists(FilePath) || System.IO.File.Exists(OriginPath))
                {
                    //Debug.WriteLine("MetaData Source: " + (OriginPath ?? FilePath));
                    TagLib.File tags;
                    if (System.IO.File.Exists(FilePath)) { tags = TagLib.File.Create(FilePath, ReadStyle.Average); }
                    else { tags = TagLib.File.Create(OriginPath, ReadStyle.Average); FilePath = OriginPath; }
                    MediaItem item = new MediaItem();
                    item.Name = tags.Tag.Title;
                    item.Album = tags.Tag.Album;
                    item.Path = FilePath;
                    item.OriginPath = OriginPath;
                    item.Selected = (Selected) ? MainWindow.PlayListSelectionChar : "";
                    item.Duration = (long)0;
                    item.DurationS = "00:00";

                    item.Size = new System.IO.FileInfo(OriginPath ?? FilePath).Length;
                    item.Performers = tags.Tag.JoinedPerformers;
                    item.Composers = tags.Tag.JoinedComposers;
                    item.Copyright = tags.Tag.Copyright;
                    item.Disc = tags.Tag.Disc;
                    item.DiscCount = tags.Tag.DiscCount;
                    item.AlbumArtists = tags.Tag.JoinedAlbumArtists;
                    item.Genres = tags.Tag.FirstGenre;
                    item.Lyrics = tags.Tag.Lyrics;
                    item.Track = tags.Tag.Track;
                    item.TrackCount = tags.Tag.TrackCount;
                    item.Year = tags.Tag.Year;

                    TagLib.Tag tag = tags.GetTag(TagLib.TagTypes.Id3v2);
                    byte rate1 = 0;

                    try { rate1 = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)tag, "Windows Media Player 9 Series", true).Rating; } catch { }

                    if (TableRatePlayer.ContainsKey(rate1)) { item.Rating = TableRatePlayer[rate1]; }
                    else
                    {
                        byte min = 255;
                        foreach (byte i in TableRatePlayer.Keys) { if (i >= rate1) { min = i; break; } }
                        if (min == 255) { item.Rating = 0; } else { item.Rating = (double)(TableRatePlayer[min] + 0.5); }
                    }
                    tags.Dispose();

                    try
                    {
                        foreach (string ext in Player.AcceptedExtentions)
                        {
                            if (FilePath.ToLower().EndsWith(ext))
                            {
                                item.Duration = (long)((new AudioFileReader(FilePath)).TotalTime.TotalMilliseconds);
                                item.DurationS = MainWindow.displayTime(item.Duration); break;
                            }
                        }
                    }
                    catch { }
                    return item;
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine("--> FilesTags.MediaItem ERROR <--");
                Debug.WriteLine(err.Message);
                Debug.WriteLine(err.ToString());
                //Debug.WriteLine(JsonConvert.SerializeObject(err));
            }
            return null;
        }

        /// <summary> Recuperate Media MetaData(cover excluded) </summary>
        public static PlayListViewItem MediaInfoShort(string FilePath, bool Selected, string OriginPath = null)
        {
            if (System.IO.File.Exists(FilePath) || System.IO.File.Exists(OriginPath))
            {
                //Debug.WriteLine("MetaData Source: " + (OriginPath ?? FilePath));
                TagLib.File tags;
                if (System.IO.File.Exists(FilePath)) { tags = TagLib.File.Create(FilePath); }
                else { tags = TagLib.File.Create(OriginPath, ReadStyle.Average); FilePath = OriginPath; }
                PlayListViewItem item = new PlayListViewItem();
                item.Name = tags.Tag.Title;
                item.Album = tags.Tag.Album;
                item.Path = FilePath;
                item.OriginPath = OriginPath;
                item.Selected = (Selected) ? MainWindow.PlayListSelectionChar : "";
                item.Duration = (long)0;
                item.DurationS = "00:00";

                item.Performers = tags.Tag.JoinedPerformers;
                item.Composers = tags.Tag.JoinedComposers;
                item.AlbumArtists = tags.Tag.JoinedAlbumArtists;

                tags.Dispose();

                try
                {
                    foreach (string ext in Player.AcceptedExtentions)
                    {
                        if (FilePath.ToLower().EndsWith(ext))
                        {
                            item.Duration = (long)((new AudioFileReader(FilePath)).TotalTime.TotalMilliseconds);
                            item.DurationS = MainWindow.displayTime(item.Duration); break;
                        }
                    }
                }
                catch { }
                return item;
            }
            return null;
        }

        public static bool SaveRating(string FilePath, double Rating, Player player)
        {
            if (!System.IO.File.Exists(FilePath)) { return false; }
            if (Rating < 0 || Rating > 5.0) { return false; }
            if (Common.IsFileLocked(FilePath) && player.GetCurrentFile() != FilePath) { return false; }
            Debug.WriteLine("FilesTags.SaveRating");
            Debug.WriteLine("Rating = " + Rating);
            Debug.WriteLine("Math.Truncate(Rating) = " + Math.Truncate(Rating));
            Debug.WriteLine("ReverseTableRateWindows[Math.Truncate(Rating)] = " + ReverseTableRateWindows[Math.Truncate(Rating)]);
            Debug.WriteLine("ReverseTableRatePlayer[Math.Truncate(Rating)] = " + ReverseTableRatePlayer[Math.Truncate(Rating)]);

            TagLib.Id3v2.Tag.DefaultVersion = 3; TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            TagLib.File fi = TagLib.File.Create(FilePath, ReadStyle.Average);
            TagLib.Tag tag = fi.GetTag(TagTypes.Id3v2);
            TagLib.Id3v2.PopularimeterFrame frame1 = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)tag, "Windows Media Player 9 Series", true);
            if (ReverseTableRateWindows.ContainsKey(Rating)) { frame1.Rating = ReverseTableRatePlayer[Rating]; }
            else
            {
                double r = Rating - Math.Round(Rating);
                frame1.Rating = (byte)(ReverseTableRatePlayer[Math.Truncate(Rating)] + 1);
            }
            if (player.GetCurrentFile() == FilePath)
            {
                player.Suspend();
                Thread.Sleep(200);
                fi.Save();
                player.Resume();
            }
            else { fi.Save(); }
            return true;
        }

    }
}
