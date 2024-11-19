﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using Control = System.Windows.Forms.Control;
using Button = System.Windows.Forms.Button;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Timer = System.Windows.Forms.Timer;
using ByteDev.Strings;
using CustomExtensions;
using Manina.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.ObjectModel;
using Sprache;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using NAudio.Gui;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Markup;

namespace AnotherMusicPlayer.MainWindow2
{
    public partial class MainWindow2 : Form
    {
        private Timer LoadTimer = new Timer();

        private static SolidColorBrush DefaultBrush = new SolidColorBrush(Colors.White);

        private static readonly int ButtonIconSize = 24;
        private static Bitmap IconPlayback = Icons.FromIconKind(IconKind.Music, ButtonIconSize, DefaultBrush);
        private static Bitmap IconLibrary = Icons.FromIconKind(IconKind.FolderMusic, ButtonIconSize, DefaultBrush);
        private static Bitmap IconPlayLists = Icons.FromIconKind(IconKind.PlaylistMusic, ButtonIconSize, DefaultBrush);
        private static Bitmap IconSettings = Icons.FromIconKind(IconKind.Cog, ButtonIconSize, DefaultBrush);

        private static Bitmap IconOpen = Icons.FromIconKind(IconKind.FolderOpen, ButtonIconSize, DefaultBrush);
        private static Bitmap IconPrevious = Icons.FromIconKind(IconKind.SkipBackward, ButtonIconSize, DefaultBrush);
        private static Bitmap IconPlay = Icons.FromIconKind(IconKind.Play, ButtonIconSize, DefaultBrush);
        private static Bitmap IconPause = Icons.FromIconKind(IconKind.Pause, ButtonIconSize, DefaultBrush);
        private static Bitmap IconNext = Icons.FromIconKind(IconKind.SkipForward, ButtonIconSize, DefaultBrush);
        private static Bitmap IconRepeat = Icons.FromIconKind(IconKind.Repeat, ButtonIconSize, DefaultBrush);
        private static Bitmap IconRepeatOnce = Icons.FromIconKind(IconKind.RepeatOnce, ButtonIconSize, DefaultBrush);
        private static Bitmap IconRepeatOff = Icons.FromIconKind(IconKind.RepeatOff, ButtonIconSize, DefaultBrush);
        private static Bitmap IconShuffle = Icons.FromIconKind(IconKind.Shuffle, ButtonIconSize, DefaultBrush);
        private static Bitmap IconClearList = Icons.FromIconKind(IconKind.PlaylistRemove, ButtonIconSize, DefaultBrush);

        private ObservableCollection<PlayListViewItem> PlayListItems = new ObservableCollection<PlayListViewItem>();
        private int PlaylistIndexAtLoading = 0;

        private TabbedThumbnail customThumbnail;
        private System.Drawing.Icon ThumbnailIconPrev = null;
        private System.Drawing.Icon ThumbnailIconPlay = null;
        private System.Drawing.Icon ThumbnailIconPause = null;
        private System.Drawing.Icon ThumbnailIconNext = null;
        private ThumbnailToolBarButton buttonPrev = null;
        private ThumbnailToolBarButton buttonPlay = null;
        private ThumbnailToolBarButton buttonNext = null;

        private List<Lyrics> lyricsWindowsList = new List<Lyrics>();

        /// <summary> Object music player </summary>
        public MainWindow2()
        {
            InitializeComponent();
            //Settings.LastPlaylistIndex = 4;
            //Settings.SaveSettings();
            PlaylistIndexAtLoading = Settings.LastPlaylistIndex;
            if (Settings.LastRepeatStatus == 0) { Player.Repeat(false); Player.Loop(false); }
            else if (Settings.LastRepeatStatus == 1) { Player.Repeat(true); Player.Loop(false); }
            else { Player.Repeat(false); Player.Loop(true); }
            Debug.WriteLine("PlaylistIndexAtLoading = " + PlaylistIndexAtLoading);

            #region Window displasment gestion
            MainWIndowHead.MouseDown += FormDragable_MouseDown;
            MainWIndowHead.MouseMove += FormDragable_MouseMove;
            MainWIndowHead.MouseUp += FormDragable_MouseUp;
            TitleLabel.MouseDown += FormDragable_MouseDown;
            TitleLabel.MouseMove += FormDragable_MouseMove;
            TitleLabel.MouseUp += FormDragable_MouseUp;
            #endregion

            #region Window resize gestion
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.ResizeRedraw = true;
            GripButton.Tag = "MainWindow";
            GripButton.MouseDown += SizerMouseDown;
            GripButton.MouseMove += SizerMouseMove;
            GripButton.MouseUp += SizerMouseUp;
            #endregion

            try
            {
                SetTitle(null);
                TabControler.Renderer.BorderColor = Color.FromKnownColor(System.Drawing.KnownColor.White);

                #region Define Tabs Icons
                PlaybackTab.Icon = IconPlayback;
                LibraryTab.Icon = IconLibrary;
                PlayListsTab.Icon = IconPlayLists;
                SettingsTab.Icon = IconSettings;
                #endregion

                BtnOpen.BackgroundImage = IconOpen;
                BtnOpen.Click += (object sender, EventArgs e) => { };

                BtnPrevious.BackgroundImage = IconPrevious;
                BtnPrevious.Click += (object sender, EventArgs e) => { Player.Stop(Player.GetCurrentFile()); Player.PlaylistPrevious(); };

                BtnPlayPause.BackgroundImage = (Player.LatestPlayerStatus == PlayerStatus.Play) ? IconPlay : IconPause;
                BtnPlayPause.Click += (object sender, EventArgs e) => 
                { 
                    if (Player.IsPlaying()) { Player.Pause(); } else { Player.Play(); } 
                    BtnPlayPause.BackgroundImage = (Player.LatestPlayerStatus == PlayerStatus.Play) ? IconPlay : IconPause;
                    buttonPlay.Icon = (Player.LatestPlayerStatus == PlayerStatus.Play) ? ThumbnailIconPlay : ThumbnailIconPause; 
                };

                BtnNext.BackgroundImage = IconNext;
                BtnNext.Click += (object sender, EventArgs e) => { Player.Stop(Player.GetCurrentFile()); Player.PlaylistNext(); };

                BtnRepeat.BackgroundImage = (Player.IsRepeat()) ? IconRepeatOnce : (Player.IsLoop()) ? IconRepeat : IconRepeatOff;
                BtnRepeat.Click += (object sender, EventArgs e) => {
                    if (Player.IsLoop()) { Player.Repeat(true); Player.Loop(false); }
                    else if (Player.IsRepeat()) { Player.Repeat(false); Player.Loop(false); }
                    else { Player.Repeat(false); Player.Loop(true); }
                    BtnRepeat.BackgroundImage = (Player.IsRepeat()) ? IconRepeatOnce : (Player.IsLoop()) ? IconRepeat : IconRepeatOff;
                };

                BtnShuffle.BackgroundImage = IconShuffle;
                BtnShuffle.Click += (object sender, EventArgs e) => { Player.PlaylistRandomize(); };

                BtnClearList.BackgroundImage = IconClearList;
                BtnClearList.Click += (object sender, EventArgs e) => { Player.PlaylistClear(); };

                PlaybackTabDataGridView.AutoGenerateColumns = false;
                PlayListItems.Add(new PlayListViewItem() { Name = "TEST" });
                PlaybackTabDataGridView.DataSource = PlayListItems;
                playbackProgressBar.Change += PlaybackProgressBar_Change;

                PlaybackTabLyricsButton.Visible = false;
                PlaybackTabLyricsButton.Click += PlaybackTabLyricsButton_Click;

                PlaybackTabRatting.RateChanged += PlaybackTabRatting_RateChanged;

                Player.PlaylistChanged += Player_PlaylistChanged;
                Player.PositionChanged += Player_PositionChanged;
                Player.LengthChanged += Player_LengthChanged;
                Player.PlaylistPositionChanged += Player_PlaylistPositionChanged;
                Player.PlayStoped += Player_PlayStoped;

                Translate();
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace); }

            LoadTimer.Tick += LoadTimer_Tick;
            LoadTimer.Interval = 1000;
            LoadTimer.Start();

            customThumbnail = new TabbedThumbnail(this.Handle, this.Handle);
            IntPtr Hicon1 = Properties.Resources.previous_24.GetHicon();
            ThumbnailIconPrev = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(Hicon1).Clone();
            DestroyIcon(Hicon1);

            IntPtr Hicon2 = Properties.Resources.play_24.GetHicon();
            ThumbnailIconPlay = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(Hicon2).Clone();
            DestroyIcon(Hicon2);

            IntPtr Hicon3 = Properties.Resources.pause_24.GetHicon();
            ThumbnailIconPause = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(Hicon3).Clone();
            DestroyIcon(Hicon3);

            IntPtr Hicon4 = Properties.Resources.next_24.GetHicon();
            ThumbnailIconNext = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(Hicon4).Clone();
            DestroyIcon(Hicon4);

            buttonPrev = new ThumbnailToolBarButton(ThumbnailIconPrev, "test");
            buttonPrev.Click += (object sender, ThumbnailButtonClickedEventArgs e) => { Player.PlaylistPrevious(); };
            buttonPlay = new ThumbnailToolBarButton((Player.LatestPlayerStatus == PlayerStatus.Play) ? ThumbnailIconPlay : ThumbnailIconPause, "IconPlay");
            buttonPlay.Click += (object sender, ThumbnailButtonClickedEventArgs e) => { 
                if (Player.IsPlaying()) { Player.Pause(); } else { Player.Play(); }
                BtnPlayPause.BackgroundImage = (Player.LatestPlayerStatus == PlayerStatus.Play) ? IconPlay : IconPause;
                buttonPlay.Icon = (Player.LatestPlayerStatus == PlayerStatus.Play) ? ThumbnailIconPlay : ThumbnailIconPause;
            };
            buttonNext = new ThumbnailToolBarButton(ThumbnailIconNext, "IconNext");
            buttonNext.Click += (object sender, ThumbnailButtonClickedEventArgs e) => { Player.PlaylistNext(); };
            TaskbarManager.Instance.ThumbnailToolBars.AddButtons(this.Handle, new ThumbnailToolBarButton[] { buttonPrev, buttonPlay, buttonNext });
            customThumbnail.SetWindowIcon(Properties.Resources.icon_large);
        }

        private void PlaybackTabRatting_RateChanged(double value)
        {
            Debug.WriteLine("New Ratting = " + value);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        #region MainWindow Events
        private void LoadTimer_Tick(object? sender, EventArgs e)
        {
            if (LoadTimer == null) { return; }
            SettingsManagment.Init(this);
            LibraryLoadOldPlaylist();
            LoadTimer.Dispose();
            LoadTimer = null;
        }

        private void MainWindow2_SizeChanged(object sender, EventArgs e)
        {
            TabControler.TabSize = new System.Drawing.Size((TabControler.Width) / TabControler.Tabs.Count, 50);
            if (Width > 800 && Height > 700)
            { PlaybackTabMainTableLayoutPanel.ColumnStyles[0].Width = 250; PlaybackTabLeftTableLayoutPanel.RowStyles[0].Height = 250; }
            else
            { PlaybackTabMainTableLayoutPanel.ColumnStyles[0].Width = 150; PlaybackTabLeftTableLayoutPanel.RowStyles[0].Height = 150; }
            PlaybackTabRatting.Margin = new Padding(Convert.ToInt32(Math.Truncate((PlaybackTabLeftBottomFlowLayoutPanel.Width - PlaybackTabRatting.Width) / 2.0)), 3, 0, 0);
        }

        private void PlaybackProgressBar_Change(object sender, double value)
        {
            long duration = Player.Length();
            Player.Position(null, Convert.ToInt64(Math.Truncate(value * duration / 100)));
        }

        private void PlaybackTabLyricsButton_Click(object sender, EventArgs e)
        {
            if (sender == null) { Debug.WriteLine(" > ER 1 < "); return; }
            if (sender.GetType() != typeof(Button)) { Debug.WriteLine(" > ER 2 < "); return; }
            if (PlaybackTabLyricsButton.Tag == null) { Debug.WriteLine(" > ER 3 < "); return; }
            MediaItem data = PlaybackTabLyricsButton.Tag as MediaItem;
            Lyrics wl = new Lyrics(data);
            wl.Show();
            lyricsWindowsList.Add(wl);
        }

        private void CloseLyricsWindows()
        {
            if (lyricsWindowsList.Count > 0)
                for (int i = lyricsWindowsList.Count - 1; i >= 0; i--)
                { lyricsWindowsList[i].Dispose(); lyricsWindowsList.RemoveAt(i); }
        }
        #endregion

        #region UI Translation
        public void Translate() { AnotherMusicPlayer.MainWindow2.Translation.Translate(this); }
        public void UpdateStyle() { }
        public void AlwaysOnTop(bool val) { this.TopMost = val; }
        private void ReplaceElementDualText(Control ctrl, string text) { ctrl.Text = text; App.SetToolTip(ctrl, text); }
        #endregion

        #region Playback change functions
        private void ChangeDisplayPlaybackPosition(long position, long duration)
        {
            if (this.InvokeRequired) { this.Invoke(() => { ChangeDisplayPlaybackPosition(position, duration); }); return; }
            DisplayPlaybackSize.Text = App.displayTime(duration);
            if (position > 0)
            {
                DisplayPlaybackPosition.Text = App.displayTime(position);
                playbackProgressBar.Value = position * 100 / duration;

                Settings.LastPlaylistDuration = position;
                Settings.SaveSettings();
                if (Settings.AutoCloseLyrics) { CloseLyricsWindows(); }
            }
        }

        private void ChangePlaylistPosition(int position)
        {
            if (this.InvokeRequired) { this.Invoke(() => { ChangePlaylistPosition(position); }); return; }
            if (PlaybackTabDataGridView.Rows.Count <= 0) { return; }
            int pos = position;
            if (pos < 0) { pos = 0; }
            else if (pos >= PlaybackTabDataGridView.Rows.Count) { pos = PlaybackTabDataGridView.Rows.Count - 1; }

            PlaybackTabDataGridView.Rows[pos].Selected = true;
            PlaybackTabDataGridView.FirstDisplayedScrollingRowIndex = pos;
            foreach (PlayListViewItem tm in PlayListItems) { tm.Selected = ""; }
            PlayListViewItem item = ((PlayListViewItem)PlaybackTabDataGridView.Rows[pos].DataBoundItem);
            item.Selected = "■";
            string title = item.Name;
            string arts = item.Artists;
            if (item.Album != null && item.Album.Trim().Length > 0) { title += " - " + item.Album.Trim(); }
            else if (arts != null && arts.Trim().Length > 0) { title += " - " + arts.Trim(); }

            SetTitle(title);
            UpdateLeftPannelMediaInfo(item);
            PlaybackPositionLabel.Text = App.GetTranslation("PlaybackPositionLabel").Replace("%X%", "" + Player.PlayList.Count).Replace("%Y%", "" + (pos+1));

            Settings.LastPlaylistIndex = pos;
            Settings.LastPlaylistDuration = 0;
            Settings.SaveSettings();
            if (Settings.AutoCloseLyrics) { CloseLyricsWindows(); }
        }
        #endregion

        #region Player event functions
        private void Player_PlayStoped(PlayerPositionChangedEventParams e) { ChangeDisplayPlaybackPosition(e.Position, e.duration); }
        private void Player_PositionChanged(PlayerPositionChangedEventParams e) { ChangeDisplayPlaybackPosition(e.Position, e.duration); }
        private void Player_LengthChanged(PlayerLengthChangedEventParams e) { ChangeDisplayPlaybackPosition(-1, e.duration); }

        private void Player_PlaylistPositionChanged(PlayerPlaylistPositionChangeParams e) { ChangePlaylistPosition(e.Position); }

        private void Player_PlaylistChanged(PlayerPlaylistChangeParams e)
        {
            if (this.InvokeRequired) { this.Invoke(() => { Player_PlaylistChanged(e); }); return; }

            PlayListItems.Clear();
            try { foreach (string file in e.playlist) { PlayListItems.Add(PlayListViewItem.FromFilePath(file)); } }
            catch (Exception) { }
            Debug.WriteLine("PlayListItems.Count = " + PlayListItems.Count);
            PlaybackTabDataGridView.DataSource = null;
            PlaybackTabDataGridView.DataSource = PlayListItems;
            ChangePlaylistPosition(PlaylistIndexAtLoading);
        }
        #endregion

        public void SetTitle(string title)
        {
            if (title != null && title.Trim() != "")
            {
                this.Text = title;
                try { customThumbnail.Title = title; } catch { }
                this.TitleLabel.Text = title;
            }
            else
            {
                try { customThumbnail.Title = App.AppName; } catch { }
                this.TitleLabel.Text = App.AppName;
            }
            App.SetToolTip(this.TitleLabel, this.TitleLabel.Text);
        }

        private void LibraryLoadOldPlaylist()
        {
            try
            {
                PlayListItems.Clear();
                Thread objThread = new Thread(new ParameterizedThreadStart(LibraryLoadOldPlaylistP2));
                objThread.IsBackground = true;
                objThread.Priority = ThreadPriority.AboveNormal;
                objThread.Start(null);
            }
            catch { }
        }

        private void LibraryLoadOldPlaylistP2(object param = null)
        {
            Dictionary<string, Dictionary<string, object>> LastPlaylist = App.bdd.DatabaseQuery("SELECT MIndex,Path1,Path2 FROM queue ORDER BY MIndex ASC", "MIndex");
            if (LastPlaylist != null)
            {
                if (LastPlaylist.Count > 0)
                {
                    Debug.WriteLine("Old PlayList detected");
                    //Debug.WriteLine(JsonConvert.SerializeObject(LastPlaylist));
                    List<string> gl = new List<string>();
                    int fails = 0;
                    bool radio = false;
                    foreach (KeyValuePair<string, Dictionary<string, object>> fi in LastPlaylist)
                    {
                        string path1 = (string)fi.Value["Path1"];
                        string path2 = (fi.Value["Path2"] == null) ? null : ((string)fi.Value["Path2"]).Trim();
                        if (path2 != null && path2 != "")
                        {
                            if (System.IO.File.Exists(path2)) { gl.Add(path2); }
                            else
                            {
                                if (System.IO.File.Exists(path1)) { gl.Add(path1); } else { fails += 1; }
                            }
                        }
                        else
                        {
                            if (path1.StartsWith("Radio|"))
                            {
                                Debug.WriteLine(" = = = > RADIO 0000");
                                gl.Clear();
                                gl.Add(path1);
                                radio = true;
                                break;
                            }
                            else
                            {
                                if (System.IO.File.Exists(path1)) { gl.Add(path1); }
                                else { fails += 1; }
                            }
                        }
                    }
                    int newIndex = -1;
                    if (fails > 0) { newIndex = 0; }
                    else { newIndex = PlaylistIndexAtLoading; }

                    if (radio == true)
                    {
                        Debug.WriteLine(" = = = > RADIO");
                        Debug.WriteLine(gl[0]);
                        string[] rtab = gl[0].Split('|');
                        if (rtab[1].Trim() != "")
                        {
                            Dictionary<string, object> CurentRadio = App.bdd.DatabaseQuery("SELECT * FROM radios WHERE RID = " + rtab[1], "RID")[rtab[1]];
                            //Debug.WriteLine(JsonConvert.SerializeObject(CurentRadio));
                            Player.OpenStream(CurentRadio["Url"] as string, (CurentRadio["FType"] as string == "M3u") ? RadioPlayer.RadioType.M3u : RadioPlayer.RadioType.Stream, CurentRadio["RID"] as string, CurentRadio["Name"] as string, Settings.StartUpPlay, CurentRadio["UrlPrefix"] as string);
                        }
                        else { Player.OpenStream(gl[0], RadioPlayer.RadioType.Stream, "", "", Settings.StartUpPlay, ""); }
                    }
                    else { Open(gl.ToArray(), false, false, newIndex, Settings.StartUpPlay, Settings.LastPlaylistDuration); }
                    //player.Stop();
                }
            }
        }

        private bool Open(string[] files, bool replace = false, bool random = false, int playIndex = 0, bool autoplay = false, long statupDuration = 0)
        {
            Debug.WriteLine("--> Open <--");
            if (replace == true) { Player.PlaylistClear(); }
            return Player.PlaylistEnqueue(files, random, playIndex, statupDuration, autoplay);
        }

        private void UpdateLeftPannelMediaInfo(PlayListViewItem item)
        {
            Dictionary<string, Dictionary<string, object>> data = null;
            string[] rtab = null;
            try
            {
                string path = item.Path;
                if (path == null)
                {
                    if (Player.PlayList.Count > 0) { path = Player.PlayList[Player.Index]; }
                    Debug.WriteLine("--> path = '" + path + "' <--");
                }

                MediaItem item2 = null;
                if (path.StartsWith("Radio|"))
                {
                    rtab = path.Split('|');
                    if (rtab[1].Trim() != "")
                    {
                        data = App.bdd.DatabaseQuery("SELECT * FROM radios WHERE RID = " + rtab[1], "RID");
                        item.Name = data["" + rtab[1].Trim()]["Name"] as string;
                    }
                }
                else
                {
                    item2 = FilesTags.MediaInfo(item.Path, false);
                }

                BitmapImage bi = null;
                if (item.Path.StartsWith("Radio|"))
                {
                    string logo = data["" + rtab[1].Trim()]["Logo"] as string;
                    string[] logoTab = logo.Split(',');
                    if (logoTab.Length > 1) { logo = logoTab[1]; }
                    try { bi = BitmapMagic.Base64StringToBitmap(logo); }
                    catch (Exception err)
                    {
                        Debug.WriteLine("data[rtab[1].Trim()][\"Logo\"] = " + logo);
                        Debug.WriteLine(JsonConvert.SerializeObject(err));
                    }
                }
                else { bi = FilesTags.MediaPicture(item.Path, App.bdd, true, (Settings.MemoryUsage == 0) ? 150 : 250, (Settings.MemoryUsage == 0) ? 150 : 250); }

                if (bi == null) { FileCover.BackgroundImage = Properties.Resources.album_large; }
                else { FileCover.BackgroundImage = App.BitmapImage2Bitmap(bi); }

                PlaybackTabTitleLabelValue.Text = item.Name;
                if (item.Album.Trim().Length > 0)
                { PlaybackTabAlbumLabelInfo.Visible = true; PlaybackTabAlbumLabelValue.Visible = true; ReplaceElementDualText(PlaybackTabAlbumLabelValue, item.Album); }
                else { PlaybackTabAlbumLabelInfo.Visible = false; PlaybackTabAlbumLabelValue.Visible = false; }

                if (item.Artists.Trim().Length > 0)
                { PlaybackTabArtistsLabelInfo.Visible = true; PlaybackTabArtistsLabelValue.Visible = true; ReplaceElementDualText(PlaybackTabArtistsLabelValue, item.Artists); }
                else { PlaybackTabArtistsLabelInfo.Visible = false; PlaybackTabArtistsLabelValue.Visible = false; }

                if (path.StartsWith("Radio|"))
                { PlaybackTabDurationLabelInfo.Visible = false; PlaybackTabDurationLabelValue.Visible = false; }
                else { PlaybackTabDurationLabelInfo.Visible = true; PlaybackTabDurationLabelValue.Visible = true; PlaybackTabDurationLabelValue.Text = item.DurationS; }

                if (!path.StartsWith("Radio|") && item2.Lyrics != null && item2.Lyrics.Trim().Length > 0)
                { PlaybackTabLyricsButton.Visible = true; PlaybackTabLyricsButton.Tag = item2; }
                else { PlaybackTabLyricsButton.Visible = false; }

                if (!path.StartsWith("Radio|")) { PlaybackTabRatting.Visible = true; PlaybackTabRatting.Rate = item2.Rating; } else { PlaybackTabRatting.Visible = false; }
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace); }
        }


        #region Generic Window Button
        public void MinimizeButton_Click(object? sender, EventArgs? e) { WindowState = FormWindowState.Minimized; }
        public void MaximizeButton_Click(object? sender, EventArgs? e)
        {
            if (WindowState == FormWindowState.Maximized) { this.MaximumSize = new System.Drawing.Size(0, 0); WindowState = FormWindowState.Normal; }
            else
            {
                Screen screen = Screen.FromControl(this);
                int x = screen.WorkingArea.X - screen.Bounds.X;
                int y = screen.WorkingArea.Y - screen.Bounds.Y;
                this.MaximizedBounds = new Rectangle(x, y, screen.WorkingArea.Width, screen.WorkingArea.Height);
                this.MaximumSize = screen.WorkingArea.Size;
                WindowState = FormWindowState.Maximized;
            }
        }
        public void CloseButton_Click(object? sender, EventArgs? e) { Close(); }
        #endregion

        #region Window displasment gestion
        private Dictionary<string, bool> draggings = new Dictionary<string, bool>();
        private Dictionary<string, System.Drawing.Point> dragCursorPoints = new Dictionary<string, System.Drawing.Point>();
        private Dictionary<string, System.Drawing.Point> dragFormPoints = new Dictionary<string, System.Drawing.Point>();
        private Dictionary<string, Form> dragForms = new Dictionary<string, Form>();

        private void FormDragable_InitTab(object sender, bool active)
        {
            try
            {
                TableLayoutPanel label1 = (TableLayoutPanel)sender;
                string label = label1.Tag as string;
                Control parent = label1.Parent;
                while (parent.GetType().Name == "TableLayoutPanel") { parent = parent.Parent; }

                draggings.Add(label, active);
                dragCursorPoints.Add(label, System.Windows.Forms.Cursor.Position);
                dragFormPoints.Add(label, label1.Location);
                dragForms.Add(label, (Form)parent);
            }
            catch (Exception) { }
        }

        public void FormDragable_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (sender == null) { return; }
            while (sender.GetType().Name != "TableLayoutPanel") { sender = ((Control)sender).Parent; }
            string label = ((TableLayoutPanel)sender).Tag as string;
            if (!draggings.ContainsKey(label)) { FormDragable_InitTab(sender, true); }
            else
            {
                draggings[label] = true;
                dragCursorPoints[label] = System.Windows.Forms.Cursor.Position;
                dragFormPoints[label] = dragForms[label].Location;
            }
        }

        public void FormDragable_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                if (sender == null) { return; }
                while (sender.GetType().Name != "TableLayoutPanel") { sender = ((Control)sender).Parent; }
                string label = ((TableLayoutPanel)sender).Tag as string;
                if (!draggings.ContainsKey(label)) { FormDragable_InitTab(sender, false); }
                if (draggings[label])
                {
                    System.Drawing.Point dif = System.Drawing.Point.Subtract(System.Windows.Forms.Cursor.Position, new System.Drawing.Size(dragCursorPoints[label]));
                    dragForms[label].Location = System.Drawing.Point.Add(dragFormPoints[label], new System.Drawing.Size(dif));
                }
            }
            catch (Exception) { }
        }

        public void FormDragable_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (sender == null) { return; }
            while (sender.GetType().Name != "TableLayoutPanel") { sender = ((Control)sender).Parent; }
            string label = ((TableLayoutPanel)sender).Tag as string;
            draggings[label] = false;
        }

        public void FormDragable_Clear(string id)
        {
            if (draggings.ContainsKey(id))
            {
                draggings.Remove(id);
                dragCursorPoints.Remove(id);
                dragFormPoints.Remove(id);
                dragForms.Remove(id);
            }
        }
        #endregion

        #region Window resize gestion
        private bool IsResizing = false;
        private int ResizePosX = 0;
        private int ResizePosY = 0;
        private int ResizeSizeW = 0;
        private int ResizeSizeH = 0;

        private void SizerInitTab(object sender, bool active, int x, int y)
        {
            try
            {
                System.Windows.Forms.Button label1 = (System.Windows.Forms.Button)sender;
                string label = label1.Tag as string;
                if (IsResizing) { return; }
                IsResizing = true;
                ResizePosX = x;
                ResizePosY = y;
                ResizeSizeW = this.Width;
                ResizeSizeH = this.Height;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void SizerMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                string label = ((System.Windows.Forms.Button)sender).Tag as string;
                if (!IsResizing) { SizerInitTab(sender, true, Cursor.Position.X, Cursor.Position.Y); }
                else
                {
                    IsResizing = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        public void SizerMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                Debug.WriteLine("SizerMouseMove");
                if (!IsResizing) { return; }
                if (IsResizing)
                {
                    Debug.WriteLine("Calculate");
                    this.Width = Cursor.Position.X - ResizePosX + ResizeSizeW;
                    this.Height = Cursor.Position.Y - ResizePosY + ResizeSizeH;


                    Debug.WriteLine("ResizeSizeW = " + ResizeSizeW);
                    Debug.WriteLine("ResizeSizeH = " + ResizeSizeH);
                    Debug.WriteLine("Width = " + Width);
                    Debug.WriteLine("Height = " + Height);

                    this.ResizeRedraw = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        public void SizerMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try { IsResizing = false; }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void SizerClear(string id)
        {
            if (IsResizing) { IsResizing = false; }
        }

        #endregion


    }
}
