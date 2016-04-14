
// Перемещает курсор в начало, только если TextBox пустой 
function MoveCursorToBeginIfTextBoxEmpty(e) {
    var tb = e.target.id;
    var textBox = document.getElementById(tb);
    if (textBox.value == "__.__.____" | textBox.value == "") {
        textBox.selectionEnd = textBox.selectionStart = 0;
        textBox.focus();
    }
}

function startm(element) {
    $(element).mask("99.99.9999", {
        placeholder: "_", autoclear: false
    });
}
// Очищает TextBox
function ClearDateTxt(element) {
    var partIdTextBox = ""; // часть ID искомого TextBox
    // Если была нажата кнопка очистки TextBoxDatS
    if (element.id.toString().indexOf("imgClearDatS") >= 0) {
        partIdTextBox = "TextBoxDatS";
    }
    // Если была нажата кнопка очистки TextBoxDatPo
    if (element.id.toString().indexOf("imgClearDatPo") >= 0) {
        partIdTextBox = "TextBoxDatPo";
    }
    if (partIdTextBox == "") return true;
    // Находим родителя кнопки, а затем все сестринские элементы
    var siblings = $(element).parent().siblings();
    // Фильтруем сестринские элементы по тэгу input
    var inpElems = siblings.find('input');
    for (var i = 0; i < inpElems.length; i++) {
        // Для оставшихся  элементов проверяем: содержится ли partIdTextBox в ID найденного элемента
        var strId = inpElems[i].id.toString();
        if (strId.indexOf(partIdTextBox) < 0) continue;
        // Если содержится, то очищаем, фокусируемся и переносим курсор вначало
        var textBox = inpElems[i];
        textBox.selectionEnd = textBox.selectionStart = 0;
        $(textBox).mask("99.99.9999", {
            placeholder: "_", autoclear: false
        });
        textBox.value = "";
        textBox.focus();
        return false;
    }
    return true;
}