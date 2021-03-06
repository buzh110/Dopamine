﻿using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Dopamine.Common.Base;
using Dopamine.Common.IO;
using Digimezzo.Utilities.Log;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Dopamine.Common.Services.Cache
{
    public class CacheService : ICacheService
    {
        #region Variables
        private string coverArtCacheFolderPath;
        private string temporaryCacheFolderPath;
        private Timer temporaryCacheCleanupTimer;
        private int temporaryCacheCleanupTimeout = 300000; // 300000 milliseconds = 5 minutes
        #endregion

        #region Properties
        public string CoverArtCacheFolderPath
        {
            get
            {
                return this.coverArtCacheFolderPath;
            }
        }

        public string TemporaryCacheFolderPath
        {
            get
            {
                return this.temporaryCacheFolderPath;
            }
        }
        #endregion

        #region Construction
        public CacheService()
        {
            string cacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder);
            this.coverArtCacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder, ApplicationPaths.CoverArtCacheFolder);
            this.temporaryCacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder, ApplicationPaths.TemporaryCacheFolder);

            // If it doesn't exist, create the cache folder.
            if (!Directory.Exists(cacheFolderPath)) Directory.CreateDirectory(cacheFolderPath);

            // If it doesn't exist, create the coverArt cache folder.
            if (!Directory.Exists(this.coverArtCacheFolderPath)) Directory.CreateDirectory(this.coverArtCacheFolderPath);

            // If it exists, delete the temporary cache folder and create it again (this makes sure it is cleaned from time to time)
            if (Directory.Exists(this.temporaryCacheFolderPath))
            {
                try
                {
                    Directory.Delete(this.temporaryCacheFolderPath, true);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not delete the temporary cache folder. Exception: {0}", ex.Message);
                }
            }

            // If the temporary cache folder doesn't exist, create it.
            if (!Directory.Exists(this.temporaryCacheFolderPath)) Directory.CreateDirectory(this.temporaryCacheFolderPath);

            temporaryCacheCleanupTimer = new Timer();
            temporaryCacheCleanupTimer.Interval = temporaryCacheCleanupTimeout;
            temporaryCacheCleanupTimer.Elapsed += TemporaryCacheCleanupTimer_Elapsed;
            temporaryCacheCleanupTimer.Start();
        }
        #endregion

        #region ICacheService
        public async Task<string> CacheArtworkAsync(byte[] artwork)
        {
            if (artwork == null) return string.Empty;

            string artworkID = "album-" + Guid.NewGuid().ToString();

            try
            {
                await Task.Run(() =>
                {
                    ImageUtils.Byte2Jpg(artwork, Path.Combine(this.coverArtCacheFolderPath, artworkID + ".jpg"), 0, 0, Constants.CoverQualityPercent);
                });
            }
            catch (Exception ex)
            {
                LogClient.Error("Could convert artwork byte[]to JPG. Exception: {0}", ex.Message);
                artworkID = string.Empty;
            }

            return artworkID;
        }

        public string GetCachedArtworkPath(string artworkID)
        {
            if (!string.IsNullOrEmpty(artworkID))
            {
                return System.IO.Path.Combine(coverArtCacheFolderPath, artworkID + ".jpg");
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task<string> DownloadFileToTemporaryCacheAsync(Uri uri)
        {
            try
            {
                string cachedFilePath = System.IO.Path.Combine(this.temporaryCacheFolderPath, Guid.NewGuid().ToString());

                using (var client = new WebClient())
                {
                    await Task.Run(() => client.DownloadFile(uri, cachedFilePath));
                }

                if (!System.IO.File.Exists(cachedFilePath)) cachedFilePath = string.Empty;

                return cachedFilePath;
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not download file to temporary cache. Exception: {0}", ex.Message);
                return string.Empty;
            }
           
        }
        #endregion

        #region Private
        private void TemporaryCacheCleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            temporaryCacheCleanupTimer.Stop();

            try
            {
                DirectoryInfo di = new DirectoryInfo(temporaryCacheFolderPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Could not delete the file '{0}' from temporary cache. Exception: {1}", file.FullName, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Error while cleaning up to temporary cache. Exception: {0}", ex.Message);
            }

            temporaryCacheCleanupTimer.Start();
        }
        #endregion
    }
}
