using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Xaml.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Geotriggers;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Core;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime;

namespace GeoTriggers
{
	/// <summary>
	/// A map page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
        private SimulatedLocationDataSource _simulatedSource;
        private LocationGeotriggerFeed _geotriggerFeed;

        private ServiceFeatureTable _wijken;

        private GeotriggerMonitor _sectionMonitor;

        public MainPage()
		{
            InitializeComponent();
            _ = Initialize();
        }

        private async Task Initialize()
        {
            try
            {
                // This sample uses a web map with a predefined tile basemap, feature styles, and labels.
                MyMapView.Map = new Map(new Uri("https://esrinederland.maps.arcgis.com/home/item.html?id=607b3f23f3074fdda973d13ea8b76b3f"));
                await MyMapView.Map.LoadAsync();

                // Get service feature tables from the map to create GeotriggerMonitors for.
                _wijken = ((FeatureLayer)MyMapView.Map.OperationalLayers[0]).FeatureTable as ServiceFeatureTable;
                await _wijken.LoadAsync();

                // Create a simulated location data source for simulating a path through the data.
                _simulatedSource = new SimulatedLocationDataSource();

                // Create SimulationParameters starting at the current time, velocity, and horizontal and vertical accuracy.
                SimulationParameters simulationParameters = new SimulationParameters(DateTime.Now, 150.0, 0.0, 0.0);

                // Get the simulated route json.
                string jsonPolyLine = File.ReadAllText("Route.json");

                // Use the polyline as defined above.
                _simulatedSource.SetLocationsWithPolyline(Geometry.FromJson(jsonPolyLine) as Polyline, simulationParameters);

                // Set up the location display.
                MyMapView.LocationDisplay.DataSource = _simulatedSource;
                MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                MyMapView.LocationDisplay.InitialZoomScale = 15000;
                await _simulatedSource?.StartAsync();

                // LocationGeotriggerFeed will be used in instantiating a FenceGeotrigger.
                _geotriggerFeed = new LocationGeotriggerFeed(_simulatedSource);

                // Create monitors for sections and points of interest.
                _sectionMonitor = CreateGeotriggerMonitor(_wijken, 0.0, "Wijk trigger");

                // Start both Geotrigger monitors.
                await _sectionMonitor?.StartAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, ex.Message.GetType().Name).ShowAsync();
            }
        }

        private GeotriggerMonitor CreateGeotriggerMonitor(ServiceFeatureTable table, double bufferSize, string triggerName)
        {
            // Create parameters for the fence.
            FeatureFenceParameters fenceParameters = new FeatureFenceParameters(table);

            // The ArcadeExpression defined in the following FenceGeotrigger returns the value for the "name" field of the feature that triggered the monitor.
            FenceGeotrigger fenceTrigger = new FenceGeotrigger(_geotriggerFeed, FenceRuleType.EnterOrExit, fenceParameters, new ArcadeExpression("$fenceFeature.bu_naam"), triggerName);

            // Create the monitor and set its event handler for notifications.
            GeotriggerMonitor geotriggerMonitor = new GeotriggerMonitor(fenceTrigger);
            geotriggerMonitor.Notification += HandleGeotriggerNotification;

            return geotriggerMonitor;
        }

        private async void HandleGeotriggerNotification(object sender, GeotriggerNotificationInfo info)
        {
            // The collection used for the list view is changed, and must be modified on a UI thread.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (info is FenceGeotriggerNotificationInfo fenceInfo)
                {
                    if (fenceInfo.FenceNotificationType == FenceNotificationType.Entered)
                    {
                        try
                        {
                            // Get the feature that's fence has been entered.
                            ArcGISFeature feature = fenceInfo.FenceGeoElement as ArcGISFeature;

                            // Get the description for the feature.
                            string description = feature.Attributes["bu_naam"].ToString();

                            // Get the attachments for the feature.
                            IReadOnlyList<Attachment> attach = await feature.GetAttachmentsAsync();

                            // Show the trigger message, in this case the name 
                            NameLabel.Text = fenceInfo.Message;
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message, ex.Message.GetType().Name).ShowAsync();
                        }
                    }
                }
            });
        }

        private void PlayPauseButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Start and stop the simulated location on a button press.
            if (_simulatedSource.Status == LocationDataSourceStatus.Started)
            {
                _simulatedSource.StopAsync();
                PlayPauseButton.Content = "Play";
            }
            else
            {
                _simulatedSource.StartAsync();
                PlayPauseButton.Content = "Pause";
            }
        }
    }
}
