var line = {
    getClientWidth: function () {
        return document.compatMode == 'CSS1Compat' && !window.opera ? document.documentElement.clientWidth : document.body.clientWidth;
    },

    getClientHeight: function () {
        return document.compatMode == 'CSS1Compat' && !window.opera ? document.documentElement.clientHeight : document.body.clientHeight;
    },

    getOffsetTop: function (element) {
        var offset = 0;
        var elem = element;
        do {
            offset += elem.offsetTop;
        } while (elem = elem.offsetParent);
        return offset;
    },

    getOffsetLeft: function (element) {
        var offset = 0;
        var elem = element;
        do {
            offset += elem.offsetLeft;
        } while (elem = elem.offsetParent);
        return offset;
    },

    byId: function (id) {
        return document.getElementById(id);
    },

    isNull: function (value) {
        return value == undefined || value == null || value == '';
    }
}

$.fn.initmain = function () {
    //$('input:button').addClass('button');

    //$('.stretch_vert').stretchVertical();
    $('.max_height').setMaxHeight();

    var isMenuExist = $('.menu').size() > 0;
    if (isMenuExist) {
        var menuTopOffset = $('.menu').offset().top;
        var offset = $('.header').height();
    }

    window.onresize = function () {
        $('.stretch_vert').stretchVertical();
        $('.max_height').setMaxHeight();
        if (isMenuExist) floatMenu(menuTopOffset, offset);
    };

    $('.checkboxspismark').click(function (event) { event.stopPropagation(); });
    $('.checkboxspismarkAll :checkbox').on('change', function () {
        if ($(this).is(':checked'))
            $('.checkboxspismark :checkbox').prop('checked', true);
        else
            $('.checkboxspismark :checkbox').prop('checked', false);
    });
    
    $('.GridViewButton').click(function (event) { event.stopPropagation(); });


    $('.adrCheck2').click(function (event) { event.stopPropagation(); });
    $('.adrCheckAll2 :checkbox').on('change', function () {
        if ($(this).is(':checked'))
            $('.adrCheck2 :checkbox').prop('checked', true);
        else
            $('.adrCheck2 :checkbox').prop('checked', false);
    });


    $('.adrCheck').click(function (event) { event.stopPropagation(); });
    $('.adrCheckAll :checkbox').on('change', function () {
        if ($(this).is(':checked'))
            $('.adrCheck :checkbox').prop('checked', true);
        else
            $('.adrCheck :checkbox').prop('checked', false);
    });

    $('.completionList').hide().css('visibility: hidden');
    Sys.WebForms.PageRequestManager.getInstance().add_beginRequest(showModalPopup);
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(hideModalPopup);
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(showHelpWindow);

    $('div.separateBody').unbind('scroll').scroll(function () {
        $(this).parent().find('div.separateHeader, div.separateFooter').css('left', -1 * $(this).scrollLeft());
    });

    if (isMenuExist) {
        $(window).scroll(function () {
            floatMenu(menuTopOffset, offset);
        });
    }

    if (typeof $.fn.bars2init == "function") {
        $.fn.bars2init();
    }

    $('input:text, select').keydown(function (event) {
        if ((event.which == 13) && (!($('input:text, select').hasClass('AllowedEnter')))) {
            event.preventDefault();
        }
    });

    beautifyInputs();

    $('input:button.input-button, input:submit.input-button').each(function () {
        var div = $("<div class=\"input-button\"></div>");
        var inp = $(this).addClass("input-button").before(div).detach();
        div.append("<div class=\"input-button input-button-left\"></div>", inp.get(0), "<div class=\"input-button input-button-right\"></div>");
    });
}

function beautifyInputs() {
    $('input:text.input-text, input:password.input-text, select.input-text').filter(":visible").each(function () {
        var div = $("<div class=\"input-text\"></div>").css("width", $(this).css("width"));
        var inp = $(this).removeClass("input-text").addClass("input-text-processed").before(div).detach();
        div.append("<div class=\"input-text input-text-left\"></div>", inp.get(0), "<div class=\"input-text input-text-right\"></div>");
    });
}

function ClientActiveTabChangedHandler() {
    beautifyInputs();
}

function floatMenu(menuTopOffset, offset) {
    var scr = $(window).scrollTop();
    if (scr > menuTopOffset) {
        $('.insteadofmenu').css('display', 'block');
        if (window.opera || $.browser.msie) {
            $('.menu_layer, .menu').addClass('floating_menu_opera');
            $('.menu_layer.floating_menu_opera').css('top', scr - offset);
        }
        else {
            $('.menu_layer, .menu').addClass('floating_menu');
        }
    }
    else {
        if (window.opera || $.browser.msie) {
            $('.menu_layer, .menu').removeClass('floating_menu_opera').css('top', 0);
        }
        else {
            $('.menu_layer, .menu').removeClass('floating_menu');
        }
        $('.insteadofmenu').css('display', 'none');
    }
}

function showModalPopup(sender, args) {
    var modalPopupBehavior = $find('programmaticModalPopupBehavior');
    if (modalPopupBehavior != null) modalPopupBehavior.show();
}

function hideModalPopup(sender, args) {
    var modalPopupBehavior = $find('programmaticModalPopupBehavior');
    if (modalPopupBehavior != null) modalPopupBehavior.hide();
}

$(function () {
    $.fn.initmain();
});

function wdn(url) {
    if (url != "") { window.open(url) }
};

$.fn.stretchVertical = function () {
    return $(this);
}

$.fn.stretchVertical2 = function () {
    return $(this).each(function () {
        $(this).not('without_scroll').css('overflow-y', 'auto').end().filter('without_scroll').css('overflow-y', 'hidden');
        try {
            maxHeight = line.getClientHeight() - line.getOffsetTop(this) - $('.footer').height() - 30;
            if (!$(this).hasClass('frame')) maxHeight -= 20;
            if (maxHeight < 100) maxHeight = 100;
        }
        catch (e) {
            maxHeight = 450;
        }
        $(this).css('max-height', maxHeight + 'px');
    });
}

$.fn.setMaxHeight = function () {
    //return $(this);
    return $(this).stretchVertical2().each(function () {
        var maxHeight = $(this).css('max-height');
        $(this).css('max-height', '').css('height', maxHeight);
    });
}

var hiddenShowHelpID = null;
var hiddenShowHelpHeaderID = null;

function showHelpWindow() {
    if (typeof hiddenShowHelpID != 'undefined') {
        var hiddenShowHelp = line.byId(hiddenShowHelpID);
        if (hiddenShowHelp != null) {
            if (hiddenShowHelp.value == "1") {
                var title = "";
                var header = line.byId(hiddenShowHelpHeaderID);
                if (header != null && !line.isNull(header.value)) title = header.value;
                $("#dialog_help").dialog({ autoOpen: true, width: 600, height: 300, maxHeight: 300, title: title });
                hiddenShowHelp.value = "0";
            }
        }
    }
}

function CheckStatusMain(sender, args) {
    var argument = "";
    if (!theForm.onsubmit || (theForm.onsubmit() != false)) {
        argument = theForm.__EVENTARGUMENT.value;
    }
    if (typeof spisokId != 'undefined' & argument.substr(0, 6) == 'Delete') {
        if (args.get_postBackElement().id == "") {
            args.set_cancel(true);
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            if (!prm.get_isInAsyncPostBack())
                hideModalPopup();
        }
        else if (args.get_postBackElement().id == spisokId) {
            if (!confirm("Вы подтверждаете удаление?")) {
                args.set_cancel(true);
                hideModalPopup();
            }
        }
    }

    if (typeof checkStatus === 'function')
        checkStatus(args, argument);
}

function navigateConfirm() {
    $(".navigate").FormNavigate({
        message: "Содержимое было изменено!\nВы уверены, что хотите покинуть страницу без сохранения?",
        aOutConfirm: "a.ignore, .actionPanel .actions .actBody a.item:has('.ignore')"
    });
}

function ignore_modified() {
    var root = window.addEventListener || window.attachEvent ? window : document.addEventListener ? document : null;
    if (typeof (root.onbeforeunload) != "undefined") root.onbeforeunload = null;
    return true;
}

function OnActionClickMain(actionId, event) {
    var result = true;
    var isProcessed = true;
    switch (actionId) {
        case 15: result = confirm("Вы подтверждаете сброс пароля?"); break;
        case 26: result = confirm("Вы подтверждаете удаление записи?"); break;
        case 61:
        case 170:
            result = confirm("Вы подтверждаете сохранение?"); break;
        case 70:
            var address = "";
            if (typeof selectedAddressID !== "undefined") address = line.byId(selectedAddressID);
            if (!line.isNull(address)) address = $(address).text();
            else address = "";

            var numLs = "";
            if (typeof selectedNumLsID !== "undefined") numLs = line.byId(selectedNumLsID);
            if (!line.isNull(numLs)) numLs = $(numLs).text();
            else numLs = "";

            var msg = "Вы действительно хотите выполнить расчет";
            if (!line.isNull(numLs)) msg += " лицевого счета";
            else msg += " лицевых счетов";
            if (!line.isNull(address)) msg += " по адресу " + address + "?";
            else msg += "?";

            result = confirm(msg);
            break;
        case 90: result = confirm("Вы подтверждаете удаление оплат?"); break;
        case 158: result = confirm("Вы подтверждаете удаление?"); break;
        case 169: result = confirm("Вы подтверждаете добавление?"); break;
        default: isProcessed = false;
    }

    if (!isProcessed)
        if (typeof OnActionClick === 'function') {
            result = OnActionClick(actionId);
        }

    if (!result) {
        event.stopPropagation ? event.stopPropagation() : (event.cancelBubble = true);
    }
    return result;
}