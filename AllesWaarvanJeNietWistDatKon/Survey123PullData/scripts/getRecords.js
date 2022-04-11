function getPreviousRecord(featureLayer, token, username, debugmode){

    var xmlhttp = new XMLHttpRequest();

    var url = featureLayer + "/query?where=username='" + username + "'&orderByFields=objectid+desc&resultRecordCount=1&outFields=*&f=json"
    if (token){
        url = url + "&token=" + token;
    }

    xmlhttp.open("GET",url,false);
    xmlhttp.send();

    if (xmlhttp.status !== 200) {
        return (debugmode? xmlhttp.status:"");
    } 
    else {
        var responseJSON = JSON.parse(xmlhttp.responseText);
        if (responseJSON.error){
            return (debugmode? JSON.stringify(responseJSON.error):"");
        } 
        else {
            if (responseJSON.features[0]){
                return JSON.stringify(responseJSON.features[0]);
            }
            else {
                return (debugmode? "Geen objecten gevonden":"");
            }
        }
    }
}

function getLastYearsRecord(featureLayer, gmbIdent, token,debugmode){

    var xmlhttp = new XMLHttpRequest();
    var lastYear = new Date().getFullYear() - 1;
    // GBMIDENT%3D%27GBM9667%27+AND+EXTRACT%28YEAR+FROM+GBVDAT%29%3D2020
    var url = featureLayer + "/query?where=gbmident='"+ gmbIdent + "' AND EXTRACT(YEAR FROM gbvdat) = " + lastYear + "&resultRecordCount=1&outFields=*&f=json"
    if (token){
        url = url + "&token=" + token;
    }

    xmlhttp.open("GET",url,false);
    xmlhttp.send();

    if (xmlhttp.status !== 200) {
        return (debugmode? xmlhttp.status:"");
    } 
    else {
        var responseJSON = JSON.parse(xmlhttp.responseText);
        if (responseJSON.error){
            return (debugmode? JSON.stringify(responseJSON.error):"");
        } 
        else {
            if (responseJSON.features[0]){
                return JSON.stringify(responseJSON.features[0]);
            }
            else {
                return (debugmode? null:"");
            }
        }
    }
}

function getAddress(point, token) {

    if(point && token) {
        var xmlhttp = new XMLHttpRequest();
        // get point

        //location=103.8767227,1.3330736
        
        var geocodeUrl = `https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode?f=json&location=${point.x},${point.y}`;
        if (token){
            geocodeUrl = geocodeUrl + "&token=" + token;
        }

        xmlhttp.open("POST",geocodeUrl,false);
        xmlhttp.send();
    
        if (xmlhttp.status !== 200) {
            return (debugmode? xmlhttp.status:"");
        } 
        else {
            var responseJSON = JSON.parse(xmlhttp.responseText);
            if (responseJSON.error){
                return (debugmode? JSON.stringify(responseJSON.error):"");
            } 
            else {
                return JSON.stringify(responseJSON.address)
                if (responseJSON.features[0]){
                    return JSON.stringify(responseJSON.features[0]);
                }
                else {
                    return (debugmode? null:"");
                }
            }
        }
    }
    
    

}