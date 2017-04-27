﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Android.Graphics;
using Android.Support.V7.App;
using Android.Views;
using Java.Util;

using Mapbox.MapboxSdk.Annotations;
using Mapbox.MapboxSdk.Camera;
using Mapbox.MapboxSdk.Geometry;
using Mapbox.MapboxSdk.Maps;
using Mapbox.Services.Commons.Geojson;

using Naxam.Mapbox.Forms;

using Newtonsoft.Json;
using Xamarin.Forms;
using Annotation = Naxam.Mapbox.Forms.Annotation;
using Bitmap = Android.Graphics.Bitmap;
using MapView = Naxam.Mapbox.Forms.MapView;
using Point = Xamarin.Forms.Point;
using Sdk = Mapbox.MapboxSdk;
using View = Android.Views.View;

namespace Naxam.Mapbox.Platform.Droid
{
    public class MapViewRenderer
        : Xamarin.Forms.Platform.Android.ViewRenderer<MapView, View>, MapboxMap.ISnapshotReadyCallback
    {
        MapViewFragment fragment;
        private const int SIZE_ZOOM = 13;
        private Position _currentCamera;
        private Marker _markerAddress;

        Dictionary<string, Sdk.Annotations.Annotation> _annotationDictionaries =
            new Dictionary<string, Sdk.Annotations.Annotation> ();

        protected override void OnElementChanged (
            Xamarin.Forms.Platform.Android.ElementChangedEventArgs<MapView> e)
        {
            base.OnElementChanged (e);

            if (e.OldElement != null) {
                fragment.MapReady -= MapReady;
                fragment.MapTouched -= MapTouch;
                e.OldElement.GetFeaturesAroundPoint -= GetFeaturesAroundPoint;
                e.OldElement.TakeSnapshot -= TakeMapSnapshot;
            }

            if (e.NewElement == null)
                return;

            if (Control == null) {
                var view = LayoutInflater.FromContext (Context)
                                         .Inflate (Resource.Layout.map_view_container, ViewGroup, false);

                var activity = (AppCompatActivity)Context;
                fragment = (MapViewFragment)activity.SupportFragmentManager.FindFragmentById (Resource.Id.map);
                fragment.MapReady += MapReady;
                fragment.MapTouched += MapTouch;
                _currentCamera = new Position ();
                SetNativeControl (view);
            }
        }

        MapboxMap map;
        Sdk.Maps.MapView _mapview;
        void MapTouch (object sender, MotionEvent e)
        {
        }
        void MapReady (object sender, MapboxMapReadyEventArgs e)
        {
            map = e.Map;
            _mapview = e.MapView;
            map.MyLocationEnabled = true;
            map.MyLocationChange += delegate (object o, MapboxMap.MyLocationChangeEventArgs args) {
                if (Element.UserLocation == null)
                    Element.UserLocation = new Position ();
                Element.UserLocation.Lat = args.P0.Latitude;
                Element.UserLocation.Long = args.P0.Longitude;
            };

            map.CameraChange += delegate (object o, MapboxMap.CameraChangeEventArgs args) {
                _currentCamera.Lat = args.P0.Target.Latitude;
                _currentCamera.Long = args.P0.Target.Longitude;
                Element.Center = _currentCamera;
            };

            map.MapClick += delegate (object o, MapboxMap.MapClickEventArgs args) {
                Element.IsTouchInMap = false;
                var point = map.Projection.ToScreenLocation (args.P0);
                var xfPoint = new Point (point.X, point.Y);
                Element.DidTapOnMapCommand?.Execute (new Tuple<Position, Point> (new Position (args.P0.Latitude, args.P0.Longitude),
                                                                                 xfPoint));
            };
            map.MarkerClick += delegate (object o, MapboxMap.MarkerClickEventArgs args) {
                Element.Center.Lat = args.P0.Position.Latitude;
                Element.Center.Long = args.P0.Position.Longitude;
                Element.IsMarkerClicked = true;
                if (
                Element.CanShowCalloutChecker.Invoke (
                    _annotationDictionaries.FirstOrDefault (x => x.Value == args.P0 as Sdk.Annotations.Annotation).Key)) {
                    args.P0.ShowInfoWindow (map, _mapview);
                }

            };
            map.UiSettings.RotateGesturesEnabled = Element.RotateEnabled;
            map.UiSettings.TiltGesturesEnabled = Element.PitchEnabled;

            _mapview.MapChanged += (object xsender, Sdk.Maps.MapView.MapChangedEventArgs xe) => {
                switch (xe.P0) {
                case Sdk.Maps.MapView.DidFinishLoadingStyle:
                    var mapStyle = Element.MapStyle;
                    if (mapStyle == null
                        || (!string.IsNullOrEmpty (map.StyleUrl) && mapStyle.UrlString != map.StyleUrl)) {
                        mapStyle = new MapStyle (map.StyleUrl);
                        Element.MapStyle = mapStyle;
                    }
                    Element.DidFinishLoadingStyleCommand?.Execute (mapStyle);
                    break;
                case Sdk.Maps.MapView.DidFinishRenderingMap:
                    Element.DidFinishRenderingCommand?.Execute (false);
                    break;
                case Sdk.Maps.MapView.DidFinishRenderingMapFullyRendered:
                    Element.DidFinishRenderingCommand?.Execute (true);
                    break;
                case Sdk.Maps.MapView.RegionDidChange:
                    Element.RegionDidChangeCommand?.Execute (false);
                    break;
                case Sdk.Maps.MapView.RegionDidChangeAnimated:
                    Element.RegionDidChangeCommand?.Execute (true);
                    break;
                default:
                    break;
                }

            };

            SetupFunctions ();
            UpdateMapStyle ();
        }

        public void SetupFunctions ()
        {
            Element.TakeSnapshot += TakeMapSnapshot;

            Element.GetFeaturesAroundPoint += GetFeaturesAroundPoint;
        }

        byte [] TakeMapSnapshot ()
        {
            map.Snapshot (this);
            return result;
        }

        IFeature [] GetFeaturesAroundPoint (Point point, double radius, string [] layers)
        {
            var output = new List<IFeature> ();
            RectF rect = new RectF ((float)(point.X - radius), (float)(point.Y - radius), (float)(point.X + radius), (float)(point.Y + radius));
            var listFeatures = map.QueryRenderedFeatures (rect, layers);
            if (listFeatures.Count != 0) {
                foreach (Feature feature in listFeatures) {
                    IFeature ifeat = null;
                    System.Diagnostics.Debug.WriteLine (feature.ToJson ());
                    if (feature.Geometry is global::Mapbox.Services.Commons.Geojson.Point) {
                        ifeat = new PointFeature ();
                        var pointFeature = feature.Geometry.Coordinates as global::Mapbox.Services.Commons.Models.Position;
                        if (pointFeature == null) continue;
                        ((PointAnnotation)ifeat).Coordinate = new Position (pointFeature.Latitude, pointFeature.Longitude);
                    } else if (feature.Geometry is LineString) {
                        ifeat = new PolylineFeature ();
                    } else if (feature.Geometry is MultiLineString) {
                        ifeat = new MultiPolylineFeature ();

                    }
                    if (ifeat != null) {
                        string id = feature.Id;
                        if (string.IsNullOrEmpty (id)
                            || output.Any ((arg) => (arg as Annotation).Id == id)) {
                            id = Guid.NewGuid ().ToString ();
                        }
                        (ifeat as Annotation).Id = id;
                        ifeat.Attributes = ConvertToDictionary (feature.ToJson ());
                        output.Add (ifeat);
                    }

                }
            }
            return output.ToArray ();
        }

        private Dictionary<string, object> ConvertToDictionary (string featureProperties)
        {
            Dictionary<string, object> objectFeature = JsonConvert.DeserializeObject<Dictionary<string, object>> (featureProperties);
            return JsonConvert.DeserializeObject<Dictionary<string, object>> (objectFeature ["properties"].ToString ()); ;
        }

        private void FocustoLocation (LatLng latLng)
        {
            CameraPosition position = new CameraPosition.Builder ().Target (latLng).Zoom (SIZE_ZOOM).Build ();
            ICameraUpdate camera = CameraUpdateFactory.NewCameraPosition (position);
            map.AnimateCamera (camera);
        }

        protected override void OnElementPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged (sender, e);
            if (e.PropertyName == MapView.CenterProperty.PropertyName) {
                if (!ReferenceEquals (Element.Center, _currentCamera)) {
                    if (Element.Center == null) return;
                    FocustoLocation (new LatLng (Element.Center.Lat, Element.Center.Long));
                }
            } else if (e.PropertyName == MapView.MapStyleProperty.PropertyName && map != null) {
                UpdateMapStyle ();
            } else if (e.PropertyName == MapView.PitchEnabledProperty.PropertyName) {
                if (map != null) {
                    map.UiSettings.TiltGesturesEnabled = Element.PitchEnabled;
                }
            } else if (e.PropertyName == MapView.RotateEnabledProperty.PropertyName) {
                if (map != null) {
                    map.UiSettings.RotateGesturesEnabled = Element.RotateEnabled;
                }
            } else if (e.PropertyName == MapView.AnnotationsProperty.PropertyName) {
                RemoveAllAnnotations ();
                if (Element.Annotations != null) {
                    AddAnnotations (Element.Annotations.ToArray ());
                    var notifyCollection = Element.Annotations as INotifyCollectionChanged;
                    if (notifyCollection != null) {
                        notifyCollection.CollectionChanged += OnAnnotationsCollectionChanged;
                    }
                }
            }
        }

        void UpdateMapStyle ()
        {
            if (Element.MapStyle != null && !string.IsNullOrEmpty (Element.MapStyle.UrlString)) {
                map.StyleUrl = Element.MapStyle.UrlString;
                Element.MapStyle.PropertyChanging += OnMapStylePropertyChanging;
                Element.MapStyle.PropertyChanged += OnMapStylePropertyChanged;
            }

        }

        void OnMapStylePropertyChanging (object sender, Xamarin.Forms.PropertyChangingEventArgs e)
        {
            if (e.PropertyName == MapStyle.CustomSourcesProperty.PropertyName
                && (sender as MapStyle).CustomSources != null) {
                var notifiyCollection = (sender as MapStyle).CustomSources as INotifyCollectionChanged;
                if (notifiyCollection != null) {
                    notifiyCollection.CollectionChanged -= OnShapeSourcesCollectionChanged;
                }
                RemoveSources (Element.MapStyle.CustomSources.ToList ());
            }
        }

        void OnMapStylePropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MapStyle.CustomSourcesProperty.PropertyName
                && (sender as MapStyle).CustomSources != null) {
                var notifiyCollection = Element.MapStyle.CustomSources as INotifyCollectionChanged;
                if (notifiyCollection != null) {
                    notifiyCollection.CollectionChanged += OnShapeSourcesCollectionChanged;
                }

                AddSources (Element.MapStyle.CustomSources.ToList ());
            } else if (e.PropertyName == MapStyle.CustomLayersProperty.PropertyName
                       && (sender as MapStyle).CustomLayers != null) {
                var notifiyCollection = Element.MapStyle.CustomLayers as INotifyCollectionChanged;
                if (notifiyCollection != null) {
                    notifiyCollection.CollectionChanged += OnLayersCollectionChanged;
                }

                AddLayers (Element.MapStyle.CustomLayers.ToList ());
            }
        }

        void OnShapeSourcesCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO
        }

        void AddSources (List<ShapeSource> list)
        {
            //TODO
        }

        void RemoveSources (List<ShapeSource> list)
        {
            //TODO
        }

        void OnLayersCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO
        }

        void AddLayers (List<Layer> list)
        {
            //TODO
        }

        private void OnAnnotationsCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) {
                var annots = new List<PolylineOptions> ();
                foreach (Annotation annot in e.NewItems) {
                    var shape = AddAnnotation (annot);
                    if (shape != null) {
                        //TODO
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                var items = new List<Annotation> ();
                foreach (Annotation annot in e.OldItems) {
                    items.Add (annot);
                }
                RemoveAnnotations (items.ToArray ());
            } else if (e.Action == NotifyCollectionChangedAction.Reset) {
                //TODO Update pins
            }
        }

        void RemoveAnnotations (Annotation [] annotations)
        {
            var currentAnnotations = map.Annotations;
            if (currentAnnotations == null) {
                return;
            }
            var annots = new List<Sdk.Annotations.Annotation> ();
            foreach (Annotation at in annotations) {
                if (_annotationDictionaries.ContainsKey (at.Id)) {
                    annots.Add (_annotationDictionaries [at.Id]);
                }
            }
            map.RemoveAnnotations (annots.ToArray ());
        }

        void AddAnnotations (Annotation [] annotations)
        {
            foreach (Annotation at in annotations) {
                var shape = AddAnnotation (at);
            }
        }

        private Sdk.Annotations.Annotation AddAnnotation (Annotation at)
        {
            Sdk.Annotations.Annotation options = null;
            if (at is PointAnnotation) {
                var marker = new MarkerOptions ();
                marker.SetTitle (at.Title);
                marker.SetSnippet (at.Title);
                marker.SetPosition (new LatLng (((PointAnnotation)at).Coordinate.Lat,
                    ((PointAnnotation)at).Coordinate.Long));
                options = map.AddMarker (marker);
            } else if (at is PolylineAnnotation) {
                var polyline = at as PolylineAnnotation;
                if (polyline.Coordinates?.Count () == 0) {
                    return null;
                }
                var coords = new ArrayList ();
                for (var i = 0; i < polyline.Coordinates.Count (); i++) {
                    coords.Add (new LatLng (polyline.Coordinates.ElementAt (i).Lat, polyline.Coordinates.ElementAt (i).Long));
                }
                var polylineOpt = new PolylineOptions ();
                polylineOpt.AddAll (coords);
                options = map.AddPolyline (polylineOpt);
            } else if (at is MultiPolylineAnnotation) {
                var polyline = at as MultiPolylineAnnotation;
                if (polyline.Coordinates == null || polyline.Coordinates.Length == 0) {
                    return null;
                }

                var lines = new List<PolylineOptions> ();
                for (var i = 0; i < polyline.Coordinates.Length; i++) {
                    if (polyline.Coordinates [i].Length == 0) {
                        continue;
                    }
                    var coords = new PolylineOptions ();
                    for (var j = 0; j < polyline.Coordinates [i].Length; j++) {
                        coords.Add (new LatLng (polyline.Coordinates [i] [j].Lat, polyline.Coordinates [i] [j].Long));
                    }
                    lines.Add (coords);
                }
                IList<Polyline> listPolylines = map.AddPolylines (lines);
                //TODO  handle add listPolyline . Need to identify to remove after that

            }
            if (options != null) {
                if (at.Id != null) {
                    _annotationDictionaries.Add (at.Id, options);
                }
            }

            return options;
        }

        void RemoveAllAnnotations ()
        {
            if (map.Annotations != null) {
                map.RemoveAnnotations (map.Annotations);
            }
        }

        private byte [] result;
        public void OnSnapshotReady (Bitmap bmp)
        {
            MemoryStream stream = new MemoryStream ();
            bmp.Compress (Bitmap.CompressFormat.Png, 0, stream);
            result = stream.ToArray ();
        }
    }
}
