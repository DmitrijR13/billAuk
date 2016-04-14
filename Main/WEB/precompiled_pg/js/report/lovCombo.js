if ('function' !== typeof RegExp.escape) {
    throw ('RegExp.escape function is missing. Include Ext.ux.util.js file.');
}

Ext.ns('Ext.ux.form');

Ext.ux.form.LovCombo = Ext.extend(Ext.form.ComboBox, {
    checkField: 'checked',
    separator: ',',
    displayValues: null,

    constructor: function (config) {
        config = config || {};
        config.listeners = config.listeners || {};
        Ext.applyIf(config.listeners, {
            scope: this
        , beforequery: this.onBeforeQuery
        , blur: this.onRealBlur
        });
        Ext.ux.form.LovCombo.superclass.constructor.call(this, config);
    },
    initComponent: function () {
        // template with checkbox
        if (!this.tpl) {
            this.tpl =
            '<tpl for=".">'
            + '<div class="x-combo-list-item">'
            + '<img src="' + Ext.BLANK_IMAGE_URL + '" '
            + 'class="ux-lovcombo-icon ux-lovcombo-icon-'
            + '{[values.' + this.checkField + '?"checked":"unchecked"' + ']}">'
            + '<div class="ux-lovcombo-item-text">{' + (this.displayField || 'text') + ':htmlEncode}</div>'
            + '</div>'
            + '</tpl>'
            ;
        }
        // call parent
        Ext.ux.form.LovCombo.superclass.initComponent.apply(this, arguments);
        // remove selection from input field
        this.onLoad = this.onLoad.createSequence(function () {
            if (this.el) {
                var v = this.el.dom.value;
                this.el.dom.value = '';
                this.el.dom.value = v;
            }
        });
    },
    initEvents: function () {
        Ext.ux.form.LovCombo.superclass.initEvents.apply(this, arguments);
        // disable default tab handling - does no good
        this.keyNav.tab = false;
    },
    clearValue: function () {
        this.value = '';
        this.setRawValue(this.value);
        this.store.clearFilter();
        this.store.each(function (r) {
            r.set(this.checkField, false);
        }, this);
        if (this.hiddenField) {
            this.hiddenField.value = '';
        }
        this.displayValues = null;
        this.applyEmptyText();
    },
    getCheckedDisplay: function () {
        return this.displayValues ? this.displayValues.join(this.separator + ' ') : '';
    },
    getCheckedValue: function (field) {
        field = field || this.valueField;
        var c = [];
        // store may be filtered so get all records
        var snapshot = this.store.snapshot || this.store.data;
        snapshot.each(function (r) {
            if (r.get(this.checkField)) {
                c.push(r.get(field));
            }
        }, this);
        return c.join(this.separator);
    },
    onBeforeQuery: function (qe) {
        qe.query = qe.query.replace(new RegExp(RegExp.escape(this.getCheckedDisplay()) + '[ ' + this.separator + ']*'), '');
    },
    onRealBlur: function () {
        this.list.hide();
        var va = [];
        var snapshot = this.store.snapshot || this.store.data;
        Ext.each(this.displayValues, function (v) {
            snapshot.each(function (r) {
                if (v === r.get(this.displayField)) {
                    va.push(r.get(this.valueField));
                }
            }, this);
        }, this);
        this.setValue(va.join(this.separator));
        this.store.clearFilter();
    },
    onSelect: function (record, index) {
        if (this.fireEvent('beforeselect', this, record, index) !== false) {
            record.set(this.checkField, !record.get(this.checkField));
            if (this.store.isFiltered()) {
                this.doQuery(this.allQuery);
            }
            this.setValue(this.getCheckedValue());
            this.fireEvent('select', this, record, index);
        }
    },
    setDisplayValues: function(value) {
        if (value) {
            var vals = value.split(this.separator);
            this.displayValues = [];
            Ext.each(vals, function(val) {
                this.store.each(function (r) {
                    if (r.get(this.valueField) == val) {
                        this.displayValues.push(r.get(this.displayField));
                    }
                }, this);
            }, this);
            
        } else {
            this.displayValues = null;
        }
    },
    setValue: function (v) {
        if (v) {
            v = '' + v;
            if (this.valueField) {
                this.store.clearFilter();
                this.store.each(function (r) {
                    var checked = !(!v.match(
                    '(^|' + this.separator + ')' + RegExp.escape(r.get(this.valueField))
                    + '(' + this.separator + '|$)'))
                    ;
                    r.set(this.checkField, checked);
                }, this);
                this.value = this.getCheckedValue();
                this.setDisplayValues(this.value);
                this.setRawValue(this.getCheckedDisplay());
                if (this.hiddenField) {
                    this.hiddenField.value = this.value;
                }
            }
            else {
                this.value = v;
                this.setRawValue(v);
                if (this.hiddenField) {
                    this.hiddenField.value = v;
                }
            }
            if (this.el) {
                this.el.removeClass(this.emptyClass);
            }
        }
        else {
            if (this.editable) {
                this.store.removeAll();
            }
            else {
                this.clearValue();
            }
        }
    },
    getValue: function () {
        return !!this.value ? this.value.split(',') : null;
    },
    selectAll: function () {
        this.store.each(function (record) {
            record.set(this.checkField, true);
        }, this);
    
        //display full list
        this.doQuery(this.allQuery);
        this.setValue(this.getCheckedValue());
    },
    deselectAll: function () {
        this.clearValue();
    },
    assertValue:function() {
        this.list.hide();
        var va = [];
        var snapshot = this.store.snapshot || this.store.data;

        Ext.each(this.displayValues, function(v) {
            snapshot.each(function(r) {
                if(v === r.get(this.displayField)) {
                    va.push(r.get(this.valueField));
                }
            }, this);
        }, this);
        this.setValue(va.join(this.separator));
        this.store.clearFilter();
    }
});