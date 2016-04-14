$(function() {
    $.fn.initperekidka();
});

$.fn.initperekidka = function()
{
    $('div.separateBody').scroll(
        function() {
            $('div.separateHeader').add('div.separateFooter').css('left',-1*$(this).scrollLeft());
        }
    );
}