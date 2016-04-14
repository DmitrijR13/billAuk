$.fn.initodn = function(){
    $('#tbl_odn tr.calc').each(function()
    {
        var parent_id = this.id.split('_')[1];
        
        $('#'+this.id+' td:first img').click(function(){
            var rows = $('#tbl_odn tr.child'+parent_id)
                .filter('.past_reval')
                .toggle();
            
            if (rows.filter(':visible').size() > 0)
                $(this).attr('src', $('#imgRedMinus').attr('src'));
            else $(this).attr('src', $('#imgRedPlus').attr('src'));
            
            $(this).showColumnsOdn().scrollTableOdn();
        });
        
        $('#'+this.id+' td:nth-child(2) img').click(function(){
            var rows = $('#tbl_odn tr.child'+parent_id)
                .filter('.future_reval')
                .toggle();  
                
            if (rows.filter(':visible').size() > 0)
                $(this).attr('src', $('#imgBlueMinus').attr('src'));
            else $(this).attr('src', $('#imgBluePlus').attr('src'));  
            $(this).showColumnsOdn().scrollTableOdn();
        });
    });
    
    $('#tbl_odn th.expandable').click(function(){
        var parent_id = this.id.split('_')[1];
        $('#tbl_odn .child'+parent_id).toggle();        
    });
}

$.fn.showColumnsOdn = function(){
    if  ($('#tbl_odn tr.future_reval:visible').add('#tbl_odn tr.past_reval:visible').size() > 0)
        $('#tbl_odn tr .reval').show();
    else $('#tbl_odn tr .reval').hide();
    
    return $(this);
}

$.fn.scrollTableOdn = function() {
    return $(this).each(function() {
        var top = $(this).parents('td').get(0).offsetTop;
        var div = $('#tbl_odn').parents('div').get(0);
        if (div != null) div.scrollTop = top;
    });
}

$(function() {
    $.fn.initodn(); 
});