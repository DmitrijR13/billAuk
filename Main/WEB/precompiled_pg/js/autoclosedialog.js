if (localStorage.getItem("AutoClosed") == "true") {
    var realConfirm = window.confirm;
    window.confirm = function () {
        window.confirm = realConfirm;
        return true;
    };
};


