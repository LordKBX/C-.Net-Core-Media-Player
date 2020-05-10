﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace AnotherMusicPlayer
{
    public partial class MainWindow : Window
    {

        private void Open_Button_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            win1.IsEnabled = false;
            bool DoConv = false;
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.Filter = "Audio (*.AAC;*.FLAC;*.MP3;*.OGG;*.WMA)|*.AAC;*.FLAC;*.MP3;*.OGG;*.WMA";
            openFileDlg.Multiselect = true;
            openFileDlg.Title = "File Selection";
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                //if (player.IsPlaying()) { player.Stop(); }
                DoConv = Open(openFileDlg.FileNames);
            }
            if (DoConv == false) { Mouse.OverrideCursor = null; win1.IsEnabled = true; }
            else {
                
            }
        }

        private bool Open(string[] files)
        {
            bool doConv = false;
            int startIndex = PlayListIndex;

            if (files.Length > 0)
            {
                if (!player.IsPlaying()) {
                    if (MediaTestFileExtention(files[0]) == false) { doConv = true; }
                }
                if (!player.IsPlaying() && MediaTestFileExtention(files[0]) == false) { doConv = true; }
                
                string NewFile;
                for (int i = 0; i < files.Length; i++)
                {
                    NewFile = null;
                    if (MediaTestFileExtention(files[i]) == false)
                    {
                        if (Settings.ConversionMode == 1) { NewFile = Path.GetTempPath() + Path.ChangeExtension(Path.GetFileName(files[i]), ".mp3"); }
                        else { NewFile = Path.ChangeExtension(files[i], ".mp3"); }

                        if (i == 0) {
                            doConv = true;
                            player.Conv(files[i], NewFile, (Settings.ConversionMode == 1)?false:true);
                        }
                        else { player.Conv(files[i], NewFile, (Settings.ConversionMode == 1) ? false : true); }
                        //LoadFileAsync(files[i]);
                    }
                    string[] tmp = new string[] { files[i], NewFile };

                    if (!PlayList.Contains(tmp)) { PlayList.Add(tmp); }
                }
            }
            Timer_PlayListIndex = -1;

            if (PlayListIndex < 0)  { 
                PlayListIndex = 0;
            }
            if (!player.IsPlaying()) { fileOpen(PlayList[PlayListIndex][(PlayList[PlayListIndex][1] == null) ? 0 : 1]); }
            return doConv;
        }

        private async void ConvAndPlay(string FileInput, string FileOutput)
        {
            await player.Conv(FileInput, FileOutput, (Settings.ConversionMode == 1) ? false : true);
            Dispatcher.BeginInvoke(new Action(() => {
                try { fileOpen(FileInput); }
                catch{ }
            }));
        }


        private void Play_Button_Click(object sender, RoutedEventArgs e) { Pause(); }
        private void Previous_Button_Click(object sender, RoutedEventArgs e) { PreviousTrack(); }
        private void Next_Button_Click(object sender, RoutedEventArgs e) { NextTrack(); }

        private void PositionMoins10_Button_Click(object sender, RoutedEventArgs e) { player.Position(null, player.Position() - 10000); }
        private void PositionPlus10_Button_Click(object sender, RoutedEventArgs e) { player.Position(null, player.Position() + 10000); }

        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            player.StopAll();
            PlayList.Clear();
            PlayListIndex = -1;

            PlayItemNameValue.ToolTip = PlayItemNameValue.Text = "";
            PlayItemAlbumValue.ToolTip = PlayItemAlbumValue.Text = "";
            PlayItemArtistsValue.ToolTip = PlayItemArtistsValue.Text = "";
            PlayItemDurationValue.ToolTip = PlayItemDurationValue.Text = "";
            FileCover.Source = Bimage("CoverImg");
        }

        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            List<string[]> tmpList = new List<string[]>();
            List<int> pasts = new List<int>();
            Random rnd = new Random();
            string currentFile = (PlayListIndex > -1) ? PlayList[PlayListIndex][0] : null;
            int newIndex = PlayListIndex;

            int index = 0;
            while (tmpList.Count < PlayList.Count)
            {
                index = rnd.Next(0, PlayList.Count);
                if (pasts.Contains(index)) { continue; }
                tmpList.Add(PlayList[index]);
                pasts.Add(index);
                if (PlayList[index][0] == currentFile) { newIndex = tmpList.Count -1; }
            }

            PlayList = tmpList;
            PlayListIndex = newIndex;
            Timer_PlayListIndex = -1;
        }

        private void BtnRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (PlayRepeatStatus <= 0) { PlayRepeatStatus = 1; player.Repeat(true); }
            else if (PlayRepeatStatus == 1) { PlayRepeatStatus = 2; player.Repeat(false); }
            else { PlayRepeatStatus = 0; player.Repeat(false); }
        }

        private void ParamsLibFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = OpenFolder();
            if (path != null && path != Settings.LibFolder)
            {
                ParamsLibFolderTextBox.Text = path;
                Settings.LibFolder = path;
                Settings.SaveSettings();
                MediatequeInvokeScan(true);
            }
        }

        private string OpenFolder()
        {
            string path = null;
            ResourceDictionary res1 = Resources.MergedDictionaries[1];
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = Settings.LibFolder; // Use current value for initial dir
            dialog.Title = (string)res1["ParamsLibFolderSelectorTitle"]; // instead of default "Save As"
            dialog.Filter = ((string)res1["ParamsLibFolderSelectorBlockerTitle"]) + "|*." + ((string)res1["ParamsLibFolderSelectorBlockerType"]); // Prevents displaying files
            dialog.FileName = (string)res1["ParamsLibFolderSelectorBlockerName"]; // Filename will then be "select.this.directory"
            if (dialog.ShowDialog() == true)
            {
                path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\"+((string)res1["ParamsLibFolderSelectorBlockerName"]) +"."+((string)res1["ParamsLibFolderSelectorBlockerType"]), "");
                path = path.Replace("."+ ((string)res1["ParamsLibFolderSelectorBlockerType"]), "");
                // If user has changed the filename, create the new directory
                if (!System.IO.Directory.Exists(path)) { return null; }
                // Our final value is in path
                //Debug.WriteLine(path);
            }
            return path;
        }

    }
}