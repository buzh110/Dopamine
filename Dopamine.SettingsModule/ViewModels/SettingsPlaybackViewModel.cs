﻿using System;
using Digimezzo.Utilities.Helpers;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Dopamine.Common.Services.Dialog;
using Dopamine.Common.Services.Notification;
using Dopamine.Common.Services.Playback;
using Dopamine.Common.Services.Taskbar;
using Dopamine.Common.Base;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CSCore.CoreAudioAPI;
using Dopamine.Common.Audio;

namespace Dopamine.SettingsModule.ViewModels
{
    public class SettingsPlaybackViewModel : BindableBase
    {
        #region Variables
        private ObservableCollection<NameValue> latencies;
        private NameValue selectedLatency;
        private IPlaybackService playbackService;
        private ITaskbarService taskbarService;
        private INotificationService notificationService;
        private IDialogService dialogService;
        private bool checkBoxWasapiExclusiveModeChecked;
        private bool checkBoxShowNotificationWhenPlayingChecked;
        private bool checkBoxShowNotificationWhenPausingChecked;
        private bool checkBoxShowNotificationWhenResumingChecked;
        private bool checkBoxShowNotificationControlsChecked;
        private bool checkBoxShowProgressInTaskbarChecked;
        private bool checkBoxShowNotificationOnlyWhenPlayerNotVisibleChecked;
        private ObservableCollection<NameValue> notificationPositions;
        private NameValue selectedNotificationPosition;
        private ObservableCollection<int> notificationSeconds;
        private int selectedNotificationSecond;
        private ObservableCollection<MMDevice> outputDevices;
        private MMDevice selectedOutputDevice;
        #endregion

        #region Commands
        public DelegateCommand ShowTestNotificationCommand { get; set; }
        #endregion

        #region Properties
        public bool IsNotificationEnabled
        {
            get { return this.CheckBoxShowNotificationWhenPlayingChecked | this.CheckBoxShowNotificationWhenPausingChecked | this.CheckBoxShowNotificationWhenResumingChecked; }
        }

        public ObservableCollection<NameValue> Latencies
        {
            get { return this.latencies; }
            set { SetProperty<ObservableCollection<NameValue>>(ref this.latencies, value); }
        }

        public NameValue SelectedLatency
        {
            get { return this.selectedLatency; }
            set
            {
                SettingsClient.Set<int>("Playback", "AudioLatency", value.Value);
                SetProperty<NameValue>(ref this.selectedLatency, value);

                if (this.playbackService != null)
                {
                    this.playbackService.Latency = value.Value;
                }
            }
        }

        public bool CheckBoxWasapiExclusiveModeChecked
        {
            get { return this.checkBoxWasapiExclusiveModeChecked; }
            set
            {
                if (value)
                {
                    this.ConfirmEnableExclusiveMode();
                }
                else
                {
                    this.ApplyExclusiveMode(false);
                }
            }
        }

        public bool CheckBoxShowNotificationWhenPlayingChecked
        {
            get { return this.checkBoxShowNotificationWhenPlayingChecked; }
            set
            {
                SettingsClient.Set<bool>("Behaviour", "ShowNotificationWhenPlaying", value);
                SetProperty<bool>(ref this.checkBoxShowNotificationWhenPlayingChecked, value);
                OnPropertyChanged(() => this.IsNotificationEnabled);
            }
        }

        public bool CheckBoxShowNotificationWhenPausingChecked
        {
            get { return this.checkBoxShowNotificationWhenPausingChecked; }
            set
            {
                SettingsClient.Set<bool>("Behaviour", "ShowNotificationWhenPausing", value);
                SetProperty<bool>(ref this.checkBoxShowNotificationWhenPausingChecked, value);
                OnPropertyChanged(() => this.IsNotificationEnabled);
            }
        }

        public bool CheckBoxShowNotificationWhenResumingChecked
        {
            get { return this.checkBoxShowNotificationWhenResumingChecked; }
            set
            {
                SettingsClient.Set<bool>("Behaviour", "ShowNotificationWhenResuming", value);
                SetProperty<bool>(ref this.checkBoxShowNotificationWhenResumingChecked, value);
                OnPropertyChanged(() => this.IsNotificationEnabled);
            }
        }

        public bool CheckBoxShowNotificationControlsChecked
        {
            get { return this.checkBoxShowNotificationControlsChecked; }
            set
            {
                SettingsClient.Set<bool>("Behaviour", "ShowNotificationControls", value);
                SetProperty<bool>(ref this.checkBoxShowNotificationControlsChecked, value);
            }
        }

        public bool CheckBoxShowProgressInTaskbarChecked
        {
            get { return this.checkBoxShowProgressInTaskbarChecked; }
            set
            {
                SettingsClient.Set<bool>("Playback", "ShowProgressInTaskbar", value);
                SetProperty<bool>(ref this.checkBoxShowProgressInTaskbarChecked, value);

                if (this.taskbarService != null && this.playbackService != null)
                {
                    this.taskbarService.SetShowProgressInTaskbar(value);
                }
            }
        }

        public bool CheckBoxShowNotificationOnlyWhenPlayerNotVisibleChecked
        {
            get { return this.checkBoxShowNotificationOnlyWhenPlayerNotVisibleChecked; }
            set
            {
                SettingsClient.Set<bool>("Behaviour", "ShowNotificationOnlyWhenPlayerNotVisible", value);
                SetProperty<bool>(ref this.checkBoxShowNotificationOnlyWhenPlayerNotVisibleChecked, value);
            }
        }

        public ObservableCollection<NameValue> NotificationPositions
        {
            get { return this.notificationPositions; }
            set { SetProperty<ObservableCollection<NameValue>>(ref this.notificationPositions, value); }
        }

        public NameValue SelectedNotificationPosition
        {
            get { return this.selectedNotificationPosition; }
            set
            {
                SettingsClient.Set<int>("Behaviour", "NotificationPosition", value.Value);
                SetProperty<NameValue>(ref this.selectedNotificationPosition, value);
            }
        }

        public ObservableCollection<int> NotificationSeconds
        {
            get { return this.notificationSeconds; }
            set { SetProperty<ObservableCollection<int>>(ref this.notificationSeconds, value); }
        }

        public int SelectedNotificationSecond
        {
            get { return this.selectedNotificationSecond; }
            set
            {
                SettingsClient.Set<int>("Behaviour", "NotificationAutoCloseSeconds", value);
                SetProperty<int>(ref this.selectedNotificationSecond, value);
            }
        }

        public ObservableCollection<MMDevice> OutputDevices
        {
            get => this.outputDevices;
            set => SetProperty<ObservableCollection<MMDevice>>(ref this.outputDevices, value);
        }

        public MMDevice SelectedOutputDevice
        {
            get => this.selectedOutputDevice;
            set
            {
                SetProperty<MMDevice>(ref this.selectedOutputDevice, value);
                this.playbackService.SetCurrentOutputDeviceAsync(value);
            }
        }
        #endregion

        #region Construction
        public SettingsPlaybackViewModel(IPlaybackService playbackService, ITaskbarService taskbarService, INotificationService notificationService, IDialogService dialogService)
        {
            this.playbackService = playbackService;
            this.taskbarService = taskbarService;
            this.notificationService = notificationService;
            this.dialogService = dialogService;

            ShowTestNotificationCommand = new DelegateCommand(() => this.notificationService.ShowNotificationAsync());

            this.GetCheckBoxesAsync();
            this.GetNotificationPositionsAsync();
            this.GetNotificationSecondsAsync();
            this.GetLatenciesAsync();
            this.GetOutputDevicesAsync();
        }
        #endregion

        #region Private
        private async void GetOutputDevicesAsync()
        {
            await Task.Run(async () =>
            {
                var outputDevices = new ObservableCollection<MMDevice>();
                foreach (var device in await this.playbackService.GetAllOutputDevicesAsync())
                {
                    outputDevices.Add(device);
                }
                Application.Current.Dispatcher.Invoke(new Action(() => this.OutputDevices = outputDevices));

                var currentDevice = await this.playbackService.GetCurrentOutputDeviceAsync();
                await Application.Current.Dispatcher.InvokeAsync(
                    new Action(() => this.SelectedOutputDevice = outputDevices.First(d => d.DeviceID == currentDevice.DeviceID)));
            });
        }

        private async void GetLatenciesAsync()
        {
            var localLatencies = new ObservableCollection<NameValue>();

            await Task.Run(() =>
            {
                // Increment by 50
                for (int index = 50; index <= 500; index += 50)
                {
                    if (index == 200)
                    {
                        localLatencies.Add(new NameValue
                        {
                            Name = index + " ms (" + Application.Current.FindResource("Language_Default").ToString().ToLower() + ")",
                            Value = index
                        });
                    }
                    else
                    {
                        localLatencies.Add(new NameValue
                        {
                            Name = index + " ms",
                            Value = index
                        });
                    }
                }

            });

            this.Latencies = localLatencies;

            NameValue localSelectedLatency = null;

            await Task.Run(() => localSelectedLatency = this.Latencies.Where((pa) => pa.Value == SettingsClient.Get<int>("Playback", "AudioLatency")).Select((pa) => pa).First());

            this.SelectedLatency = localSelectedLatency;
        }

        private async void GetNotificationPositionsAsync()
        {
            var localNotificationPositions = new ObservableCollection<NameValue>();

            await Task.Run(() =>
            {
                localNotificationPositions.Add(new NameValue { Name = ResourceUtils.GetStringResource("Language_Bottom_Left"), Value = (int)NotificationPosition.BottomLeft });
                localNotificationPositions.Add(new NameValue { Name = ResourceUtils.GetStringResource("Language_Top_Left"), Value = (int)NotificationPosition.TopLeft });
                localNotificationPositions.Add(new NameValue { Name = ResourceUtils.GetStringResource("Language_Top_Right"), Value = (int)NotificationPosition.TopRight });
                localNotificationPositions.Add(new NameValue { Name = ResourceUtils.GetStringResource("Language_Bottom_Right"), Value = (int)NotificationPosition.BottomRight });
            });

            this.NotificationPositions = localNotificationPositions;

            NameValue localSelectedNotificationPosition = null;

            await Task.Run(() => localSelectedNotificationPosition = NotificationPositions.Where((np) => np.Value == SettingsClient.Get<int>("Behaviour", "NotificationPosition")).Select((np) => np).First());

            this.SelectedNotificationPosition = localSelectedNotificationPosition;
        }

        private async void GetNotificationSecondsAsync()
        {
            var localNotificationSeconds = new ObservableCollection<int>();

            await Task.Run(() =>
            {
                for (int index = 1; index <= 5; index++)
                {
                    localNotificationSeconds.Add(index);
                }

            });

            this.NotificationSeconds = localNotificationSeconds;

            int localSelectedNotificationSecond = 0;

            await Task.Run(() => localSelectedNotificationSecond = NotificationSeconds.Where((ns) => ns == SettingsClient.Get<int>("Behaviour", "NotificationAutoCloseSeconds")).Select((ns) => ns).First());

            this.SelectedNotificationSecond = localSelectedNotificationSecond;
        }

        private async void GetCheckBoxesAsync()
        {
            await Task.Run(() =>
            {
                // Change the backing field, not the property. Otherwise the confirmation popup is shown when the screen is constructed.
                this.checkBoxWasapiExclusiveModeChecked = SettingsClient.Get<bool>("Playback", "WasapiExclusiveMode");
                OnPropertyChanged(() => this.CheckBoxWasapiExclusiveModeChecked);

                this.CheckBoxShowNotificationWhenPlayingChecked = SettingsClient.Get<bool>("Behaviour", "ShowNotificationWhenPlaying");
                this.CheckBoxShowNotificationWhenPausingChecked = SettingsClient.Get<bool>("Behaviour", "ShowNotificationWhenPausing");
                this.CheckBoxShowNotificationWhenResumingChecked = SettingsClient.Get<bool>("Behaviour", "ShowNotificationWhenResuming");
                this.CheckBoxShowNotificationControlsChecked = SettingsClient.Get<bool>("Behaviour", "ShowNotificationControls");
                this.CheckBoxShowProgressInTaskbarChecked = SettingsClient.Get<bool>("Playback", "ShowProgressInTaskbar");
                this.CheckBoxShowNotificationOnlyWhenPlayerNotVisibleChecked = SettingsClient.Get<bool>("Behaviour", "ShowNotificationOnlyWhenPlayerNotVisible");

            });
        }

        private void ConfirmEnableExclusiveMode()
        {
            if (this.dialogService.ShowConfirmation(
                0xe11b,
                16,
                ResourceUtils.GetStringResource("Language_Exclusive_Mode"),
                ResourceUtils.GetStringResource("Language_Exclusive_Mode_Confirmation"),
                ResourceUtils.GetStringResource("Language_Yes"),
                ResourceUtils.GetStringResource("Language_No")))
            {
                ApplyExclusiveMode(true);
            }
        }

        private void ApplyExclusiveMode(bool isEnabled)
        {
            SettingsClient.Set<bool>("Playback", "WasapiExclusiveMode", isEnabled);
            SetProperty<bool>(ref this.checkBoxWasapiExclusiveModeChecked, isEnabled);
            if (this.playbackService != null) this.playbackService.ExclusiveMode = isEnabled;
        }
        #endregion
    }
}
