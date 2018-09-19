using System;
using System.Collections.Generic;
using System.Linq;
using Naxam.Controls.Mapbox.Forms;

namespace MapBoxQs.MapComponent
{
    public static class MapHelperExtensions
    {
        // based on: https://wiki.openstreetmap.org/wiki/Zoom_levels 
        private static readonly (int zoomLevel, double longitudeWidth)[] ZoomLevelLongitudeWidths = new (int zoomLevel, double longitudeWidth)[]
        {
            (19, 0.0005), (18, 0.001), (17, 0.003), (16, 0.005), (15, 0.011),
            (14, 0.022), (13, 0.044), (12, 0.088), (11, 0.176), (10, 0.352),
            (9, 0.703), (8, 1.406), (7, 2.813), (6, 5.625), (5, 11.25),
            (4, 22.5), (3, 45), (2, 90), (1, 180), (0, 360)
        };

        public static Position CalculateCenter(this CoordinateBounds bounds)
        {
            return new Position
            {
                Lat = bounds.SouthWest.Lat / 2 + bounds.NorthEast.Lat / 2,
                Long = bounds.SouthWest.Long / 2 + bounds.NorthEast.Long / 2
            };
        }

        /// <summary>
        /// Calculate the best Mapbox zoom level for the given bounds.
        /// </summary>
        public static int CalculateZoomlevel(this CoordinateBounds bounds)
        {
            var longWidth = Math.Abs(bounds.NorthEast.Long - bounds.SouthWest.Long);

            // compare the longitude width with neighboring zoomLevel widths. 
            var level = ZoomLevelLongitudeWidths.Zip(ZoomLevelLongitudeWidths.Skip(1),
                (a, b) =>
                {
                    if (IsBetweenZoomLevels(a, b)) return a;
                    return (0,-1); // not a match: return negative result
                }).First(r => r.longitudeWidth > 0);

            return level.zoomLevel;

            bool IsBetweenZoomLevels((int zoomLevel, double longitudeWidth) left, (int zoomLevel, double longitudeWidth) right)
            {
                return longWidth <= left.longitudeWidth && longWidth < right.longitudeWidth;
            }
        }

        public static CoordinateBounds CalculateBounds(this IEnumerable<Position> positions)
        {
            // https://stackoverflow.com/a/9070812/23820
            var input = positions.ToList();
            var minX = input.Min(p => p.Long);
            var maxX = input.Max(p => p.Long);
            var minY = input.Min(p => p.Lat);
            var maxY = input.Max(p => p.Lat);

            var bounds = new CoordinateBounds {NorthEast = new Position(maxY, minX), SouthWest = new Position(minY, maxX)};
            return bounds;
        }
    }
}