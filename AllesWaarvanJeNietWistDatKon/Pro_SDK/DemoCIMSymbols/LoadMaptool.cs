using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DemoCIMSymbols
{
    internal class LoadMaptool : MapTool
    {
        #region members
        private GraphicsLayer DemoGraphicsLayer { get; set; } = null;
        #endregion

        #region constructor
        public LoadMaptool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Map;
        }
        #endregion

        #region overrides
        protected override Task OnToolActivateAsync(bool active)
        {
            // Check if the current map is available and a 2D map.
            Map map = MapView.Active.Map;
            if (map.MapType != MapType.Map)
            {
                // Map isn't a 2d map.
                MessageBox.Show("Geen 2D kaart");
                return null;
            }

            QueuedTask.Run(() =>
            {
                DemoGraphicsLayer = map.Layers.FirstOrDefault(item => item.GetType() == typeof(GraphicsLayer)) as GraphicsLayer;
                if (DemoGraphicsLayer == null)
                {
                    // Create a grapics layer
                    GraphicsLayerCreationParams gl_param = new() { Name = "Symbol demo" };
                    // By default will be added to the top of the TOC
                    DemoGraphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(gl_param, map);
                }
            });


            return base.OnToolActivateAsync(active);
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return base.OnSketchCompleteAsync(geometry);
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            return QueuedTask.Run(() =>
            {
                // Get the mouse click point
                MapPoint location = MapView.Active.ClientToMap(e.ClientPoint);

                var json = File.ReadAllText(@"Symbols2.json");

                CIMPointSymbol pointGraphic = CIMPointSymbol.FromJson(json);
                CIMPointGraphic graphic = new()
                {
                    Symbol = pointGraphic?.MakeSymbolReference(),
                    Location = location
                };

                // Add the graphic to the grapicslayer. 
                DemoGraphicsLayer.AddElement(graphic);

                // By default all items are selected, deselect all items
                DemoGraphicsLayer.UnSelectElements();
            });
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
        #endregion
    }
}
