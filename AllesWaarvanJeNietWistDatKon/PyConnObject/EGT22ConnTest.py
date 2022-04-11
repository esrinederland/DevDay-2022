import arcgis

_portalUrl = "https://devteam.esri.nl/portal"
_profileName = "devteam_maarten_dev"
_itemId = "c393f531573848c2a34b94cf3fc7eb82"

print("Connecting to portal")
gis = arcgis.GIS(_portalUrl, profile=_profileName)

print(f"Successfully logged into '{gis.properties.portalHostname}' via the '{gis.properties.user.username}' user") 

items = gis.content.search(_itemId)
print(f"Found {len(items)} items")

serviceItem = items[0]
print(f"Found service item: {serviceItem.title}")

serviceUrl = serviceItem.url
print("Getting layers")

##Getting the layers through the gis._con object:
result = gis._con.post(serviceUrl)

for layer in result["layers"]:
    print(f"layer {layer['id']}: {layer['name']}")

print("script complete")

