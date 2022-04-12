import logging
import os

import azure.functions as func

from azure.storage.queue import QueueClient, TextBase64EncodePolicy, TextBase64DecodePolicy

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('The ArcadeButtonHttpTrigger function processed a request.')

    # Get the OBJECTID from the request
    objectid = req.params.get('OBJECTID')
    # Determine the Queue Name from the Feature Service name in the request
    queueName = f"{req.params.get('FeatureService')}queue"

    # Create a QueueClient to send the OBJECTID to the right queue
    connectionString = os.environ["AzureWebJobsStorage"]
    queueClient = QueueClient.from_connection_string(
                                conn_str=connectionString, 
                                queue_name=queueName,
                                message_encode_policy=TextBase64EncodePolicy(),
                                message_decode_policy=TextBase64DecodePolicy()
                            )

    try:
        # Add the OBJECTID to the queue
        logging.info(f"Adding {objectid} to the '{queueName}'")

        # Send the JSON to the queue
        queueClient.send_message(objectid)
        return func.HttpResponse(f"Object met OBJECTID '{objectid}' krijgt status: {req.params.get('FeatureService')}.", status_code=200)
    except Exception as ex:
        return func.HttpResponse(f"This HTTP triggered function failed. No OBJECTID was added to the queue.", status_code=418)