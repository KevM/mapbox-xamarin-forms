using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MapBoxQs.MapComponent
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MapHostPage : ContentPage
	{
		public MapHostPage ()
		{
		    BindingContext= new MapHostViewModel();
		    InitializeComponent ();
		}
	}
}