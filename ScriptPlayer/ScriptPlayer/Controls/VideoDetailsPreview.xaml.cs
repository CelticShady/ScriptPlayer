﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Controls
{
    /// <summary>
    /// Interaction logic for VideoDetailsPreview.xaml
    /// </summary>
    public partial class VideoDetailsPreview : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(VideoDetailsPreview), new PropertyMetadata(default(MainViewModel), OnViewModelPropertyChanged));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty EntryProperty = DependencyProperty.Register(
            "Entry", typeof(PlaylistEntry), typeof(VideoDetailsPreview), new PropertyMetadata(default(PlaylistEntry), OnEntryPropertyChanged));

        private bool _currentEntryLoaded;

        private static void OnEntryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoDetailsPreview) d).OnEntryChanged();
        }

        private void OnEntryChanged()
        {
            player.Close();
            heatMap.Source = null;
            text.Text = "";
            _currentEntryLoaded = false;

            RecheckLoadInfo();

        }

        public PlaylistEntry Entry
        {
            get { return (PlaylistEntry)GetValue(EntryProperty); }
            set { SetValue(EntryProperty, value); }
        }

        private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoDetailsPreview)d).RecheckLoadInfo();
        }

        public VideoDetailsPreview()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            InitializeComponent();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("UNLOADED");
            player.Close();
        }

        private void OnLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            Debug.WriteLine("LOADED");
            RecheckLoadInfo();
        }

        private void RecheckLoadInfo()
        {
            if (Entry == null)
                return;

            if (string.IsNullOrEmpty(Entry.Fullname))
                return;

            if (ViewModel == null)
                return;

            if (!IsLoaded)
                return;

            if (_currentEntryLoaded)
                return;

            LoadMetadata();
        }

        private void LoadMetadata()
        {
            FindRelatedFile(Entry.Fullname, "png", LoadHeatmapImage);
            FindRelatedFile(Entry.Fullname, "gif", LoadPreview);

            text.Text = Entry.Shortname + " [" + (Entry.Duration?.ToString("hh\\:mm\\:ss") ?? "?") + "]";

            _currentEntryLoaded = true;
        }

        private void LoadPreview(string path)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(new Action(() => { player.Load(path); }));
            else
                player.Load(path);
        }

        private void FindRelatedFile(string path, string extension, Action<string> action)
        {
            string foundPath = ViewModel.GetRelatedFile(path, new[] { extension });
            if (string.IsNullOrEmpty(foundPath))
                return;

            action(foundPath);
        }

        public void LoadHeatmapImage(string path)
        {
            new Thread(() =>
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                if (!Dispatcher.CheckAccess())
                    Dispatcher.BeginInvoke(new Action(() => { heatMap.Source = image; }));
                else
                    heatMap.Source = image;
            }).Start();
        }

        protected override void OnInitialized(EventArgs e)
        {
            Debug.WriteLine("ON INITIALIZED");
            base.OnInitialized(e);
        }
    }
}
