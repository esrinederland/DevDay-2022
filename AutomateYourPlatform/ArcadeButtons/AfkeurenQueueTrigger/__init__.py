import logging
import os

from arcgis.gis import GIS
from arcgis.geocoding import reverse_geocode
import azure.functions as func

def main(msg: func.QueueMessage) -> None:
    objectid = msg.get_body().decode('utf-8')
    logging.info(f'Python queue trigger function processed a queue item: {objectid}')

    # Connect to ArcGIS
    gis = GIS(username=os.environ["ArcGISUsername"], password=os.environ["ArcGISPassword"])

    # Get the source and target Feature Services
    sourceFS = gis.content.get(os.environ["SourceFeatureService"])
    targetFS = gis.content.get(os.environ["AfkeurenFeatureService"])

    # Query the source Feature Service using the objectid
    sourceFeatureSet = sourceFS.layers[0].query(object_ids=f"{objectid}")

    # Get the Point geometry from the source Feature Set
    feature = sourceFeatureSet.features[0]
    geometry = feature.geometry
    geometry["spatialReference"] = sourceFeatureSet.spatial_reference
    geocodeResult = reverse_geocode(geometry)

    # Add the geocode results to the Feature attributes
    feature.attributes["Adres"] = geocodeResult["address"]["Address"]
    feature.attributes["Postcode"] = geocodeResult["address"]["Postal"]
    feature.attributes["Plaatsnaam"] = geocodeResult["address"]["City"]

    # Add the Feature to the target Feature Service
    addFeaturesResult = targetFS.layers[0].edit_features(adds=[feature])
    logging.info(addFeaturesResult)

    # Delete the Feature from the source Feature Service
    sourceFS.layers[0].delete_features(deletes=f"{objectid}")

    logging.info(f"Moved Feature with objectid {objectid} from the source Feature Service to the target Feature Service")
