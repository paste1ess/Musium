using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Musium.Models;
using Musium.Pages;
using Musium.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;

namespace Musium
{
    public sealed partial class MainWindow : Window
    {
        private readonly AudioService Audio = AudioService.Instance;
        public static NavigationView MainNavView;
        public static Frame RootNavFrame;
        public MainWindow()
        {
            InitializeComponent();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            appWindow.SetIcon("Assets/Icon.ico");

            MainNavView = RootNavigationView;
            RootNavFrame = RootFrame;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(Titlebar);

            UpdateNavigationViewSelection(typeof(Musium.Pages.NowPlaying));

            Audio.SetDispatcherQueue(DispatcherQueue);
            Audio.SetMediaPlayer(AudioPlayerElement);

            Audio.PropertyChanged += Audio_PropertyChanged;
            Closed += MainWindow_Closed;
            UpdateTitle();
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            Audio.PropertyChanged -= Audio_PropertyChanged;
            AudioService.Instance.Dispose();
        }

        private void Audio_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Audio.CurrentSongPlaying))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateTitle();
                });
            }
        }

        private void UpdateTitle()
        {
            if (Audio.CurrentSongPlaying is Song currentSong)
            {
                Titlebar.Title = "Playing: " + currentSong.Title + " - " + currentSong.Album.Artist.Name;
            }
            else
            {
                Titlebar.Title = "Musium";
            }
        }
        public static void UpdateNavigationViewSelection(Type pageType)
        {
            RootNavFrame.Navigate(pageType);
            var itemToSelect = MainNavView.MenuItems
                                      .OfType<NavigationViewItem>()
                                      .FirstOrDefault(item => item.Tag?.ToString() == pageType.FullName);
            MainNavView.SelectedItem = itemToSelect;
        }

        private void rootNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                rootNav_Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                rootNav_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }
        private void rootNav_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            Type preNavPageType = RootFrame.CurrentSourcePageType;

            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                RootFrame.Navigate(navPageType, null, transitionInfo);
            }
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigationView.IsPaneOpen = !RootNavigationView.IsPaneOpen;
        }
    }
}
