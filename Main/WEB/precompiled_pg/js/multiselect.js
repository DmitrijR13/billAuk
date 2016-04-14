var curVisible=null;
var curObjID=null;

//document.attachEvent('onclick', handleClick);
if (document.addEventListener)
{ document.addEventListener('click', handleClick); } else { document.attachEvent('onclick', handleClick); }

// Detect if the browser is IE or not.
// If it is not IE, we assume that the browser is NS.
var IE = document.all ? true : false


// If NS -- that is, !IE -- then set up for mouse capture
if (!IE) document.captureEvents(Event.MOUSEMOVE)


function findPos(obj) {
	var curleft = curtop = 0;
	if (obj.offsetParent) {
		curleft = obj.offsetLeft
		curtop = obj.offsetTop
		while (obj = obj.offsetParent) {
			curleft += obj.offsetLeft
			curtop += obj.offsetTop
		}
	}
	return [curleft,curtop];
}

function placeDiv(objid) {
    if(curObjID == objid){ removeDiv(curVisible); return; }
    if(curVisible!=null) { removeDiv(curVisible); }

	var dd  = document.getElementById(objid);
	var div = document.getElementById(objid+'div');
	
	var checkboxes = div.getElementsByTagName('input');
	
	for (i=0; i<checkboxes.length; i++) {
		if(dd.value.match(checkboxes[i].value)!=null) {
			checkboxes[i].checked=true;
		}
	}

	setLyr(dd,div);
	showItem(div);
	div.focus();
	curVisible=div;
	curObjID=objid;
}

function removeDiv(div) {
	var dd  = document.getElementById(div.id.replace('div',''));
	var checkboxes = div.getElementsByTagName('input');
	var returnArray=new Array(0);
	
	for (i=0; i<checkboxes.length; i++) {
		if(checkboxes[i].checked) {
			returnArray.push(checkboxes[i].value);
		}
	}
	
	dd.value = returnArray.join(';');
	
	hideItem(div);
	curVisible=null;
	curObjID=null;
	
	//__doPostBack(dd.getAttribute('alt'),'@@@AutoPostBack');
	PerformPostActions(dd.getAttribute('alt'));
}

function setLyr(obj,lyr) {
	var coors = findPos(obj);
	coors[1] += 22;
	lyr.style.top = coors[1] + 'px';
	lyr.style.left = coors[0] + 'px';
}

function showItem(obj) {
	obj.style.visibility='visible';
}

function hideItem(obj) {
	obj.style.visibility='hidden';
}

function handleClick() {
	if(curVisible!=null) //is a div visible?
	{
		var r = {l: curVisible.offsetLeft, t: curVisible.offsetTop, r: curVisible.offsetWidth, b: curVisible.offsetHeight};
		//var r = {l: line.getOffsetLeft(curVisible), t: line.getOffsetTop(curVisible), r: curVisible.offsetWidth, b: curVisible.offsetHeight};
		var curVisibleOP = curVisible.offsetParent; 
		if (curVisibleOP!=null){
		    r.l += curVisibleOP.offsetLeft;
		    r.t += curVisibleOP.offsetTop;
		}
		r.r += (r.l+18);
		r.b += r.t;
		r.t -= 22;
		
		var p = getMouseXY(document);
		
		if( (p.x>r.r) || (p.x<r.l) || (p.y>r.b) || (p.y<r.t) )  //no hit!
		{
			removeDiv(curVisible);
		}
	}	
}

function getMouseXY(e) {
  if (IE) { // grab the x-y pos.s if browser is IE
    tempX = event.clientX + document.body.scrollLeft;
    tempY = event.clientY + document.body.scrollTop;
  } else {  // grab the x-y pos.s if browser is NS
    tempX = e.pageX;
    tempY = e.pageY;
  }  
  // catch possible negative values in NS4
  if (tempX < 0){tempX = 0;}
  if (tempY < 0){tempY = 0;}  
  // show the position values in the form named Show
  // in the text fields named MouseX and MouseY
  return {x: tempX, y: tempY};
}



//document.onclick = handleClick;
