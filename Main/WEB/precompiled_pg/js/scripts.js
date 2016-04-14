$.fn.bars2init = function() {

//$(document).ready(function (){
    
    /* Dropdowns in navigation */
    var dropdowns = $();
    $('nav ul li.nav-item-dropdown').each(function () {
       var th = $(this);
       var position = th.offset();
       var dropdown = th.find('.nav-submenu');
       dropdown.wrap('<div class="nav-dropdown nav-dropdown-hidden"></div>')
               .parent()
               .appendTo('body')
               .css({ 'top' : (position.top+th.height())+'px', 'left' : position.left+'px' });
       dropdown = dropdown.parent();
       dropdown.find('li:first-child').addClass('nav-dropdown-item-first');
       dropdowns = dropdowns.add(dropdown);
       
       th.click(function () {
           if ( dropdown.hasClass('nav-dropdown-hidden') ) {
               dropdowns.not(dropdown).addClass('nav-dropdown-hidden');
               dropdown.removeClass('nav-dropdown-hidden');
           } else {
               dropdowns.addClass('nav-dropdown-hidden');
           }
           dropdowns.trigger('dropdown.visibility');
           return false;
       });
       dropdown.bind('dropdown.visibility', function () {
           if ( $(this).hasClass('nav-dropdown-hidden') ) {
               th.removeClass('nav-item-dropdown-active');
           } else {
               th.addClass('nav-item-dropdown-active');
           }
       });
    });
    $(document).click(function (event) {
        if ( $(event.target).parents('.nav-submenu,.nav-item-dropdown').length===0 ) {
            dropdowns.addClass('nav-dropdown-hidden');
            dropdowns.trigger('dropdown.visibility');
        }
    });
    dropdowns.each(function (){
        var maxWidth = 0;
        $(this).css('display', 'block')
               .find('li').each(function (){
                    var th = $(this);
                    if ( th.width()>maxWidth ) {
                        maxWidth = th.width();
                    }
               }).width(maxWidth);
        $(this).css('display', '');
    });
    
    /* Resizable content wrapper */
    var wrapper = $('.fluid-wrapper');
    if ( wrapper.length ) {
        $(window).resize(function (){
            var height = $(window).height();
            var position = wrapper.offset();
            wrapper.height(height-position.top-32);
        }).resize();
    }
    
    /* Fancy filter buttons */
    var smartbuttonsDropdowns = $('.smartbutton-dropdown a').unbind('click').click(function (event){
        $(this).parent().toggleClass('smartbutton-down').find('.dropdown-bottom').toggleClass('dropdown-bottom-shown');
    }).parent();
    var smartbuttons = $('.smartbutton').not(smartbuttonsDropdowns).mousedown(function (){
        $(this).addClass('smartbutton-down');
    }).bind('mouseup mouseleave',function () {
        smartbuttons.removeClass('smartbutton-down');
    });
    
    /* Scroll for touch devices like iPad, iPhone, iPod... */
    if((navigator.userAgent.match(/iPhone/i)) || (navigator.userAgent.match(/iPod/i)) || (navigator.userAgent.match(/iPad/i))){
        //var scroller = new TouchScroll(document.querySelector(".fluid-wrapper"), {elastic: true});
    }
}

//});