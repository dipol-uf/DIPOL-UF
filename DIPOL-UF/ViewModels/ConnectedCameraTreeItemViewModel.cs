namespace DIPOL_UF.ViewModels
{
    class ConnectedCameraTreeItemViewModel : ViewModel<Models.ConnectedCamera>
    {
        public ANDOR_CS.Classes.CameraBase Camera => model.Camera;
        public System.Collections.ObjectModel.ObservableCollection<MenuItemViewModel> ContextMenu => model.ContextMenu;
        

        public ConnectedCameraTreeItemViewModel(Models.ConnectedCamera model)
            : base(model)
        { }
    }
}
