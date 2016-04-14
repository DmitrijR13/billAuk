function _findElByTag(inpTag,inpPatt) {
 var theEl = document.getElementsByTagName(inpTag);
 var sRet = '0';
 for (j = theEl.length-1; j >= 0 ; j--)
 {
  var curEl = theEl[j];
  if ( inpPatt.test(curEl.id) ) { foundEl = curEl; sRet = '1'; break; }
 }
 if (sRet != '1') { return null; }
 return foundEl
}

function _setRowCssClass(curRow, classForAdd, classForDelete) {
    if ( !$(curRow).hasClass('selec') )
    {
        $(curRow).removeClass(classForDelete); 
        $(curRow).addClass(classForAdd); 
    } 
}

function _doItOnClick(curRow,myShowPatt,myHidePatt,rowStr) {
 myHide = _findElByTag('input',myHidePatt);
 showInfoId = _findElByTag('span',myShowPatt);

 var pagPatt = /kppgnum/i;
 numPage   = _findElByTag('input',pagPatt);
 
 lblRMnYr  = _findElByTag('input',/kprmnyr/i);
 lblRMnYr.value = $(curRow).children(':first-child').text();

 if ( !showInfoId ) { 
  document.getElementById('ctl00_ContentLeft_Label1').innerText = 'Click! Label not found!';
 }
 else {
  showInfoId.innerText = 'Click! Anes found! Row=' + rowStr + ' t:' + document.getElementById('ctl00_ContentLeft_posGrid').value
   + ' hBB: '+ myHide.value + ' id: '+ showInfoId.id + ' pg= ' + numPage.value + ' MnYr= ' + lblRMnYr.value;
 } 
 
}

function retClientID() {
    var showInfoId = document.getElementById(myShowLabel);

    var myPosGrid = document.getElementById("<%= posGrid.ClientID %>");
    return myPosGrid.id;
}

function __doSpisokPostBack(eventArgument) {

    var pagPatt = /kppgnum/i;
    numPage   = _findElByTag('input',pagPatt);
    if (numPage) { 
    //if (numPage.value == '51' ) { return ; }
  //  if (numPage.value == '52' ) { return ; }
    if (numPage.value == '54' ) { return ; }
    if (numPage.value == '55' ) { return ; }
    if (numPage.value == '127' ) { return ; }
    //if (numPage.value == '59' ) { return ; }
    if (numPage.value == '61' ) { return ; }
   // if (numPage.value == '62' ) { return ; }
   // if (numPage.value == '63' ) { return ; }
   if (numPage.value == '96' ) { return ; }
    if (numPage.value == '64' ) { return ; }
    if (numPage.value == '121') { return ; }
    if (numPage.value == '132') { return ; }
    if (numPage.value == '126') { return ; }
    if (numPage.value == '161') { return ; }
    }

    var theForm = document.forms['aspnetForm'];
    if (!theForm) { theForm = document.aspnetForm; }
    
    if (!theForm.onsubmit || (theForm.onsubmit() != false)) {

        var curid = retMyID();
        var eventTarget = curid.replace( /_/g, '$' );
        
        theForm.__EVENTTARGET.value = eventTarget;
        theForm.__EVENTARGUMENT.value = eventArgument;
        theForm.submit();
    }

}
