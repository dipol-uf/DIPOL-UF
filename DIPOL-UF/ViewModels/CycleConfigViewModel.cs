using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal class CycleConfigViewModel : ReactiveViewModel<ReactiveWrapper<int?>>
    {

        public ReactiveCommand<Window, Unit> SubmitCommand { get; }
        public ReactiveCommand<Window, Unit> CancelCommand { get; }

        [Reactive]
        public string RepeatsValue { get; set; }

        public CycleConfigViewModel(ReactiveWrapper<int?> model) : base(model)
        {
            CreateValidator(
                this.WhenPropertyChanged(x => x.RepeatsValue).Select(x => (nameof(Validators.Validate.CanBeParsed),
                    Validators.Validate.CanBeParsed(x.Value, out int _))), nameof(RepeatsValue));

            
            this.WhenPropertyChanged(x => x.RepeatsValue)
                .Subscribe(x =>
                {
                    var v1 = (nameof(RepeatsValue), nameof(Validators.Validate.CanBeParsed),
                        Validators.Validate.CanBeParsed(x.Value, out int result));

                    var v2 = (nameof(RepeatsValue), nameof(Validators.Validate.ShouldFallWithinRange),
                        v1.Item3 is { }
                            ? Validators.Validate.ShouldFallWithinRange(result, 1, 128)
                            : null);

                    BatchUpdateErrors(v1, v2);
                }).DisposeWith(Subscriptions);

            Model.Object = null;

            CancelCommand = ReactiveCommand.Create<Window>(w => w?.Close())
                .DisposeWith(Subscriptions);
            SubmitCommand = ReactiveCommand.Create<Window>(w =>
                {
                    if (int.TryParse(RepeatsValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var val))
                    {
                        Model.Object = val;
                        w?.Close();
                    }
                }, ObserveHasErrors.Select(x => !x))
                .DisposeWith(Subscriptions);

        }
    }
}
