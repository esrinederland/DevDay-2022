using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace DemoFPS
{
    internal class FPS : MapTool
    {
        #region members
        private int _counter;
        private bool _active;

        private readonly Timer _timer;
        #endregion

        #region constructor
        public FPS()
        {
            _counter = 0;
            _timer = new Timer(250);
            _timer.Elapsed += OnTimer_Elapsed;
            _timer.Enabled = false;
            _active = false;
        }
        #endregion

        #region overrides
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return base.OnSketchCompleteAsync(geometry);
        }
        protected override void OnToolMouseMove(MapViewMouseEventArgs e)
        {
            if (_active)
            {
                QueuedTask.Run(() =>
                {
                    _counter++;
                    if (_counter % 10 == 0)
                    {
                        MapView.Active.LookAtAsync(ActiveMapView.ClientToMap(e.ClientPoint), new TimeSpan(0, 0, 0, 0, 15));
                        _counter = 0;
                    }
                });
            }

            e.Handled = true;
        }

        protected override Task HandleKeyDownAsync(MapViewKeyEventArgs k)
        {
            var camera = MapView.Active.Camera;
            switch (k.Key)
            {
                case Key.J:
                    {
                        camera.Pitch += 5;
                        camera.Z += 5;
                        _timer.Enabled = true;
                    }
                    break;
                case Key.R:
                    camera.Roll -= 45;
                    break;
                case Key.L:
                    camera.Roll += 45;
                    break;
                case Key.Q:
                    _active = !_active;
                    break;
                case Key.Left:
                    camera.Heading += 5;
                    break;
                case Key.Right:
                    camera.Heading -= 5;
                    break;
                case Key.Up:
                    camera.Y += 0.02;
                    break;
                case Key.Down:
                    camera.Y -= 0.02;
                    break;
            }

            return MapView.Active.ZoomToAsync(camera, new TimeSpan(0, 0, 0, 0, 250));
        }

        protected override void OnToolKeyDown(MapViewKeyEventArgs k)
        {
            //We will do some basic key handling to allow jumping and rolling
            if ((k.Key == Key.J) ||
                (k.Key == Key.R) ||
                (k.Key == Key.L) ||
                (k.Key == Key.Q) ||
                (k.Key == Key.Left) ||
                (k.Key == Key.Right) ||
                (k.Key == Key.Up) ||
                (k.Key == Key.Down))
                k.Handled = true;
            else
                base.OnToolKeyDown(k);
        }
        #endregion

        #region events
        private void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            var camera = MapView.Active.Camera;
            camera.Pitch -= 5;
            camera.Z -= 5;

            MapView.Active.ZoomToAsync(camera, new TimeSpan(0, 0, 0, 0, 250));
        }
        #endregion
    }
}
