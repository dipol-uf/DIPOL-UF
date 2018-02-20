using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace DIPOL_UF
{
    [ContentProperty]
    static class TestText
    {
        public static string Wtf2 => "WTF2";
        public static string Wtf => "WTF12435";

    }

    //[ContentProperty(nameof(Binding))]
    [MarkupExtensionReturnType(typeof(string))]
    public class TextExtension : MarkupExtension
    {
        //[ConstructorArgument("p")]
        public object Binding
        {
            get;
            set;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var ipvt = (IProvideValueTarget) serviceProvider.GetService(typeof(IProvideValueTarget));
            if (ipvt.TargetObject is DependencyObject dep &&
                System.ComponentModel.DesignerProperties.GetIsInDesignMode(dep))
                return "Is in designer";
            return "Not in designer";
        }

        public TextExtension()
        {
        }

        public TextExtension(object p)
        {
            Binding = p;
        }
    }
}
