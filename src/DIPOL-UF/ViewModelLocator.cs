using System;
using DIPOL_UF.ViewModels;
using DIPOL_UF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DIPOL_UF
{
    internal class ViewModelLocator
    {
        public object this[string viewModelName] => viewModelName switch
        {
            nameof(DipolMainWindow) => Injector.ServiceProvider.GetRequiredService<DipolMainWindowViewModel>(),
            _ => throw new ArgumentException($@"View model {viewModelName} was not found.", nameof(viewModelName))
        };
    }
}
