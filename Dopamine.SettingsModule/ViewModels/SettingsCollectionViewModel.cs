﻿using Digimezzo.Utilities.Settings;
using Dopamine.Common.Database.Repositories.Interfaces;
using Dopamine.Common.Services.Collection;
using Dopamine.Common.Services.Indexing;
using Prism;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Threading.Tasks;

namespace Dopamine.SettingsModule.ViewModels
{
    public class SettingsCollectionViewModel : BindableBase, IActiveAware, INavigationAware
    {
        #region Variables
        private bool isActive;
        private bool checkBoxIgnoreRemovedFilesChecked;
        private bool checkBoxRefreshCollectionOnStartupChecked;
        private IIndexingService indexingService;
        private ICollectionService collectionService;
        private ITrackRepository trackRepository;
        #endregion

        #region Commands
        public DelegateCommand RefreshNowCommand { get; set; }
        #endregion

        #region Properties
        public bool IsActive
        {
            get { return this.isActive; }
            set { SetProperty<bool>(ref this.isActive, value); }
        }

        public bool CheckBoxIgnoreRemovedFilesChecked
        {
            get { return this.checkBoxIgnoreRemovedFilesChecked; }
            set
            {
                SettingsClient.Set<bool>("Indexing", "IgnoreRemovedFiles", value);
                SetProperty<bool>(ref this.checkBoxIgnoreRemovedFilesChecked, value);
                if (!value) this.trackRepository.ClearRemovedTrackAsync(); // Fire and forget.
            }
        }

        public bool CheckBoxRefreshCollectionOnStartupChecked
        {
            get { return this.checkBoxRefreshCollectionOnStartupChecked; }
            set
            {
                SettingsClient.Set<bool>("Indexing", "RefreshCollectionOnStartup", value);
                SetProperty<bool>(ref this.checkBoxRefreshCollectionOnStartupChecked, value);
            }
        }
        #endregion

        #region Construction
        public SettingsCollectionViewModel(IIndexingService indexingService, ICollectionService collectionService, ITrackRepository trackRepository)
        {
            this.indexingService = indexingService;
            this.collectionService = collectionService;
            this.trackRepository = trackRepository;

            this.RefreshNowCommand = new DelegateCommand(this.RefreshNow);

            this.GetCheckBoxesAsync();
        }
        #endregion

        #region IActiveAware
        public event EventHandler IsActiveChanged;
        #endregion

        #region Private
        private async void GetCheckBoxesAsync()
        {
            await Task.Run(() =>
            {
                this.CheckBoxRefreshCollectionOnStartupChecked = SettingsClient.Get<bool>("Indexing", "RefreshCollectionOnStartup");

                // Set the backing field of the property. This avoids executing a clear
                // of removed tracks when loading the screen when the setting is false.
                this.checkBoxIgnoreRemovedFilesChecked = SettingsClient.Get<bool>("Indexing", "IgnoreRemovedFiles");
                OnPropertyChanged(() => this.CheckBoxIgnoreRemovedFilesChecked);
            });
        }

        private void RefreshNow()
        {
            this.indexingService.RefreshNow();
        }
        #endregion

        #region INavigationAware
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            this.indexingService.DelayedIndexCollectionAsync(1000, SettingsClient.Get<bool>("Indexing", "IgnoreRemovedFiles"), false);
            this.collectionService.SaveMarkedFoldersAsync();
        }


        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }
        #endregion

    }
}
