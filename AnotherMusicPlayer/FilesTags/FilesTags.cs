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
            Disc = DiscCount = Track = TrackCount = Year = 0;
        }
    }

    class FilesTags
    {
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

                    tags.Dispose();

                    try
                    {
                        foreach (string ext in Player.AcceptedExtentions)
                        {
                            if (FilePath.ToLower().EndsWith(ext))
                            {
                                AudioFileReader fir = new AudioFileReader(FilePath);
                                item.Duration = (long)fir.TotalTime.TotalMilliseconds;
                                fir.Dispose();
                                item.DurationS = MainWindow.displayTime(item.Duration);
                                break;
                            }
                        }
                    }
                    catch { }
                    return item;
                }
            }
            catch (Exception err) {
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
                            AudioFileReader fir = new AudioFileReader(FilePath);
                            item.Duration = (long)fir.TotalTime.TotalMilliseconds;
                            fir.Dispose();
                            item.DurationS = MainWindow.displayTime(item.Duration);
                            break;
                        }
                    }
                }
                catch { }
                return item;
            }
            return null;
        }

        /// <summary> Recuperate Media Cover </summary>
        public static BitmapImage MediaPicture(string FilePath, Database bdd = null, bool export = true, int MaxWidth = 400, int MaxHeight = 400, bool save = true)
        {
            if (!System.IO.File.Exists(FilePath)) { return null; }
            BitmapImage bitmap = null;
            string FilePathExt = FilePath + "|" + MaxWidth + "x" + MaxHeight;
            try
            {
                string data = null;
                if (bdd != null) { data = bdd.DatabaseGetCover(FilePathExt); }
                if (data != null)
                {
                    bitmap = BitmapMagic.Base64StringToBitmap(data);
                }
                else
                {
                    TagLib.File tags = TagLib.File.Create(FilePath);

                    if (tags.Tag.Pictures.Length > 0)
                    {
                        TagLib.IPicture pic = tags.Tag.Pictures[0];

                        //Debug.WriteLine("Picture size = " + pic.Data.Data.Length);
                        MemoryStream ms = new MemoryStream(pic.Data.Data);
                        ms.Seek(0, SeekOrigin.Begin);
                        // ImageSource for System.Windows.Controls.Image
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();

                        if ((bitmap.PixelWidth > MaxWidth || bitmap.PixelHeight > MaxHeight) && MaxWidth > 0 && MaxHeight > 0)
                        {
                            double width = bitmap.PixelWidth, height = bitmap.PixelHeight;
                            if (width > MaxWidth) { height = (height / width) * MaxWidth; width = MaxWidth; }
                            if (height > MaxHeight) { width = (width / height) * MaxHeight; height = MaxHeight; }

                            ms.Seek(0, SeekOrigin.Begin); System.Drawing.Image im = System.Drawing.Image.FromStream(ms); ms.Close();
                            System.Drawing.Bitmap im2 = ResizeImage(im, Convert.ToInt32(width), Convert.ToInt32(height));
                            bitmap = null; bitmap = ConvertBitmapToBitmapImage(im2);

                            MemoryStream ms2 = new MemoryStream(); im2.Save(ms2, ImageFormat.Jpeg); ms2.Seek(0, SeekOrigin.Begin);

                            if (save)
                            {
                                TagLib.Picture pic2 = new TagLib.Picture();
                                pic2.Type = TagLib.PictureType.FrontCover; pic2.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg; pic2.Description = "Cover";
                                pic2.Data = TagLib.ByteVector.FromStream(ms2); tags.Tag.Pictures = new TagLib.IPicture[1] { pic2 };
                                try { tags.Save(); } catch { } //error if file already opened
                            }
                            ms2.Close();
                        }
                        bitmap.Freeze();

                        if (bdd != null) {
                            bdd.DatabaseSaveCover(
                            FilePathExt,
                            BitmapMagic.BitmapToBase64String(
                                BitmapMagic.BitmapImage2Bitmap(bitmap),
                                System.Drawing.Imaging.ImageFormat.Jpeg
                                )
                            ); }
                    }
                    else
                    {
                        char SeparatorChar = System.IO.Path.DirectorySeparatorChar;
                        string folder = FilePath.Substring(0, FilePath.LastIndexOf(SeparatorChar));
                        bitmap = (System.IO.File.Exists(folder + SeparatorChar + "Cover.jpg")) ? new BitmapImage(new Uri(folder + SeparatorChar + "Cover.jpg"))
                            : ((System.IO.File.Exists(folder + SeparatorChar + "Cover.png")) ? new BitmapImage(new Uri(folder + SeparatorChar + "Cover.png")) : MainWindow.Bimage("CoverImg"));
                    }
                    tags.Dispose();
                }
            }
            catch (Exception err) { Debug.WriteLine(JsonConvert.SerializeObject(err)); }
            return (export) ? bitmap : null;
        }

        /// <summary>
        /// Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        private static BitmapImage ConvertBitmapToBitmapImage(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

    }
}
