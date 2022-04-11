import requests
import datetime
from arcgis import GIS
from arcgis.features import FeatureLayer

# Create a reference to ArcGIS using the Python API. 
gis = GIS('home')
fl_stations = FeatureLayer("https://services.arcgis.com/emS4w7iyWEQiulAb/ArcGIS/rest/services/Meetstations/FeatureServer/6")
fl_meetwaarden = FeatureLayer("https://services.arcgis.com/emS4w7iyWEQiulAb/ArcGIS/rest/services/Meetwaarden/FeatureServer/9")

# Get all the stations from ArcGIS. 
station_ditc = {}
stations_query_result = fl_stations.query(where="42=42", out_field="*", out_sr="4326", return_geometry=True, order_by_fields="Name")
for feature in stations_query_result.features:
    station_ditc[feature.attributes["ID"]] = [feature.geometry, feature.attributes["Name"]]

# Create a list for the new measurement features.
new_measurements = []

# Set the from and to time. 
now = datetime.datetime.now()
last_hour = now + datetime.timedelta(hours=-4)

# Format the time. 
now_str = now.strftime("%Y-%m-%dT%H:00:00")
last_hour_str = last_hour.strftime("%Y-%m-%dT%H:59:00")
start = 1
end = 1
# Start getting the data, with paging.
while start <= end:
    measurements_reponse = requests.get(f"https://api.luchtmeetnet.nl/open_api/measurements?page=1&order_by=timestamp_measured&order_direction=desc&end={now_str}&start={last_hour_str}")
    if measurements_reponse.ok:
        start = start + 1
        measurements_json = measurements_reponse.json()
        end = measurements_json["pagination"]["last_page"]
        for measurement in measurements_json["data"]:
            if measurement["formula"] == "NO2":
                # Get the date and convert the data. 
                location = station_ditc[measurement["station_number"]][0]
                location_name = station_ditc[measurement["station_number"]][1]
                new_measurements.append({"attributes": { "Name": location_name,
                                                         "NO2": measurement["value"],
                                                         "Tijd": measurement["timestamp_measured"] },
                                        "geometry": {"y": location["y"], "x": location["x"],
                                                    "spatialReference": {
                                                        "wkid": 4326
                                                    }}})

# Sava all the new features.
print(f"Start save {len(new_measurements)} features")
addresult = fl_meetwaarden.edit_features(adds=new_measurements)
print(addresult)
print("Save features complete")