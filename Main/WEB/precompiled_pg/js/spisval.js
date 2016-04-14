$.fn.initspisval = function()
{
    $('tr.expandable td.first_cell img').click(function(){
        var img = $(this);
        var row = img.parents('td.first_cell').parents('tr').eq(0);
        
        $('.child'+row.attr('id')).toggle();
        if (row.hasClass('expandable'))
        {
            row.removeClass('expandable').addClass('collapsable');
            img.attr('src', $('#imageMinus').attr('src'));
        }
        else
        {
            row.removeClass('collapsable').addClass('expandable');
            img.attr('src', $('#imageAdd').attr('src'));
        }
    });
}

$(function() {
    $.fn.initspisval();
});
