using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DemoCIMSymbols
{
    internal class SymbolByUrl : MapTool
    {
        #region members
        private GraphicsLayer DemoGraphicsLayer { get; set; } = null;
        #endregion

        #region constructor
        public SymbolByUrl()
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
                // Get the mouse click point
                MapPoint location = MapView.Active.ClientToMap(e.ClientPoint);

                CIMPictureMarker marker = new()
                {
                    Size = 30,
                    URL = BuildPictureMarkerURL(new Uri(@"https://www.buienradar.nl/resources/images/icons/weather/30x30/f.png"))
                };

                CIMPointGraphic graphic = new()
                {
                    Symbol = SymbolFactory.Instance.ConstructPointSymbol(marker).MakeSymbolReference(),
                    Location = location
                };

                // Add the graphic to the grapicslayer. 
                DemoGraphicsLayer.AddElement(graphic);

                // By default all items are selected, deselect all items
                DemoGraphicsLayer.UnSelectElements();
            });
        }

        #endregion

        #region private methods
        private static string BuildPictureMarkerURL(Uri uri)
        {
            _ = new PngBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            BitmapDecoder decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            StringBuilder builder = new();
            builder.Append("data:");
            builder.Append(decoder.CodecInfo.MimeTypes);
            builder.Append(";base64,");
            builder.Append(ToBase64(decoder));
            return builder.ToString();
        }

        private static string ToBase64(BitmapDecoder decoder)
        {
            BitmapEncoder encoder = BitmapEncoder.Create(decoder.CodecInfo.ContainerFormat);
            foreach (BitmapFrame frame in decoder.Frames)
            {
                encoder.Frames.Add(frame);
            }
            
            using MemoryStream stream = new();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray(), Base64FormattingOptions.InsertLineBreaks);
        }
        #endregion
    }
}
