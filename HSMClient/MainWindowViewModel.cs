﻿using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Input;
using HSMClient.Common;
using HSMClient.Configuration;
using HSMClient.Dialog;
using HSMClientWPFControls.Bases;
using HSMClientWPFControls.Model;
using HSMClientWPFControls.Objects;
using HSMClientWPFControls.SensorExpandingService;
using HSMClientWPFControls.View.SensorDialog;
using HSMClientWPFControls.ViewModel;
using HSMCommon.Model;

namespace HSMClient
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region IDisposable implementation

        // Disposed flag.
        private bool _disposed;

        // Implement IDisposable.
        //public new void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        protected override void Dispose(bool disposingManagedResources)
        {
            // The idea here is that Dispose(Boolean) knows whether it is 
            // being called to do explicit cleanup (the Boolean is true) 
            // versus being called due to a garbage collection (the Boolean 
            // is false). This distinction is useful because, when being 
            // disposed explicitly, the Dispose(Boolean) method can safely 
            // execute code using reference type fields that refer to other 
            // objects knowing for sure that these other objects have not been 
            // finalized or disposed of yet. When the Boolean is false, 
            // the Dispose(Boolean) method should not execute code that 
            // refer to reference type fields because those objects may 
            // have already been finalized."

            if (!_disposed)
            {
                if (disposingManagedResources)
                {
                    
                    // Dispose managed resources here...
                }

                // Dispose unmanaged resources here...

                // Set large fields to null here...

                // Mark as disposed.
                _monitoringTree?.Dispose();
                _disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~MainWindowViewModel()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        #endregion

        private string _title;
        private MonitoringTreeViewModel _monitoringTree;
        private readonly ClientMonitoringModel _monitoringModel;
        private DownloadUpdateModel _updateModel;
        private ChangeClientCertificateViewModel _changeCertificateModel;
        public MainWindowViewModel()
        {
            //CheckConfiguration();
            _monitoringModel = new ClientMonitoringModel();
            Model = _monitoringModel;
            _monitoringTree = new MonitoringTreeViewModel(_monitoringModel);
            _changeCertificateModel = new ChangeClientCertificateViewModel(_monitoringModel);
            
            IDialogModelFactory factory = new DialogModelFactory(_monitoringModel.SensorHistoryConnector);
            DialogSensorExpandingService expandingService = new DialogSensorExpandingService(factory);
            expandingService.RegisterDialog(SensorTypes.BoolSensor, typeof(DefaultValuesListSensorView),
                typeof(DefaultValuesListSensorView));
            expandingService.RegisterDialog(SensorTypes.IntSensor, typeof(DefaultValuesListSensorView),
                typeof(ClientDefaultValuesListSensorModel));
            expandingService.RegisterDialog(SensorTypes.DoubleSensor, typeof(DefaultValuesListSensorView),
                typeof(ClientDefaultValuesListSensorModel));
            expandingService.RegisterDialog(SensorTypes.StringSensor, typeof(DefaultValuesListSensorView),
                typeof(ClientDefaultValuesListSensorModel));
            expandingService.RegisterDialog(SensorTypes.BarIntSensor, typeof(DefaultValuesListSensorView),
                typeof(ClientDefaultValuesListSensorModel));
            expandingService.RegisterDialog(SensorTypes.BarDoubleSensor, typeof(DefaultValuesListSensorView),
                typeof(ClientDefaultValuesListSensorModel));

            _monitoringTree.SensorExpandingService = expandingService;

            _monitoringModel.ShowProductsEvent += monitoringModel_ShowProductsEvent;
            _monitoringModel.ShowSettingsWindowEvent += monitoringModel_ShowSettingsWindowEvent;
            _monitoringModel.ShowGenerateCertificateWindowEvent += monitoringModel_ShowGenerateCertificateWindowEvent;
            //_monitoringModel.DefaultCertificateReplacedEvent += monitoringModel_DefaultCertificateReplacedEvent;
            DownloadUpdateCommand = new SingleDelegateCommand(DownloadVersion);
        }

        public ICommand DownloadUpdateCommand { get; private set; }
        public ClientVersionModel CurrentVersion => ConfigProvider.Instance.CurrentVersion;
            
        public bool IsClientCertificateDefault => _monitoringModel.IsClientCertificateDefault;
        public ClientVersionModel LastAvailableVersion => _monitoringModel.LastAvailableVersion;

        private void monitoringModel_ShowProductsEvent(object sender, EventArgs e)
        {
            ProductsWindow window = new ProductsWindow(_monitoringModel.ProductsConnector);
            window.Owner = App.Current.MainWindow;
            window.Show();
        }

        private void monitoringModel_ShowSettingsWindowEvent(object sender, EventArgs e)
        {
            SettingsWindow window = new SettingsWindow(_monitoringModel.SettingsConnector);
            window.Owner = App.Current.MainWindow;
            window.Show();
        }

        private void monitoringModel_ShowGenerateCertificateWindowEvent(object sender, EventArgs e)
        {
            GenerateCertificateWindow window = new GenerateCertificateWindow(_monitoringModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }
        private void monitoringModel_DefaultCertificateReplacedEvent(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsClientCertificateDefault));
        }
        public MonitoringTreeViewModel MonitoringTree
        {
            get => _monitoringTree;
            set => _monitoringTree = value;
        }

        public ChangeClientCertificateViewModel ChangeCertificateModel
        {
            get => _changeCertificateModel;
            set => _changeCertificateModel = value;
        }

        public string Title
        {
            get => $"{TextConstants.AppName}. Version: {CurrentVersion}";
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string UpdateButtonText => $"Install version {LastAvailableVersion}";

        public bool IsClientUpdateAvailable => CurrentVersion < LastAvailableVersion;
        public bool IsUpdateDownloading => _updateModel != null;
        public int UpdateDownloadProgress => _updateModel.DownloadProgress;
        private bool DownloadVersion(object o, bool isCheckOnly)
        {
            if (isCheckOnly)
                return true;

            _updateModel = new DownloadUpdateModel(_monitoringModel);
            OnPropertyChanged(nameof(IsUpdateDownloading));
            return true;
        }
    }
}
