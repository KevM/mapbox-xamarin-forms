using System.Collections.ObjectModel;
using System.ComponentModel;
using Naxam.Controls.Mapbox.Forms;

namespace MapBoxQs.MapComponent
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly CoordinateBounds _initialBounds;

        public MapViewModel(CoordinateBounds initialBounds)
        {
            _initialBounds = initialBounds;
            _centerLocation = initialBounds.CalculateCenter();
            _zoomLevel = initialBounds.CalculateZoomlevel();
        }

        private Position _centerLocation;

        public Position CenterLocation
        {
            get => _centerLocation;
            set
            {
                _centerLocation = value;
                OnPropertyChanged("CenterLocation");
            }
        }

        private double _zoomLevel = 16;
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = value;
                OnPropertyChanged("ZoomLevel");
            }
        }

        private ObservableCollection<Annotation> _annotations;
        public ObservableCollection<Annotation> Annotations
        {
            get => _annotations;
            set
            {
                _annotations = value;
                OnPropertyChanged("Annotations");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}