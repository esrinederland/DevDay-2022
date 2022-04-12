import logging
import os

from arcgis.gis import GIS
import azure.functions as func

def main(msg: func.QueueMessage) -> None:
    objectid = msg.get_body().decode('utf-8')
    logging.info(f'Python queue trigger function processed a queue item: {objectid}')

    # Connect to ArcGIS
    gis = GIS(username=os.environ["ArcGISUsername"], password=os.environ["ArcGISPassword"])

    # Get the source and target Feature Services
    sourceFS = gis.content.get(os.environ["SourceFeatureService"])
    targetFS = gis.content.get(os.environ["GoedkeurenFeatureService"])

    # Query the source Feature Service using the objectid
    sourceFeatureSet = sourceFS.layers[0].query(object_ids=f"{objectid}")

    # Add the Feature to the target Feature Service
    addFeaturesResult = targetFS.layers[0].edit_features(adds=sourceFeatureSet.features)
    logging.info(addFeaturesResult)

    # Delete the Feature from the source Feature Service
    sourceFS.layers[0].delete_features(deletes=f"{objectid}")

    logging.info(f"Moved Feature with objectid {objectid} from the source Feature Service to the target Feature Service")
