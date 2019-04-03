using DIPOL_UF.Jobs;

namespace DIPOL_UF.ViewModels
{
    class JobSettingsViewModel : ReactiveViewModel<ReactiveWrapper<Target>>
    {
        public JobSettingsViewModel(ReactiveWrapper<Target> model) : base(model)
        {
        }
    }
}
