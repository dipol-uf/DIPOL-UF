namespace DIPOL_UF.ViewModels
{
    class ConnectedCameraTreeItemViewModel : ViewModel<Models.ConnectedCameraTreeItemModel>
    {
        public ANDOR_CS.Classes.CameraBase Camera => model.Camera;
        public System.Collections.ObjectModel.ObservableCollection<MenuItemViewModel> ContextMenu => model.ContextMenu;

        public ConnectedCameraTreeItemViewModel(Models.ConnectedCameraTreeItemModel model)
            : base(model)
        { }
    }
}
