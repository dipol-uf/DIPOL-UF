//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using static DIPOL_UF.ViewModels.AcquisitionSettingsViewModel;

namespace DIPOL_UF.Extensions
{
    internal class FileDialogAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty SaveCommandProperty = 
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(FileDialogAction));
        public static readonly DependencyProperty LoadCommandProperty =
            DependencyProperty.Register(nameof(LoadCommand), typeof(ICommand), typeof(FileDialogAction));

        public ICommand SaveCommand
        {
            get => GetValue(SaveCommandProperty) as ICommand;
            set => SetValue(SaveCommandProperty, value);
        }
        public ICommand LoadCommand
        {
            get => GetValue(LoadCommandProperty) as ICommand;
            set => SetValue(LoadCommandProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if (!(parameter is DialogRequestedEventArgs args && 
                args.Descriptor is FileDialogDescriptor desc)) return;

            var initialDirectory = desc.InitialDirectory;

            if(string.IsNullOrEmpty(initialDirectory))
                initialDirectory = string.IsNullOrEmpty(Properties.Settings.Default.DefaultFolder) 
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) 
                    : Properties.Settings.Default.DefaultFolder;

            if (desc.Mode == FileDialogDescriptor.DialogMode.Save)
            {
                var dialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = desc.DefaultExtenstion,
                    FileName = desc.FileName,
                    FilterIndex = 0,
                    InitialDirectory = initialDirectory,
                    Title = desc.Title,
                    Filter =
                        $@"Dipol files (*{desc.DefaultExtenstion})|*{desc.DefaultExtenstion}|All files (*.*)|*.*"
                };
                var result = dialog.ShowDialog() == true;

                if (result)
                {
                    Properties.Settings.Default.DefaultFolder = Path.GetDirectoryName(dialog.FileName);
                    Properties.Settings.Default.Save();
                }
                
                SaveCommand?.Execute(result 
                    ? dialog.FileName
                    : null);
            }
            else
            {
                var dialog = new OpenFileDialog()
                {
                    Multiselect = false,
                    AddExtension = true,
                    DefaultExt = desc.DefaultExtenstion,
                    FileName = desc.FileName,
                    FilterIndex = 0,
                    InitialDirectory = initialDirectory,
                    Title = desc.Title,
                    Filter =
                        $@"Dipol files (*{desc.DefaultExtenstion})|*{desc.DefaultExtenstion}|All files (*.*)|*.*"
                };
                var result = dialog.ShowDialog() == true;

                if (result)
                {
                    Properties.Settings.Default.DefaultFolder = Path.GetDirectoryName(dialog.FileName);
                    Properties.Settings.Default.Save();
                }

                // BUG : can be disposed
                LoadCommand?.Execute(result
                    ? dialog.FileName
                    : null);
            }
        }
    }
}
