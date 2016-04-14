var hiddenSelectedObjectType = pmPlacemarks = null;
var panelMapId = pm_placemarks = selectedObjectType = dd_mode_id = btnSavePm_id = btnFind_id = null;
var uid = -100;

var lineMap = {
    map: null,
    typeControl: null,
    toolBar: null,
    zoom: null,
    scaleLine: null,
    button: null,
    buttonPolygon: null,
    polygons: null,
    
    viewMode: null, // 1 - редактирование, 2 - поиск, 3 - просмотр
    placemarks: null,
    selectedObjectType: null,
    placemark: null,
    currentZoom: 10,
    activeTool: null,
    
    //x: 49.152336, /* Kazan */
    //y: 55.790825,
    
    //x: 48.519507, /* Zelenodolsk */
    //y: 55.845776,
    
    x: 0,
    y: 0,
    
    showMap: function()
    {
        try {
            var a = YMaps;
        }
        catch(err) {
            //alert("Карты временно не доступны! Проверьте подключение к сети Интернет!");
            return false;
        }
        $('.map').css('display', 'block');
        //$('#YMapsID').height(480);
        $('#YMapsID').css('height', '100%');

        lineMap.map = new YMaps.Map(document.getElementById("YMapsID"));
        lineMap.map.setZoom(lineMap.currentZoom);

        YMaps.Events.observe(lineMap.map, lineMap.map.Events.DragEnd, function(map, event){
            lineMap.x = map.getCenter().getX();
            lineMap.y = map.getCenter().getY();
        });
        
        var style = new YMaps.Style("default#greenPoint");
        style.polygonStyle = new YMaps.PolygonStyle();
        style.polygonStyle.fill = 1;
        style.polygonStyle.outline = 1;
        style.polygonStyle.strokeWidth = 5;
        style.polygonStyle.strokeColor = "00ff0099";
        style.polygonStyle.fillColor = "00ff0022";
        YMaps.Styles.add("polygon#Area", style);
        
        YMaps.Events.observe(lineMap.map, lineMap.map.Events.Click, function(map, event){
            if (lineMap.activeTool == 'polygon') {
                var polygon = lineMap.addPolygon(--uid, [[event.getCoordPoint().getX(), event.getCoordPoint().getY()]], "", true);
                lineMap.buttonPolygon.deselect();
                polygon.startEditing();
                lineMap.activeTool = null;
            }
            else if (lineMap.viewMode == 2) {
                map.removeAllOverlays();
                lineMap.placemark = lineMap.addPlacemark(event.getCoordPoint().getX(), event.getCoordPoint().getY(), "", "Найти дома в окрестности данной точки", true);  
                lineMap.updateCoord();
                lineMap.checkButton();
            }
        });     
        
        lineMap.map.enableScrollZoom();
        
        lineMap.addControls();
        lineMap.updateMap();
        
        return true; 
    },
    
    addControls: function()
    {
        if (lineMap.map == null) return;
        
        lineMap.typeControl = new YMaps.TypeControl();
        lineMap.map.addControl(lineMap.typeControl);
    
        lineMap.button = new YMaps.ToolBarButton({
            caption: 'Добавить метку',
            hint: 'Добавить местоположение объекта в центр карты',
            shown: false
        });
        YMaps.Events.observe(lineMap.button, lineMap.button.Events.Click, lineMap.addPlacemarkToCenter);

        lineMap.buttonPolygon = new YMaps.ToolBarButton({
            icon: line.byId('imagePolygon').src,
            hint: 'Добавить область',
            shown: false
        });
        YMaps.Events.observe(lineMap.buttonPolygon, lineMap.buttonPolygon.Events.Click, function() {
            lineMap.activeTool = 'polygon';
            this.select();
        });
        
        lineMap.checkButton();

        lineMap.toolBar = new YMaps.ToolBar();
        lineMap.toolBar.add(lineMap.button);
        lineMap.toolBar.add(lineMap.buttonPolygon);
        lineMap.map.addControl(lineMap.toolBar);

        lineMap.zoom = new YMaps.Zoom();
        lineMap.map.addControl(lineMap.zoom);
    
        lineMap.scaleLine = new YMaps.ScaleLine()
        lineMap.map.addControl(lineMap.scaleLine);
    },
    
    updateMap: function() {
        try {
            var a = YMaps;
        }
        catch(err) {
            return false;
        }
        if (lineMap.map == null) return;
        
        lineMap.currentZoom = lineMap.map.getZoom();
        
        if (lineMap.viewMode == 1 && lineMap.selectedObjectType != "") $('#'+btnSavePm_id).show(); else $('#'+btnSavePm_id).hide();
        if (lineMap.viewMode == 2) $('#'+btnFind_id).show(); else $('#'+btnFind_id).hide();
        
        var i;
        for (i = 0; i < lineMap.placemarks.length; i++) {
            if (lineMap.placemarks[i][3] == "search") {
                for (var j = i+1; j < lineMap.placemarks.length; j++) {
                    lineMap.placemarks[j-1] = lineMap.placemarks[j];
                }
                lineMap.placemarks.pop();
            }
            else lineMap.placemarks[i][3] == "";
        }
        lineMap.saveJSON();
            
        if (lineMap.viewMode == 1)
            lineMap.addPlacemarks(lineMap.placemarks);
        else if (lineMap.viewMode == 3)
            lineMap.addPlacemarks(lineMap.placemarks);
        else
            lineMap.addPlacemarks(new Array());
        
        lineMap.setCenter();
        lineMap.checkButton();
    },
    
    setCenter: function() {
        lineMap.map.setCenter(new YMaps.GeoPoint(lineMap.x, lineMap.y), lineMap.currentZoom);
    },
    
    checkButton: function() {
        if (    (lineMap.viewMode == 2 && lineMap.placemark == null) || 
                (lineMap.viewMode == 1 && lineMap.placemark == null && lineMap.selectedObjectType == "1")  ) lineMap.button.show();
        else lineMap.button.hide();
        
        if (lineMap.viewMode == 1 && lineMap.selectedObjectType == "3") lineMap.buttonPolygon.show();
        else lineMap.buttonPolygon.hide();
    },
    
    addPlacemark: function(x, y, placemarkID, placemarkDescription, draggable){        
    
        var s = new YMaps.Style();
        var template;
        if (draggable && (lineMap.viewMode == 1 || lineMap.viewMode == 2))
            template = new YMaps.Template("<div>$[description]</div><div><a id='buttonDelete' href='#'>Удалить</a></div>");
        else template = new YMaps.Template("<div>$[description]</div>");

        s.balloonContentStyle = new YMaps.BalloonContentStyle(template);
        
        s.iconStyle = new YMaps.IconStyle();
        //if (placemarkID == lineMap.selectedRowPos || lineMap.viewMode == 2) s.iconStyle.href = "/images/go_home_cur.png";
        if (lineMap.viewMode == 1 || lineMap.viewMode == 2) s.iconStyle.href = line.byId('imageHomeCur').src;
        else s.iconStyle.href = line.byId('imageHome').src;
        s.iconStyle.size = new YMaps.Point(28, 39);
        s.iconStyle.offset = new YMaps.Point(-4, -39);

        var placemark = new YMaps.Placemark(new YMaps.GeoPoint(x, y), {draggable: draggable, style: s});
        placemark.id = placemarkID;
        placemark.description = placemarkDescription;
        placemark.name = "";
        
        YMaps.Events.observe(placemark, placemark.Events.DragEnd, lineMap.updateCoord);
        
        YMaps.Events.observe(placemark, placemark.Events.BalloonOpen, function(){
            $('#buttonDelete').click(lineMap.deletePlacemark);
        });
        
        lineMap.map.addOverlay(placemark);
        return placemark;
    },    
    
    addPlacemarkToCenter: function(){
        
        var placemarkID = --uid;
        var desc = "";
        if (lineMap.viewMode == 1) desc = "";
        else if (lineMap.viewMode == 2) desc = "Найти дома в окрестности данной точки";
        lineMap.placemark = lineMap.addPlacemark(lineMap.map.getCenter().getX(), lineMap.map.getCenter().getY(), placemarkID, desc, true);
        lineMap.updateCoord();
        lineMap.checkButton();
    },
    
    addPolygon: function(polygonID, points, description, editable){        
    
        var geoPoints = new Array();
        for (var i = 0; i < points.length; i++) geoPoints.push(new YMaps.GeoPoint(points[i][0], points[i][1]))
        
        var polygon = new YMaps.Polygon(geoPoints, {
            style: "polygon#Area",
            hasHint: 1,
            hasBalloon: 0
        });

        polygon.id = polygonID;
        polygon.name = description;
        polygon.description = "";

        lineMap.map.addOverlay(polygon);
        polygon.setEditingOptions({
            drawing: true,
            maxPoints: 100
        });
        if (editable) {
            polygon.setEditingOptions({
                 menuManager: function (index, menuItems) {
                    menuItems.push({
                        id: "StopEditing",
                        title: "<span style=\"white-space:nowrap;\">Завершить редактирование<span>",
                        onClick: function (polygon, pointIndex) {
                            polygon.stopEditing();
                            lineMap.saveOverlay(polygon);
                        }
                    });
                    menuItems.push({
                        id: "DeletePolygon",
                        title: "<span style=\"white-space:nowrap;\">Удалить область<span>",
                        onClick: function (polygon, pointIndex) {
                            polygon.getMap().removeOverlay(polygon);
                            lineMap.deleteOverlay(polygon);
                        }
                    });
                    return menuItems;
                }
            });
            YMaps.Events.observe(polygon, polygon.Events.Click, function(polygon, event){
                if (!polygon.isEditing()) polygon.startEditing();
            }); 
        }
        return polygon;
    },
    
    updateCoord: function(){
        var isFound = false;
        
        var status;
        if (lineMap.viewMode == 1) status = "changed";
        else if (lineMap.viewMode == 2) status = "search";
            
        for (var i = 0; i < lineMap.placemarks.length; i++) {
            if (lineMap.placemarks[i][0] == lineMap.placemark.id) {
                lineMap.updateOverlayInPlacemarks(lineMap.placemark, i, status);
                isFound = true;
                break;
            }
        }
        if (!isFound) {
            lineMap.placemarks.push(new Array());
            lineMap.updateOverlayInPlacemarks(lineMap.placemark, lineMap.placemarks.length - 1, status);
        }
        lineMap.saveJSON();
    },
    
    addPlacemarks: function(placemarks) {
        lineMap.map.removeAllOverlays();
        var draggable, placemark;
        lineMap.placemark = null;
        for (var i = 0; i < placemarks.length; i++) {
            if (placemarks[i][1] == -2) {
                if (lineMap.x == 0 && lineMap.y == 0) {
                    lineMap.x = placemarks[i][4][0][0];
                    lineMap.y = placemarks[i][4][0][1];
                }
            }
            else if (placemarks[i][3] != "deleted") {
                if (placemarks[i][1] == 1) { // placemark
                    draggable = (lineMap.viewMode == 1 /*&& placemarks[i][0] == lineMap.selectedRowPos*/);
                    placemark = lineMap.addPlacemark(placemarks[i][4][0][0], placemarks[i][4][0][1], placemarks[i][0], placemarks[i][2].replace('&lt;', '<').replace('&gt;', '>'), draggable);
                    if (draggable /*|| placemarks[i][0] == lineMap.selectedRowPos*/) lineMap.placemark = placemark;
                }
                else if (placemarks[i][1] == 2) { // polyline
                }
                else if (placemarks[i][1] == 3) { // polygon
                    lineMap.addPolygon(placemarks[i][0], placemarks[i][4], placemarks[i][2].replace('&lt;', '<').replace('&gt;', '>'), (lineMap.viewMode == 1));
                }
            }
        }
        if (lineMap.placemark != null) {
            lineMap.x = lineMap.placemark.getCoordPoint().getX();
            lineMap.y = lineMap.placemark.getCoordPoint().getY();
        }
        else if (placemarks.length > 0 && placemarks[0][1] != -2) {
            lineMap.x = placemarks[0][4][0][0];
            lineMap.y = placemarks[0][4][0][1];
        }
    },
    
    saveJSON: function() {
        if (pmPlacemarks != null) {
            var s = JSON.stringify(lineMap.placemarks)
            s = s.replace('\\\/', '\/').replace('<', '&lt;').replace('>', '&gt;');
            pmPlacemarks.value = s;
        }
    },
    
    updateOverlayInPlacemarks: function(overlay, index, status) {
        if (overlay instanceof YMaps.Placemark) { // placemark
            lineMap.placemarks[index][0] = overlay.id;
            lineMap.placemarks[index][1] = 1;
            lineMap.placemarks[index][2] = overlay.description;
            lineMap.placemarks[index][3] = status;
            lineMap.placemarks[index][4] = new Array();
            lineMap.placemarks[index][4].push([overlay.getCoordPoint().getX(),overlay.getCoordPoint().getY()]);
        }
        else if (overlay instanceof YMaps.Polyline) { // polyline
        }
        else if (overlay instanceof YMaps.Polygon) { // polygon
            lineMap.placemarks[index][0] = overlay.id;
            lineMap.placemarks[index][1] = 3;
            lineMap.placemarks[index][2] = overlay.description;
            lineMap.placemarks[index][3] = status;
            lineMap.placemarks[index][4] = new Array();
            var points = overlay.getPoints();
            for (var j in points) lineMap.placemarks[index][4].push([points[j].getX(), points[j].getY()]);
        }
    },
    
    saveOverlay: function(overlay) {
        var isFound = false;
        
        for (var i = 0; i < lineMap.placemarks.length; i++) {
            if (lineMap.placemarks[i][0] == overlay.id) {
                lineMap.updateOverlayInPlacemarks(overlay, i, "changed");
                isFound = true;
                break;
            }
        }
        if (!isFound) {
            lineMap.placemarks.push(new Array());
            lineMap.updateOverlayInPlacemarks(overlay, lineMap.placemarks.length - 1, "changed");
        }
        lineMap.saveJSON();
    },
    
    deleteOverlay: function(overlay) {
        for (var i = 0; i < lineMap.placemarks.length; i++) {
            if (lineMap.placemarks[i][0] == overlay.id) {
                if (lineMap.placemarks[i][0] > 0 && lineMap.placemarks[i][3] != "search")
                    lineMap.placemarks[i][3] = "deleted";
                else {
                    for (var j = i; j < lineMap.placemarks.length - 1; j++)
                        lineMap.placemarks[j] = lineMap.placemarks[j+1];
                        lineMap.placemarks.pop();
                }
                lineMap.saveJSON();
                return;
            }
        }
    },
    
    deletePlacemark: function(){
        if (lineMap.placemark != null) {
            lineMap.map.removeOverlay(lineMap.placemark);
            lineMap.deleteOverlay(lineMap.placemark);
            lineMap.placemark = null;
            lineMap.checkButton();
        }
    },
    
    readData: function() {
        var hiddenSelectedObjectType = line.byId(selectedObjectType);
        if (hiddenSelectedObjectType != null && !line.isNull(hiddenSelectedObjectType.value)) lineMap.selectedObjectType = hiddenSelectedObjectType.value;
        else lineMap.selectedObjectType = "";
        
        try {
            lineMap.viewMode = parseInt(line.byId(dd_mode_id).value);
        }
        catch(e) {
            lineMap.viewMode = 0;
        }
        
        $('#'+dd_mode_id).change(function() {
            lineMap.viewMode = this.value;
            lineMap.updateMap();
        });

        pmPlacemarks = line.byId(pm_placemarks);
        if (pmPlacemarks != null && !line.isNull(pmPlacemarks.value)) {
            try {
                lineMap.placemarks = JSON.parse(pmPlacemarks.value);
            }
            catch(err) {
                lineMap.placemarks = new Array();
            }
        }
        else lineMap.placemarks = new Array();
    }
}

function initMap()
{
    lineMap.readData();
    lineMap.showMap();
}

function updateMap()
{
    lineMap.readData();
    lineMap.updateMap();
}

/*function closeMap()
{
    var panelMap = line.byId(panelMapId);
    if (panelMap) $(panelMap).css('display', 'none');
    try  {
        lineMap.map.destructor();
    }
    catch(err) {}
}*/