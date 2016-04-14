$.fn.initcharges = function () {
    $('#tableCharge tr.calc').each(function () {
        var parent_id = this.id.split('_')[1];

        $('#' + this.id + ' img.past').click(function (event) {
            event.stopPropagation();
            var rows = $('#tableCharge tr.child' + parent_id)
            .filter('.past_reval')
            .toggle();

            if (rows.filter(':visible').size() > 0)
                $(this).attr('src', $('#imgRedMinus').attr('src'));
            else $(this).attr('src', $('#imgRedPlus').attr('src'));
        });

        $('#' + this.id + ' img.future').click(function (event) {
            event.stopPropagation();
            var rows = $('#tableCharge tr.child' + parent_id)
                .filter('.future_reval')
                .toggle();

            if (rows.filter(':visible').size() > 0)
                $(this).attr('src', $('#imgBlueMinus').attr('src'));
            else $(this).attr('src', $('#imgBluePlus').attr('src'));
        });

        /*Для обработки нажатия по строке таблицы*/
        $(this).filter('.clear').click(function () {
            ShowRowInfo(this);
        });
    });

    $('#tableCharge tr.future_reval').filter('.clear').each(function () {
        /*Для обработки нажатия по строке таблицы*/
        $(this).click(function () {
            ShowRowInfo(this);
        });
    });

    $('#tableCharge tr.past_reval').filter('.clear').each(function () {
        /*Для обработки нажатия по строке таблицы*/
        $(this).click(function () {
            ShowRowInfo(this);
        });
    });

}

/*$.fn.scrollChargeTable = function() {
    return $(this).each(function() {
        var top = $(this).parents('td').get(0).offsetTop;
        var div = $('#tableCharge').parents('div').get(0);
        if (div != null) div.scrollTop = top;
    });
}*/

$(function() {
    $.fn.initcharges();
});


function ShowRowInfo(row) {
    var parent_id = row.id.split('_')[1];
    var row_month = $('#month' + parent_id)[0].value;
    var row_year = $('#year' + parent_id)[0].value;
    var dd_date = $('#' + dtControlID_dd)[0].value;
    var nzp_serv = $('#nzpServ' + parent_id)[0].value;
    var nzp_supp = $('#nzpSupp' + parent_id)[0].value;
    if (dd_date != "" && dd_date.substr(4, 2) != '00') {
        var div = $("#descr");
        if (div.size() == 0)
            div = $("<div class='modal_popup' style='text-align: left' id='descr'></div>")
                .dialog({ autoOpen: false, closeOnEscape: true, width: 650, height: 250, maxWidth: 1200, maxHeight: 800, title: "Протокол начисления по услуге", show: "slide", position: 'center' });

        div.html("<table width='100%' height='100%'><tr><td style='text-align: center'><img src='../../images/wait_small.gif' width='16' height='16'/>&nbsp;Подождите, пожалуйста ...</td></tr></table>")
            .dialog("open")
            .load("../../kart/charge/handler2.ashx", { p0: row_month, p1: row_year, p2: dd_date, p3: nzp_serv, p4: nzp_supp });
    }
}