$(document).ready(function () {
    SetupAccordion();
    if (typeof Sys.WebForms != 'undefined') {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(SetupAccordion);
    }
});
// главный метод настройки аккордиона
function SetupAccordion() {
    // ищем все элементы с заданным классом 
    var accordionList = $(".accordionScopeAdress");
    var activeIndex = [];
    for (var i = 0; i < accordionList.length; i++) {
        // находим сестринский элемент (hidden field) аккордиона
        var hidFld = getSibling(accordionList[i]);
        if (hidFld != null) {
            // извлекаем его значение
            activeIndex[i] = parseInt(hidFld.value);
        }
        // инициализация аккордиона
        initAccordion(accordionList[i], activeIndex[i]);
    }
}

function initAccordion(accordion, indexPane) {
    $(accordion).accordion({
        collapsible: true,
        active: setActivePane(indexPane),
        activate: function (event, ui) {
            var hidField = getSibling(this);
            if (hidField == null) return;
            var index = $(this).children('h3').index(ui.newHeader);
            hidField.value = index;
        }
    });
}

function getSibling(accordionItem) {
    var sibls = $(accordionItem).siblings();
    for (var j = 0; j < sibls.length; j++) {
        if (sibls[j].id.toString().indexOf("hidAccordionIndex") >= 0) {
            if (sibls[j] != null && sibls[j] != 'undefined') {
                return sibls[j];
            }
        }
    }
    return null;
}
function setActivePane(act) {
    if (act == -1 || act == 'undefined' || act == null) {
        return false;
    }
    return act;
}
