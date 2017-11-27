﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using DIPOL_UF.Commands;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace DIPOL_UF.Models
{
    class ConnectedCamera : ObservableObject
    {
        private CameraBase camera = null;
        private float targetTemperature = 0.0f;

        private DelegateCommand controlCoolerCommand = null;
        private DelegateCommand verifyTextInputCommand = null;
        
        public float TargetTemperature
        {
            get => targetTemperature;
            set
            {
                if (value != targetTemperature)
                {
                    targetTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }
        public CameraBase Camera
        {
            get => camera;
            private set
            {
                if (value != camera)
                {
                    camera = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand VerifyTextInputCommand
        {
            get => verifyTextInputCommand;
            set
            {
                if (value != verifyTextInputCommand)
                {
                    verifyTextInputCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ControlCoolerCommand
        {
            get => controlCoolerCommand;
            set
            {
                if (value != controlCoolerCommand)
                {
                    controlCoolerCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ConnectedCamera(CameraBase camera)
        {
            Camera = camera;
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            VerifyTextInputCommand = new DelegateCommand(
                VerifyTextInputCommandExecute,
                DelegateCommand.CanExecuteAlways);

            ControlCoolerCommand = new DelegateCommand(
                ControlCoolerCommandExecute,
                (par) => camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature)
                );
        }

        private void VerifyTextInputCommandExecute(object parameter)
        {
            if (parameter is CommandEventArgs<TextCompositionEventArgs> txtCompEA)
            {

                txtCompEA.EventArgs.Handled = !Regex.IsMatch(txtCompEA.EventArgs.Text, @"[-.\d]");
            }
            else if (parameter is CommandEventArgs<TextChangedEventArgs> txtChEA)
            {
                if (txtChEA.Sender is TextBox box && !Regex.IsMatch(box.Text, @"-?\d+\.?\d*"))
                {
                    box.Text = "0.0";
                }
            }
        }
        private void ControlCoolerCommandExecute(object parameter)
        {
            if (camera.CoolerMode == Switch.Disabled)
            {
                camera.SetTemperature((int)targetTemperature);
                camera.CoolerControl(Switch.Enabled);
            }
            else
                camera.CoolerControl(Switch.Disabled);
        }
    }
}
