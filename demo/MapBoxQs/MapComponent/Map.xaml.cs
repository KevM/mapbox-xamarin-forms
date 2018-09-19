using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MapBoxQs.MapComponent
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Map : ContentView
    {
        public Map()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(object), typeof(Map), null, BindingMode.OneWay, propertyChanged: OnSourcePropertyChanged);

        private static void OnSourcePropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var map = (Map) bindable;

            map.naxamMap.BindingContext = newvalue;
        }
        
        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
    }
}
