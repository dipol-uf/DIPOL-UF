//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

using DIPOL_UF.Commands;
using DIPOL_UF.Enums;

using static DIPOL_UF.DIPOL_UF_App;

namespace DIPOL_UF.Models
{
    internal class ConnectedCamera : ObservableObject, IDisposable
    {

        private static readonly ConcurrentDictionary<string, ConnectedCamera> connectedCameras 
            = new ConcurrentDictionary<string, ConnectedCamera>();

        // Used by TreeItemViewModel
        private ObservableCollection<ViewModels.MenuItemViewModel> _contextMenu
            = new ObservableCollection<ViewModels.MenuItemViewModel>();

        private bool _autosave;
        private CameraBase _camera;
        private Task _acqTask;
        private CancellationTokenSource _acqTaskCancel;
        private float _targetTemperature;
        private string _targetTemperatureText = "0";
        private bool _canControlTemperature;
        private Tuple<float, float, float, int> _timing;
        private DelegateCommand _controlCoolerCommand;
        //private DelegateCommand verifyTextInputCommand = null;
        private DelegateCommand _setUpAcquisitionCommand;
        private DelegateCommand _controlAcquisitionCommand;
        private int _currentImageIndex;
        private ControlState _state = ControlState.Individual;

        // Used by TreeItemViewModel
        public ObservableCollection<ViewModels.MenuItemViewModel> ContextMenu
        {
            get => _contextMenu;
            set
            {
                if (value != _contextMenu)
                {
                    _contextMenu = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IReadOnlyDictionary<string, ConnectedCamera> ConnectedCameras
            => connectedCameras;

        public string Key
        {
            get;
        }
        public DispatcherTimer ProgBarTimer
        {
            get;
        } = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(100),
            IsEnabled = false
        };
        public int CurrentImageIndex
        {
            get => _currentImageIndex;
            private set
            {
                if (value != _currentImageIndex)
                {
                    _currentImageIndex = value;
                    RaisePropertyChanged();
                }
            }
        }
        public float TargetTemperature
        {
            get => _targetTemperature;
            set
            {
                if (Math.Abs(value - _targetTemperature) > float.Epsilon)
                {
                    _targetTemperature = value;
                    _targetTemperatureText = value.ToString("F1");
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperatureText));
                }
            }
        }
        public string TargetTemperatureText
        {
            get => _targetTemperatureText;
            set
            {
                if (value != _targetTemperatureText)
                {
                    _targetTemperatureText = value;
                    _targetTemperature = float.Parse(value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperature));

                }
            }
        }
        public bool CanControlTemperature
        {
            get => _canControlTemperature;
            set
            {
                if (_camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature) &&
                    value != _canControlTemperature)
                {
                    _canControlTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }
        public FanMode FanMode
        {
            get => _camera.FanMode;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.FanControl) &&
                    value != FanMode)
                {
                    if (value == FanMode.LowSpeed &&
                        !_camera.Capabilities.Features.HasFlag(SdkFeatures.LowFanMode))
                        _camera.FanControl(FanMode.LowSpeed);
                    else
                        _camera.FanControl(value);

                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode InternalShutterState
        {
            get => _camera.Shutter.Internal;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.Shutter) &&
                    value != _camera.Shutter.Internal)
                {
                    _camera.ShutterControl(
                       Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                       Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27),
                       value,
                       _camera.Shutter.External ?? ShutterMode.PermanentlyOpen,
                       (TtlShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode? ExternalShutterState
        {
            get => _camera.Shutter.External;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.ShutterEx) &&
                    value != _camera.Shutter.Internal)
                {
                    _camera.ShutterControl(
                        Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                        Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27), 
                        _camera.Shutter.Internal, 
                        value ?? ShutterMode.PermanentlyOpen, 
                        (TtlShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public CameraBase Camera
        {
            get => _camera;
            private set
            {
                if (!Equals(value, _camera))
                {
                    _camera = value;
                    RaisePropertyChanged();
                }
            }
        }
        public Tuple<float, float, float, int> Timing
        {
            get => _timing;
            set
            {
                if (!value.Equals(_timing))
                {
                    _timing = value;
                    RaisePropertyChanged();
                }

            }
        }
        public bool AreSettingsApplied => Camera.CurrentSettings != null;
        public ControlState State
        {
            get => _state;
            set
            {
                if (value != _state)
                {
                    ChangeState(value);
                    RaisePropertyChanged();
                }
            }
        }
        public bool Autosave
        {
            get => _autosave;
            set
            {
                if(value != _autosave)
                {
                    _autosave = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ControlCoolerCommand
        {
            get => _controlCoolerCommand;
            set
            {
                if (value != _controlCoolerCommand)
                {
                    _controlCoolerCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand SetUpAcquisitionCommand
        {
            get => _setUpAcquisitionCommand;
            set
            {
                if (value != _setUpAcquisitionCommand)
                {
                    _setUpAcquisitionCommand = value;
                    RaisePropertyChanged();                    
                }
            }

        }
        public DelegateCommand ControlAcquisitionCommand
        {
            get => _controlAcquisitionCommand;
            set
            {
                if (value != _controlAcquisitionCommand)
                {
                    _controlAcquisitionCommand = value;
                    RaisePropertyChanged();
                }
            }

        }

        public DipolImagePresenter ImagePresenterModel { get; } = new DipolImagePresenter();
        
        public ConnectedCamera(CameraBase camera, string key)
        {
            Camera = camera;
            Key = key;
            CanControlTemperature = camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
            InitializeCommands();
            Camera.PropertyChanged += Camera_PropertyCahnged;
            Camera.NewImageReceived += Camera_NewImageReceived;
            connectedCameras.TryAdd(Camera.ToString(), this);
            NotifyCollectionChanged();
        }
        public void ContextMenuCommandExecute(object parameter)
        {
            if (parameter is string menu)
            {
                switch (menu)
                {
                    case "Properties":
                        var propVM = new ViewModels.CameraPropertiesViewModel(Camera);
                        var window = new Views.CameraPropertiesView(propVM)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Topmost = true
                        };
                        window.Show();
                        return;
                }

            }
        }

        public void Dispose()
        {
            if (Camera != null)
            {
                connectedCameras.TryRemove(Camera.ToString(), out _);
                NotifyCollectionChanged();
            }
        }

        private static void NotifyCollectionChanged()
        {
            foreach(var item in connectedCameras.Values)
                item.RaisePropertyChanged(nameof(ConnectedCameras));
        }

        private void InitializeCommands()
        {
            ControlCoolerCommand = new DelegateCommand(
                ControlCoolerCommandExecute,
                (par) => Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature) &&
                    !Camera.IsAcquiring
                );

            SetUpAcquisitionCommand = new DelegateCommand(
                SetUpAcquisitionCommandExecute,
                (par) => !Camera.IsAcquiring
                );

            ControlAcquisitionCommand = new DelegateCommand(
                ControlAcquisitionCommandExecute,
                (par) => Camera.CurrentSettings != null);
        }
        private void Camera_PropertyCahnged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Camera.IsAcquiring))
            {
                Helper.ExecuteOnUI(SetUpAcquisitionCommand.OnCanExecuteChanged);
                Helper.ExecuteOnUI(ControlCoolerCommand.OnCanExecuteChanged);
                if (Camera.IsAcquiring)
                {
                    ProgBarTimer.Tag = DateTime.Now;
                    ProgBarTimer.Start();
                }
                else
                    ProgBarTimer.Stop();
            }
        }
        private void ControlCoolerCommandExecute(object parameter)
        {
            if (_camera.CoolerMode == Switch.Disabled)
            {
                CanControlTemperature = false;
                _camera.SetTemperature((int)_targetTemperature);
                _camera.CoolerControl(Switch.Enabled);
            }
            else
            {
                _camera.CoolerControl(Switch.Disabled);
                CanControlTemperature = true;
            }
        }
        private void SetUpAcquisitionCommandExecute(object parameter)
        {
            var hadSettings = Camera.CurrentSettings != null;
            
            var settings = hadSettings 
                ? Camera.CurrentSettings 
                : Camera.GetAcquisitionSettingsTemplate();
            
            var viewModel = new ViewModels.AcquisitionSettingsViewModel(settings, Camera);

            if (new Views.AcquisitionSettingsView(viewModel).ShowDialog() == true)
            {
                if (!viewModel.EstimatedTiming.Equals(default))
                {
                    Timing = viewModel.EstimatedTiming.ToTuple();
                    RaisePropertyChanged(nameof(AreSettingsApplied));
                    ControlAcquisitionCommand.OnCanExecuteChanged();
                }
            }
            else if(!hadSettings)
                settings.Dispose();
        }
        private void ControlAcquisitionCommandExecute(object parameter)
        {
            if (!Camera.IsAcquiring)
            {
                if (Camera.CurrentSettings == null)
                {
                    MessageBox.Show("Please apply acquisition settings first.", "No settings applied",
                          MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    return;
                }

                CurrentImageIndex = 0;
                _acqTaskCancel = new CancellationTokenSource();
                _acqTask = Camera.StartAcquisitionAsync(_acqTaskCancel,
                    Math.Max(Convert.ToInt32(Camera.CurrentSettings.ExposureTime / 50), 50));
               
            }
            else
            {
                if (!_acqTask.IsCompleted)
                    _acqTaskCancel.Cancel();
            }
        }
        private void Camera_NewImageReceived(object sender, ANDOR_CS.Events.NewImageReceivedEventArgs e)
        {
            CurrentImageIndex = e.First;
            if(Camera.AcquiredImages.TryDequeue(out var im))
                ImagePresenterModel.LoadImage(im);
        }

        private void ChangeState(ControlState newState)
        {
            if (newState == ControlState.Master)
            {
                foreach(var item in ConnectedCameras.Values)
                    item.State = ControlState.Slave;
                NotifyCollectionChanged();
            }
            
            _state = newState;
        }
    }
}
