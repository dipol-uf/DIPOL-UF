using System.ComponentModel;

namespace DIPOL_UF
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(sender, e);

        protected virtual void RaisePropertyChanged(
            [System.Runtime.CompilerServices.CallerMemberName]  string propertyName = "")
            => OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        protected ObservableObject()
        {
#if DEBUG
            DebugTracer.AddTarget(this, this.ToString());
#endif
        }
    }
}
