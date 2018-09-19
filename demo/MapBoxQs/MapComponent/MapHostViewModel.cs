using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Naxam.Controls.Mapbox.Forms;
using Xamarin.Forms;

namespace MapBoxQs.MapComponent
{
    public static class TestLocations
    {
        public static Position[] Positions => new[]
        {
            new Position(25.67516708, -101.33053589), 
            new Position(25.673231, -101.332268),
            new Position(25.673231, -101.332268)
        };
    }

    public class MapHostViewModel : INotifyPropertyChanged
    {
        private IEnumerable<Position> _positions;

        public MapHostViewModel()
        {
            _positions = TestLocations.Positions;
            Map = new MapViewModel(_positions.CalculateBounds());

            BindMapViewModelCommand = new Command(() =>
            {
                _positions = _positions.Select(p=>new Position(p.Lat+0.1, p.Long));
                var bounds = _positions.CalculateBounds();
                var vm = new MapViewModel(bounds);
                Map = vm;
            });
        }

        private MapViewModel _mapViewModel;

        private ICommand _bindMapViewModelCommand;
        public ICommand BindMapViewModelCommand
        {
            get => _bindMapViewModelCommand;
            set
            {
                _bindMapViewModelCommand = value;
                OnPropertyChanged("BindMapViewModelCommand");
            }
        }

        public MapViewModel Map
        {
            get => _mapViewModel;
            set
            {
                _mapViewModel = value;
                OnPropertyChanged("Map");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
