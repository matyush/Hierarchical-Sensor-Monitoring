﻿using System;
using System.IO;
using System.Threading;
using HSMClient.Common.Logging;
using HSMClient.Connection;
using HSMClientWPFControls.Bases;
using HSMClientWPFControls.Model;
using HSMCommon.Model;

namespace HSMClient
{
    public class UpdateClientModel : ModelBase, IUpdateClientModel
    {
        //private bool _isUpdateAvailable;
        private bool _isUpdateDownloaded = false;
        private bool _isUpdating = false;
        private int _downloadProgress = 0;
        private int _downloadedFilesCount = 0;
        private AdminConnector _admin;
        private ClientUpdateInfoModel _updateInfo;
        private string _updateDirectory;
        private Thread _downloadThread;
        private ClientVersionModel _currentVersion;
        private ClientVersionModel _latestVersion;
        public UpdateClientModel(ClientMonitoringModel model)
        {
            _admin = new AdminConnector(model.ConnectionAddress);
            _updateDirectory = Path.Combine(Path.GetTempPath(), "HSMClientUpdate");
            _currentVersion = model.CurrentVersion;
            Directory.CreateDirectory(_updateDirectory);
            _latestVersion = model.LastAvailableVersion;
        }

        public ClientVersionModel UpdateVersion => _latestVersion;

        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }
        public int DownloadedFilesCount
        {
            get => _downloadedFilesCount;
            set
            {
                _downloadedFilesCount = value;
                OnPropertyChanged(nameof(DownloadedFilesCount));
            }
        }

        public bool IsUpdateDownloaded
        {
            get => _isUpdateDownloaded;
            set
            {
                _isUpdateDownloaded = value;
                OnPropertyChanged(nameof(IsUpdateDownloaded));
            }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }

        public bool IsUpdateAvailable
        {
            get => _latestVersion > _currentVersion;
        }
        public ClientUpdateInfoModel UpdateInfo
        {
            get => _updateInfo;
            set
            {
                _updateInfo = value;
                OnPropertyChanged(nameof(UpdateInfo));
            }
        }

        public void DownloadUpdate()
        {
            IsUpdating = true;
            _admin.Initialize();
            StartDownloadThread();
        }
        private void FillUpdateInfo()
        {
            try
            {
                UpdateInfo = _admin.GetUpdateInfo();
            }
            catch (Exception e)
            {
                Logger.Error($"GetUpdate info error: {e}");
            }
        }
        private void StartDownloadThread()
        {
            _downloadThread = new Thread(DownloadThread);
            _downloadThread.Name = $"Thread_{DateTime.Now.ToLongTimeString()}";
            _downloadThread.Start();
        }

        private void DownloadThread()
        {
            FillUpdateInfo();
            foreach (var file in _updateInfo.Files)
            {
                byte[] fileBytes = _admin.GetFile(file).Result;
                WriteFile(fileBytes, file);
                ++DownloadedFilesCount;
                DownloadProgress = (int)(100 * DownloadedFilesCount / _updateInfo.Files.Count);
            }

            IsUpdateDownloaded = true;
        }
        private void WriteFile(byte[] bytes, string file)
        {
            using (FileStream fs = new FileStream(Path.Combine(_updateDirectory, file), FileMode.Create, FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                fs.Close(); 
            }
        }
    }
}
