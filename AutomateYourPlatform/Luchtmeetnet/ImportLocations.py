import requests
import datetime
from arcgis import GIS
from arcgis.features import FeatureLayer

gis = GIS('home')
fl_stations = FeatureLayer("https://services.arcgis.com/emS4w7iyWEQiulAb/ArcGIS/rest/services/Meetstations/FeatureServer/6")
fl_meetwaarden = FeatureLayer("https://services.arcgis.com/emS4w7iyWEQiulAb/ArcGIS/rest/services/Meetwaarden/FeatureServer/9")
new_locations = []
start = 1
end = 1
while start <= end:
    stations_response = requests.get(f"https://api.luchtmeetnet.nl/open_api/stations?page={start}")
    if stations_response.ok:
        start = start + 1
        station_json = stations_response.json()
        end = station_json["pagination"]["last_page"]
        for item in station_json["data"]:
            station_id = item["number"]
            station_name = item["location"]

            station_data_response = requests.get(f"https://api.luchtmeetnet.nl/open_api/stations/{station_id}")
            if station_data_response.ok:
                station_data_json = station_data_response.json()

                x = station_data_json["data"]["geometry"]["coordinates"][0]
                y = station_data_json["data"]["geometry"]["coordinates"][1]

                new_locations.append({"attributes": {   "Name": station_name,
                                                        "ID": station_id },
                                        "geometry": {"x": x, "y": y, 
                                        "spatialReference": {
                                                        "wkid": 4326
                                                    }}})
print("Save features")
fl_stations.edit_features(adds=new_locations)
print("Save features complete")