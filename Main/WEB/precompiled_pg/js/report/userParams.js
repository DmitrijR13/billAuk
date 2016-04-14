Ext.ns('Bars.KP50.report');

Bars.KP50.report.UserParam = Ext.extend(Ext.util.Observable, {
    userParam: null,
    isForm: false,
    
    constructor: function (config) {
        Ext.apply(this, config);
        Bars.KP50.report.UserParam.superclass.constructor.call(this);
    },

    createField: Ext.emptyFn,
    
    getDefaultConfig: function() {
        return { anchor: '100%', boxMaxWidth: 400 };
    }
});


Bars.KP50.report.DateTimeField = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        return new Ext.form.DateField(Ext.apply(this.getDefaultConfig(), {
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            value: this.userParam.Value,
            allowBlank: !this.userParam.Require,
            format: 'd.m.Y'
        }));
    }
});

Bars.KP50.report.TextField = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        return new Ext.form.TextField(Ext.apply(this.getDefaultConfig(), {
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            value: this.userParam.Value,
            allowBlank: !this.userParam.Require
        }));
    }
});

Bars.KP50.report.ComboBox = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        var config = Ext.apply({}, this.userParam.ComboBoxConfig);

        Ext.apply(config, {
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            allowBlank: !this.userParam.Require,
            editable: false,
            triggerAction: 'all'
        }, this.getDefaultConfig());

        if (this.userParam.StoreData) {
            config.store.autoLoad = false;
            config.value = this.userParam.Value;
        }

        var combobox = new Ext.form.ComboBox(config);
        if (this.userParam.Value && combobox.mode === "remote") {
            combobox.getStore().on('load', function () {
                combobox.setValue(this.userParam.Value);
            }, this, { single: true });
        }
        
        if (this.userParam.StoreData) {
            combobox.getStore().loadData(this.userParam.StoreData, false);
        }

        return combobox;
    }
});

Bars.KP50.report.PeriodField = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        
        var beginValue, endValue;
        var value = this.userParam.Value;
        if (value) {
            var vals = value.split(';');
            beginValue = Date.parseDate(vals[0], 'd.m.Y');
            if (vals.length === 2) {
                endValue = Date.parseDate(vals[1], 'd.m.Y');
            }
        }

        var begin = new Ext.form.DateField({ anchor: '100%', format: 'd.m.Y', value: beginValue });
        var end;
        if (endValue) 
            end = new Ext.form.DateField({ anchor: '100%', format: 'd.m.Y', value: endValue });
        else 
            end = new Ext.form.DateField({ anchor: '100%', format: 'd.m.Y', value: '01.01.3000' });

        var cont;
        if (endValue)
        cont = new Ext.Container({
            paramCode: this.userParam.Code,
            anchor: '100%',
            boxMaxWidth: 400 + this.paramsContainerLabelWidth,
            layout: 'hbox',
            defaults: { xtype: 'container', layout: 'anchor' },
            items: [
                { items: [{ xtype: 'displayfield', fieldLabel: this.userParam.Name }], layout: 'form', labelWidth: 0 , width: this.paramsContainerLabelWidth },
                { xtype: 'spacer', width: 5 },
                { items: [begin], hideLabel: true, flex: 1 },
                { xtype: 'spacer', width: 5 },
                { items: [end], hideLabel: true, flex:   1 }
            ],
            getValue: function() {
                var val1 = begin.getValue(),
                    val2 = end.getValue();
                
                return (val1 ? val1.format("Y-m-d\\TH:i:s") : '') + ',' + (val2 ? val2.format("Y-m-d\\TH:i:s") : '');
            },
            validate: function() {
                return !!begin.getValue() && !!end.getValue();
            }
        });
        else
            cont = new Ext.Container({
                paramCode: this.userParam.Code,
                anchor: '100%',
                boxMaxWidth: 400 + this.paramsContainerLabelWidth,
                layout: 'hbox',
                defaults: { xtype: 'container', layout: 'anchor' },
                items: [
                    { items: [{ xtype: 'displayfield', fieldLabel: this.userParam.Name }], layout: 'form', labelWidth: 0, width: this.paramsContainerLabelWidth },
                    { xtype: 'spacer', width: 5 },
                    { items: [begin], hideLabel: true, flex: 1 },
                ],
                getValue: function () {
                    var val1 = begin.getValue(),
                        val2 = end.getValue();

                    return (val1 ? val1.format("Y-m-d\\TH:i:s") : '') + ',' + (val2 ? val2.format("Y-m-d\\TH:i:s") : '');
                },
                validate: function () {
                    return !!begin.getValue() && !!end.getValue();
                }
            });
        return cont;
    }
});

Bars.KP50.report.MultiSelectField = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        var config = Ext.apply({}, this.userParam.ComboBoxConfig);

        Ext.apply(config, {
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            allowBlank: !this.userParam.Require,
            editable: false,
            triggerAction: 'all'
        }, this.getDefaultConfig());

        if (this.userParam.StoreData) {
            config.store.autoLoad = false;
        }

        var combobox = new Ext.ux.form.LovCombo(config);
        if (this.userParam.StoreData) {
            combobox.store.loadData(this.userParam.StoreData, false);
        }

        return combobox;
    }
});

Bars.KP50.report.ServiceField = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        var config = Ext.apply({}, this.userParam.ComboBoxConfig);
        var status = Ext.apply({}, this.userParam.StatusConfig);

        Ext.apply(config, {
            editable: false,
            triggerAction: 'all',
            width: 250
        }, this.getDefaultConfig());

        Ext.apply(status, {
            editable: false,
            triggerAction: 'all',
            width: 145
        }, this.getDefaultConfig());

        if (this.userParam.StoreData) {
            status.store.autoLoad = false;
            status.value = this.userParam.Value;
        }

        var statusCombo = new Ext.form.ComboBox(status);
        if (this.userParam.Value && statusCombo.mode === "remote") {
            statusCombo.getStore().on('load', function () {
                statusCombo.setValue(this.userParam.Value);
            }, this, { single: true });
        }

        if (this.userParam.StoreData) {
            statusCombo.getStore().loadData(this.userParam.StoreData, false);
        }

        var servicesCombo = new Ext.ux.form.LovCombo(config);
        var cont = new Ext.Container({
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            allowBlank: !this.userParam.Require,
            editable: false,
            triggerAction: 'all',
            anchor: '100%',
            boxMaxWidth: 400 + this.paramsContainerLabelWidth,
            layout: 'hbox',
            defaults: { xtype: 'container', layout: 'anchor' },
            items: [
                servicesCombo,
                { xtype: 'spacer', width: 5 },
                statusCombo
            ],
        });

        cont.getValue = function() {
            return {
                Services: servicesCombo.getValue(),
                Status: statusCombo.getValue()
            };
        };
        return cont;
    }
});

Bars.KP50.report.AddressField = Ext.extend(Bars.KP50.report.UserParam, {
    isForm: true,
    
    createField: function () {

        var comboConfig = Ext.apply({
            valueField: 'Id',
            displayField: 'Name',
            editable: false,
            mode: 'remote',
            triggerAction: 'all'
        }, this.getDefaultConfig());
        
        var comboRaions = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Район',
            operationId: 'LIST_RAIONS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'AddressHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboStreets = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Улица',
            disabled: true,
            operationId: 'LIST_STREETS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'AddressHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboHouses = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Дом',
            queryMode: 'local',
            disabled: true,
            operationId: 'LIST_HOUSES',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'AddressHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        
        comboStreets.parentCombo = comboRaions; 
        comboHouses.parentCombo = comboStreets;
        comboRaions.childCombo1 = comboStreets;
        comboRaions.childCombo2 = comboHouses;
        comboStreets.childCombo1 = comboHouses;

        comboRaions.store.on('beforeload', this.onBeforeLoad, comboRaions);
        comboStreets.store.on('beforeload', this.onBeforeLoad, comboStreets);
        comboHouses.store.on('beforeload', this.onBeforeLoad, comboHouses);

        comboRaions.on('change', this.onChange, this);
        comboStreets.on('change', this.onChange, this);

        var config = Ext.apply({}, {
            paramCode: this.userParam.Code,
            layout: 'form',
            anchor: '100%',
            labelWidth: this.paramsContainerLabelWidth,
            items: [
                comboRaions,
                comboStreets,
                comboHouses
            ]
        });
        
        var form = new Ext.Container(config);
        form.getValue = function () {
            return {
                Raions: comboRaions.getValue(),
                Streets: comboStreets.getValue(),
                Houses: comboHouses.getValue()
            };
        };
        return form;
    },
    
    onBeforeLoad: function (store, options) {
        options.params.operationId = this.operationId;
        if (this.parentCombo) {
            options.params.selectedValues = this.parentCombo.getValue() ? Ext.encode(this.parentCombo.getValue()) : null;
        }
    },
    
    onChange: function (combo, newValue) {
        if (combo.childCombo2) {
            combo.childCombo2.setValue(null);
            delete combo.childCombo2.lastQuery;
        }
        if (combo.childCombo1) {
            combo.childCombo1.setValue(null);
            delete combo.childCombo1.lastQuery;
            combo.childCombo1.clearValue();
            if (!combo.childCombo2 && newValue && newValue.length > 1) {
                combo.childCombo1.setEditable(false);
            }
            else {
                combo.childCombo1.setEditable(false);
            }
            combo.childCombo1.setDisabled(!newValue);
        }
        if (combo.childCombo2) {
            combo.childCombo2.clearValue();
            combo.childCombo2.setDisabled(true);
        }
    }
});

Bars.KP50.report.SupplierAndBankField = Ext.extend(Bars.KP50.report.UserParam, {
    isForm: true,

    createField: function () {

        var comboConfig = Ext.apply({
            valueField: 'Id',
            displayField: 'Name',
            editable: false,
            mode: 'remote',
            triggerAction: 'all'
        }, this.getDefaultConfig());

        var comboBanks = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Банки данных',
            operationId: 'LIST_BANKS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'SupplierAndBankHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboSuppliers = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Поставщики',
            disabled: true,
            operationId: 'LIST_SUPPLIERS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'SupplierAndBankHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));

        comboSuppliers.parentCombo = comboBanks;
        comboBanks.childCombo1 = comboSuppliers;

        comboBanks.store.on('beforeload', this.onBeforeLoad, comboBanks);
        comboSuppliers.store.on('beforeload', this.onBeforeLoad, comboSuppliers);

        comboBanks.on('change', this.onChange, this);
        comboSuppliers.on('change', this.onChange, this);

        var config = Ext.apply({}, {
            paramCode: this.userParam.Code,
            layout: 'form',
            anchor: '100%',
            labelWidth: this.paramsContainerLabelWidth,
            items: [
                comboBanks,
                comboSuppliers
            ]
        });

        var form = new Ext.Container(config);
        form.getValue = function () {
            return {
                Raions: comboBanks.getValue(),
                Streets: comboSuppliers.getValue()
            };
        };
        return form;
    },

    onBeforeLoad: function (store, options) {
        options.params.operationId = this.operationId;
        if (this.parentCombo) {
            options.params.selectedValues = this.parentCombo.getValue() ? Ext.encode(this.parentCombo.getValue()) : null;
        }
    },

    onChange: function (combo, newValue) {
        debugger;
        if (combo.childCombo2) {
            combo.childCombo2.setValue(null);
            delete combo.childCombo2.lastQuery;
        }
        if (combo.childCombo1) {
            combo.childCombo1.setValue(null);
            delete combo.childCombo1.lastQuery;
            combo.childCombo1.clearValue();
            if (!combo.childCombo2 && newValue && newValue.length > 1) {
                combo.childCombo1.setEditable(true);
            }
            else {
                combo.childCombo1.setEditable(false);
            }
            combo.childCombo1.setDisabled(!newValue);
        }
        if (combo.childCombo2) {
            combo.childCombo2.clearValue();
            combo.childCombo2.setDisabled(true);
        }
    }
});


Bars.KP50.report.BankSupplierField = Ext.extend(Bars.KP50.report.UserParam, {
    isForm: true,

    createField: function () {

        var comboConfig = Ext.apply({
            valueField: 'Id',
            displayField: 'Name',
            editable: false,
            mode: 'remote',
            triggerAction: 'all'
        }, this.getDefaultConfig());

        var comboBanks = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Территория',
            operationId: 'LIST_BANKS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'BankSupplierHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboAgents = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Агент',
            disabled: true,
            operationId: 'LIST_AGENTS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'BankSupplierHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboPrincipals = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Принципал',
            disabled: true,
            operationId: 'LIST_PRINCIPALS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'BankSupplierHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboSuppliers = new Ext.ux.form.LovCombo(Ext.apply({
            fieldLabel: 'Поставщик',
            disabled: true,
            operationId: 'LIST_SUPPLIERS',
            store: new Ext.data.JsonStore({ autoLoad: false, url: 'BankSupplierHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));

        
        comboAgents.parentCombo = comboBanks;
        comboPrincipals.parentCombo = comboBanks;
        comboSuppliers.parentCombo = comboBanks;
        
        comboBanks.childCombo1 = comboAgents;
        comboBanks.childCombo2 = comboPrincipals;
        comboBanks.childCombo3 = comboSuppliers;
        

        comboBanks.store.on('beforeload', this.onBeforeLoad, comboBanks);
        comboAgents.store.on('beforeload', this.onBeforeLoad, comboAgents);
        comboPrincipals.store.on('beforeload', this.onBeforeLoad, comboPrincipals);
        comboSuppliers.store.on('beforeload', this.onBeforeLoad, comboSuppliers);

        comboBanks.on('change', this.onChange, this);
       // comboAgents.on('change', this.onChange, this);
       // comboPrincipals.on('change', this.onChange, this);
       // comboSuppliers.on('change', this.onChange, this);

        var config = Ext.apply({}, {
            paramCode: this.userParam.Code,
            layout: 'form',
            anchor: '100%',
            labelWidth: this.paramsContainerLabelWidth,
            items: [
                comboBanks,
                comboAgents,
                comboPrincipals,
                comboSuppliers
            ]
        });

        var form = new Ext.Container(config);
        form.getValue = function () {
            return {
                Banks: comboBanks.getValue(),
                Agents: comboAgents.getValue(),
                Principals: comboPrincipals.getValue(),
                Suppliers: comboSuppliers.getValue()
            };
        };
        return form;
    },

    onBeforeLoad: function (store, options) {
        options.params.operationId = this.operationId;
        if (this.parentCombo) {
            options.params.selectedValues = this.parentCombo.getValue() ? Ext.encode(this.parentCombo.getValue()) : null;
        }
    },

    onChange: function(combo, newValue) {
        debugger;
        if (combo.childCombo1) {
            combo.childCombo1.setValue(null);
            delete combo.childCombo1.lastQuery;
            combo.childCombo1.clearValue();
        }
        if (combo.childCombo2) {
            combo.childCombo2.setValue(null);
            delete combo.childCombo2.lastQuery;
            combo.childCombo2.clearValue();
        }

        if (combo.childCombo3) {
            combo.childCombo3.setValue(null);
            delete combo.childCombo3.lastQuery;
            combo.childCombo3.clearValue();
        }

        if (newValue && newValue.length > 1) {
            combo.childCombo1.setEditable(true);
            combo.childCombo2.setEditable(true);
            combo.childCombo3.setEditable(true);
        } else {
            combo.childCombo1.setEditable(false);
            combo.childCombo2.setEditable(false);
            combo.childCombo3.setEditable(false);
        }
        combo.childCombo1.setDisabled(!newValue);
        combo.childCombo2.setDisabled(!newValue);
        combo.childCombo3.setDisabled(!newValue);

    }
});

Bars.KP50.report.TypeAndPrmField = Ext.extend(Bars.KP50.report.UserParam, {
    isForm: true,

    createField: function () {

        var comboConfig = window.Ext.apply({
            valueField: 'Id',
            displayField: 'Name',
            editable: false,
            mode: 'remote',
            triggerAction: 'all'
        }, this.getDefaultConfig());

        var comboTypes = new window.Ext.ux.form.LovCombo(window.Ext.apply({
            fieldLabel: 'Тип параметра',
            operationId: 'LIST_TYPES',
            store: new window.Ext.data.JsonStore({ autoLoad: false, url: 'TypeAndPrmHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));
        var comboPrms = new window.Ext.ux.form.LovCombo(window.Ext.apply({
            fieldLabel: 'Наименование параметра',
            disabled: true,
            operationId: 'LIST_PRMS',
            store: new window.Ext.data.JsonStore({ autoLoad: false, url: 'TypeAndPrmHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], root: 'data' })
        }, comboConfig));

        comboPrms.parentCombo = comboTypes;
        comboTypes.childCombo1 = comboPrms;

        comboTypes.store.on('beforeload', this.onBeforeLoad, comboTypes);
        comboPrms.store.on('beforeload', this.onBeforeLoad, comboPrms);
        
        comboTypes.on('change', this.onChange, this);

        var config = window.Ext.apply({}, {
            paramCode: this.userParam.Code,
            layout: 'form',
            anchor: '100%',
            labelWidth: this.paramsContainerLabelWidth,
            items: [
                comboTypes,
                comboPrms
            ]
        });

        var form = new window.Ext.Container(config);
        form.getValue = function () {
            return {
                TypePrm: comboTypes.getValue(),
                NzpPrm: comboPrms.getValue()
            };
        };
        return form;
    },

    onBeforeLoad: function (store, options) {
        options.params.operationId = this.operationId;
        if (this.parentCombo) {
            options.params.selectedValues = this.parentCombo.getValue() ? window.Ext.encode(this.parentCombo.getValue()) : null;
        }
    },

    onChange: function (combo, newValue) {
        debugger;
        if (combo.childCombo1) {
            combo.childCombo1.setValue(null);
            delete combo.childCombo1.lastQuery;
            combo.childCombo1.clearValue();
            combo.childCombo1.setEditable(false);
            combo.childCombo1.setDisabled(!newValue);
        }
    }
});

Ext.apply(Ext.form.VTypes, {
    numbers: function (val, field) {
        var numbersRegex = /^\d{2,6}$/;
        return numbersRegex.test(val);
    },
    numbersText: 'Слишком большое или маленькое число',
    numbersMask: /[\d]/
});

Bars.KP50.report.ReportRowCount = Ext.extend(Bars.KP50.report.UserParam, {
    createField: function () {
        return new Ext.form.TextField(Ext.apply(this.getDefaultConfig(), {
            paramCode: this.userParam.Code,
            fieldLabel: this.userParam.Name,
            value: this.userParam.Value,
            allowBlank: !this.userParam.Require,
            vtype: 'numbers'
        }));
    }
});