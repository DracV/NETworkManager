﻿using NETworkManager.Models.Network;
using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.ObjectModel;
using NETworkManager.Utilities;
using System.Windows;
using NETworkManager.Models.Settings;
using System.Windows.Threading;
using System.Linq;

namespace NETworkManager.ViewModels
{
    public class ListenersViewModel : ViewModelBase
    {
        #region Variables
        private readonly bool _isLoading;
        private readonly DispatcherTimer _autoRefreshTimer = new DispatcherTimer();

        private string _search;
        public string Search
        {
            get => _search;
            set
            {
                if (value == _search)
                    return;

                _search = value;

                ListenersView.Refresh();

                OnPropertyChanged();
            }
        }

        private ObservableCollection<ListenerInfo> _listeners = new ObservableCollection<ListenerInfo>();
        public ObservableCollection<ListenerInfo> Listeners
        {
            get => _listeners;
            set
            {
                if (value == _listeners)
                    return;

                _listeners = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView ListenersView { get; }

        private ListenerInfo _selectedListenerInfo;
        public ListenerInfo SelectedListenerInfo
        {
            get => _selectedListenerInfo;
            set
            {
                if (value == _selectedListenerInfo)
                    return;

                _selectedListenerInfo = value;
                OnPropertyChanged();
            }
        }

        private bool _autoRefresh;
        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (value == _autoRefresh)
                    return;

                if (!_isLoading)
                    SettingsManager.Current.Listeners_AutoRefresh = value;

                _autoRefresh = value;

                // Start timer to refresh automatically
                if (!_isLoading)
                {
                    if (value)
                        StartAutoRefreshTimer();
                    else
                        StopAutoRefreshTimer();
                }

                OnPropertyChanged();
            }
        }

        public ICollectionView AutoRefreshTimes { get; }

        private AutoRefreshTimeInfo _selectedAutoRefreshTime;
        public AutoRefreshTimeInfo SelectedAutoRefreshTime
        {
            get => _selectedAutoRefreshTime;
            set
            {
                if (value == _selectedAutoRefreshTime)
                    return;

                if (!_isLoading)
                    SettingsManager.Current.Listeners_AutoRefreshTime = value;

                _selectedAutoRefreshTime = value;

                if (AutoRefresh)
                    ChangeAutoRefreshTimerInterval(AutoRefreshTime.CalculateTimeSpan(value));

                OnPropertyChanged();
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (value == _isRefreshing)
                    return;

                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        private bool _displayStatusMessage;
        public bool DisplayStatusMessage
        {
            get => _displayStatusMessage;
            set
            {
                if (value == _displayStatusMessage)
                    return;

                _displayStatusMessage = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (value == _statusMessage)
                    return;

                _statusMessage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Contructor, load settings
        public ListenersViewModel()
        {
            _isLoading = true;

            ListenersView = CollectionViewSource.GetDefaultView(Listeners);
            ListenersView.SortDescriptions.Add(new SortDescription(nameof(ListenerInfo.Protocol), ListSortDirection.Ascending));
            ListenersView.SortDescriptions.Add(new SortDescription(nameof(ListenerInfo.IPAddressInt32), ListSortDirection.Ascending));
            ListenersView.Filter = o =>
            {

                if (!(o is ListenerInfo info))
                    return false;

                if (string.IsNullOrEmpty(Search))
                    return true;

                var filter = Search.Replace(" ", "").Replace("-", "").Replace(":", "");

                // Search by IP Address, Port and Protocol
                return info.IPAddress.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) > -1 || info.Port.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) > -1 || info.Protocol.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) > -1;
            };

            AutoRefreshTimes = CollectionViewSource.GetDefaultView(AutoRefreshTime.Defaults);
            SelectedAutoRefreshTime = AutoRefreshTimes.SourceCollection.Cast<AutoRefreshTimeInfo>().FirstOrDefault(x => (x.Value == SettingsManager.Current.Listeners_AutoRefreshTime.Value && x.TimeUnit == SettingsManager.Current.Listeners_AutoRefreshTime.TimeUnit));

            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            LoadSettings();

            _isLoading = false;

            Refresh();

            if (AutoRefresh)
                StartAutoRefreshTimer();
        }

        private void LoadSettings()
        {
            AutoRefresh = SettingsManager.Current.Listeners_AutoRefresh;
        }
        #endregion

        #region ICommands & Actions
        public ICommand RefreshCommand
        {
            get { return new RelayCommand(p => RefreshAction()); }
        }

        private void RefreshAction()
        {
            DisplayStatusMessage = false;

            Refresh();
        }

        public ICommand CopySelectedProtocolCommand
        {
            get { return new RelayCommand(p => CopySelectedProtocolAction()); }
        }

        private void CopySelectedProtocolAction()
        {
            Clipboard.SetText(SelectedListenerInfo.Protocol.ToString());
        }
        
        public ICommand CopySelectedIPAddressCommand
        {
            get { return new RelayCommand(p => CopySelectedIPAddressAction()); }
        }

        private void CopySelectedIPAddressAction()
        {
            Clipboard.SetText(SelectedListenerInfo.IPAddress.ToString());
        }

        public ICommand CopySelectedPortCommand
        {
            get { return new RelayCommand(p => CopySelectedPortAction()); }
        }

        private void CopySelectedPortAction()
        {
            Clipboard.SetText(SelectedListenerInfo.Port.ToString());
        }
        #endregion

        #region Methods
        private async void Refresh()
        {
            IsRefreshing = true;

            Listeners.Clear();

            (await Listener.GetAllActiveListenersAsync()).ForEach(x => Listeners.Add(x));

            IsRefreshing = false;
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        private void ChangeAutoRefreshTimerInterval(TimeSpan timeSpan)
        {
            _autoRefreshTimer.Interval = timeSpan;
        }

        private void StartAutoRefreshTimer()
        {
            ChangeAutoRefreshTimerInterval(AutoRefreshTime.CalculateTimeSpan(SelectedAutoRefreshTime));

            _autoRefreshTimer.Start();
        }

        private void StopAutoRefreshTimer()
        {
            _autoRefreshTimer.Stop();
        }
        #endregion
    }
}