using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pro3
{
    internal class MapTool1 : MapTool
    {
        public MapTool1()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return base.OnSketchCompleteAsync(geometry);
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                case MouseButton.Left:
                    e.Handled = true;
                    break;
            }
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            return QueuedTask.Run(() =>
            {
                MapPoint location = MapView.Active.ClientToMap(e.ClientPoint);
                if (location == null)
                {
                    return;
                }
                var selectedPoint = GetSelectedItems(e);

                try
                {
                    if (selectedPoint.Values.Count > 0)
                    {
                        long oid = selectedPoint.Values.First()[0];
                        var layer = selectedPoint.Keys.First() as FeatureLayer;
                        if (layer.ShapeType == esriGeometryType.esriGeometryPoint)
                        {
                            MessageBox.Show($"Het geselecteerde item met ObjedtId {oid} is van het type point.");

                            Geometry geom = GeometryEngine.Instance.Intersection(location, null, GeometryDimension.esriGeometry0Dimension);
                        }
                    }
                }
                catch(GeodatabaseException gbex)
                {
                    MessageBox.Show($"Geodatabase exception: {gbex.Message}");
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Exception: {ex.Message}");
                }
            });
        }

        private static Dictionary<BasicFeatureLayer, List<long>> GetSelectedItems(MapViewMouseButtonEventArgs e)
        {
            var point = MapPointBuilder.CreateMapPoint(e.ClientPoint.X, e.ClientPoint.Y);
            return MapView.Active.GetFeatures(point, true);
        }
    }
}
