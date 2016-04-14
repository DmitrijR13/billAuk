/// <reference path="../extjs/ext-all-debug-w-comments.js" />

Ext.ns('Bars.KP50.report');
Ext.ns('Bars.KP50.url');

Bars.KP50.url.content = function (url) {
    return location.protocol + '//' + location.host + rootUrl + url;
};

Bars.KP50.url.action = function (url) {
    return location.protocol + '//' + location.host + rootUrl + url;
};

Bars.KP50.report.ViewPanel = Ext.extend(Ext.Panel, {
    border: false,
    layout: 'border',

    initComponent: function () {
        Bars.KP50.report.ViewPanel.superclass.initComponent.call(this);

        var gmyFilesStore = new Ext.data.Store({
            autoDestroy: true,
            autoLoad: { params: { start: 0, limit: 20 } },
            remoteSort: true,
            reader: new Ext.data.JsonReader({
                fields: ['rep_name', 'dat_out', 'status_name', 'exec_comment', 'nzp_exc'],
                root: 'data',
                totalProperty: 'totalCount'
            }),
            proxy: new Ext.data.HttpProxy({ method: 'GET', url: Bars.KP50.url.action('ReportHandler.axd?operationId=LIST_FILES'), json: true })
        });

        var scrollerMenu = new Ext.ux.TabScrollerMenu({
            maxText: 45,
            pageSize: 10
        });

        var me = this;
        this.add([
            {
                xtype: 'treepanel',
                region: 'west',
                width: 400,
                ref: 'tree',
                title: 'Отчеты',
                border: false,
                autoScroll: true,
                animate: true,
                enableDD: false,
                rootVisible: false,
                //root: new Ext.tree.TreeNode(),
                //root: new Ext.tree.AsyncTreeNode({
                //    text: '',
                //    draggable: false, // disable root node dragging
                //    id: 'root'
                //}),
                //loader: new Ext.tree.TreeLoader({ dataUrl: Bars.KP50.url.action('ReportHandler.axd?operationId=GET_REPORTS') }),
                dataUrl: Bars.KP50.url.action('ReportHandler.axd?operationId=GET_REPORTS'),
                root: {},
                //loader: new Ext.tree.TreeLoader({
                //    dataUrl: Bars.KP50.url.action('ReportHandler.axd?operationId=GET_REPORTS'),
                //    //preloadChildren: true,
                //    baseParams: this._getQueryParams()
                //}),
                tbar: [
                    new Ext.Button({
                        iconCls: 'icon-expand-all',
                        scope: this,
                        handler: function () {
                            this.tree.expandAll();
                        }
                    }),
                    new Ext.Button({
                        iconCls: 'icon-collapse-all',
                        scope: this,
                        handler: function () {
                            this.tree.collapseAll();
                        }
                    }),
                    new Ext.Button(
                        {
                            iconCls: 'icon-arrow-refresh',
                            text: 'Обновить',
                            listeners: {
                                click: {
                                    fn: function () {
                                        this.reloadTree();
                                    },
                                    scope: this
                                }
                            }
                        })
                ],
                listeners: {
                    click: function (node) {
                        Ext.Ajax.request({
                            url: Bars.KP50.url.action('ReportHandler.axd?operationId=GET_REPORT_PARAMS'),
                            success: function (result) {
                                me.createTab(Ext.decode(result.responseText));
                            },
                            failure: function (error) {
                                //debugger;
                            },
                            params: me._getQueryParams({ reportId: node.attributes.code })
                        });
                    }
                }
            },
            {
                xtype: 'tabpanel',
                region: 'center',
                ref: 'tabPanel',
                enableTabScroll: true,
                border: false,
                activeTab: 0,
                plugins: [scrollerMenu],
                items: [{
                    autoScroll: true,
                    title: 'Примечание',
                    html: '<div class="reportDesc-content">\n' +
                        '<h1 class="descTitle">Печатные формы</h1>\n' +
                        '<p class="textblock">\n' +
                        'Для того чтобы сформировать отчет, выберите нужную вам печатную форму в списке слева и щелкните по ней левой кнопкой мыши. Выбранная вами печатная форма откроется на отдельной вкладке, где вы сможете задать нужные параметры отчета.' +
                        '</p>\n' +
                        '</div>'
                },
                {
                    xtype: 'grid',
                    ref: '../grid',
                    title: 'Мои файлы',
                    sm: new Ext.grid.RowSelectionModel({ singleSelect: true }),
                    viewConfig: {},
                    autoExpandColumn: 'columnExecComment',
                    autoExpandMax: 2000,
                    store: gmyFilesStore,
                    colModel: new Ext.grid.ColumnModel({
                        defaults: {
                            sortable: false,
                            menuDisabled: true
                        },
                        columns: [
                            {
                                header: '<div align="center">Наименование файла</div>',
                                dataIndex: 'rep_name',
                                width: 250,
                                renderer: function (v, meta, record) {
                                    if (record.get('nzp_exc')) {
                                        return '<a href="' + Bars.KP50.url.action('general/download.ashx?id=' + record.get('nzp_exc')) + '" target="_blank">' + v + '</a>';
                                    }
                                    return v;
                                }
                            },
                            { header: '<div align="center">Дата формирования</div>', dataIndex: 'dat_out', format: 'd.m.Y hh:mi:ss', width: 140, align: 'center' },
                            { header: '<div align="center">Статус</div>', dataIndex: 'status_name', width: 150, align: 'center' },
                            { header: '<div align="center">Комментарий</div>', dataIndex: 'exec_comment', id: 'columnExecComment' }
                        ]
                    }),
                    bbar: {
                        xtype: 'paging',
                        displayInfo: true,
                        store: gmyFilesStore,
                        pageSize: 20,
                        items: [
                            { xtype: 'tbseparator' },
                            {
                                xtype: 'combo',
                                editable: false,
                                ref: 'fieldPageSize',
                                hideTrigger: true,
                                fieldLabel: 'Записей',
                                width: Ext.isIE ? 100: 80,
                                labelWidth: 43,
                                mode: 'local',
                                triggerAction: 'all',
                                store: new Ext.data.ArrayStore({
                                    id: 0,
                                    fields: ['count'],
                                    data: [[20], [50], [100], [10]],
                                    autoLoad: false
                                }),
                                valueField: 'count',
                                displayField: 'count',
                                listeners: {
                                    select: function (cb, record) {
                                        var pageSize = record.data.count;
                                        this.refOwner.pageSize = pageSize;
                                        this.refOwner.doRefresh();
                                    },
                                    afterrender: function () {
                                        var _this = this;
                                        setTimeout(function () { _this.setValue(20); }, 10);
                                    }
                                }
                            }
                        ]
                    }
                }]
            }
        ]);
    },
    listeners: {
        render: function (cmp) {
            cmp.reloadTree();
        }
    },
    reloadTree: function () {
        var rootNode = this.tree.getRootNode();

        //rootNode.removeAll();
        //this.tree.loader.requestData(rootNode);
        //rootNode.expand();
    },
    createTab: function(report) {
        var idx = this.tabPanel.items.findIndexBy(function(tab) {
            return tab.reportId == report.Id;
        });
        if (idx != -1) {
            this.tabPanel.setActiveTab(idx);
            return;
        }

        var descTag = {
            tag: 'div',
            children: [
                { tag: 'h1', cls: 'nameTitle', html: Ext.util.Format.htmlEncode(report.Name) }
            ]
        };

        if (report.Description) {
            descTag.children.push({ tag: 'p', cls: 'descTitle', html: Ext.util.Format.htmlEncode(report.Description) });
        }

        descTag.children.push({ tag: 'hr' });
        descTag.children.push({ tag: 'b', cls: 'textblock', html: 'Параметры отчета:' });

        var tab = this.tabPanel.add({
            xtype: 'panel',
            title: report.Name,
            closable: true,
            reportId: report.Id,
            layout: 'form',
            cls: 'print-report-page',
            autoScroll: true,
            tbar: {
                xtype: 'toolbar',
                items: [
                    {
                        xtype: 'button',
                        text: 'Получить отчет',
                        iconCls: 'icon-printer',
                        listeners: {
                            click: this.onPrintButtonClick,
                            scope: this
                        }
                    }
                ]
            }
        });

        if (report.Filters != null) {
            tab.add({
                id: 'filters',
                padding: 10,
                title: 'Фильтры отчета',
                xtype: 'panel',
                autoHeight: true,
                style: 'padding: 20px 30px 0px 30px',
                ref: 'filterContainer',
                layout: 'form',
                width: '450px',
                labelWidth: 150,
                autoScroll: true
            });
            Ext.each(report.Filters, function (filter) {
                tab.filterContainer.add(this.getUserParamField(filter));
            }, this);
        }

        tab.add(
        {
            padding: 10,
            title: 'Параметры отчета',
            xtype: 'panel',
            autoHeight: true,
            style: 'padding: 20px 30px 0px 30px',
            ref: 'paramsContainer',
            layout: 'form',
            width: '450px',
            labelWidth: 150,
            autoScroll: true
        });
        Ext.each(report.Params, function (parameter) {
            tab.paramsContainer.add(this.getUserParamField(parameter));
        }, this);

        this.tabPanel.setActiveTab(tab);
    },
    //getUserFilterField: function (filter) {
    //    var filterClass = eval(filter.JavascriptClassName);
    //    var filterObj = new filterClass({ userFilter: filter, paramsContainerLabelWidth: 150 });
    //    return filterObj.createField();
    //},
    getUserParamField: function (parameter) {
        var paramClass = eval(parameter.JavascriptClassName);
        var paramObj = new paramClass({ userParam: parameter, paramsContainerLabelWidth: 150 });
        return paramObj.createField();
    },
    onPrintButtonClick: function () {
        var errors = this.getValidationErrors();
        if (errors.length > 0) {
            this.showValidationErrorMessages(errors);
        } else {
            var me = this;
            var userFilters;
            if (this.tabPanel.getActiveTab().filterContainer != null) {
                var filters = this.collectUserFilterValues();
                userFilters = Ext.encode(filters);
            } else {
                userFilters = null;
            }
            var params = this.collectUserParamValues();
            var userParams = Ext.encode(params);
            var tab = this.tabPanel.getActiveTab();
            var myMask = new Ext.LoadMask(tab.getEl(), { msg: "Формирование отчета..." });
            myMask.show();
            
            Ext.Ajax.request({
                url: Bars.KP50.url.action('ReportHandler.axd?operationId=PRINT_REPORT'),
                success: function (result) {
                    myMask.hide();
                    var resp = Ext.decode(result.responseText);
                    if (resp.success) {
                        var data = resp.data;
                        if (resp.data.isPreview) {
                            me.viewReport(data.nzpExcelUtility);
                        } else {
                            Ext.MessageBox.alert('Сообщение', 'Отчет формируется. Прогресс выполнения можно посмотреть на странице "Мои файлы"');
                            me.grid.getStore().reload();
                        }
                    } else {
                        Ext.MessageBox.show(
                        {
                            title: 'Ошибка получения отчета',
                            msg: resp.message,
                            buttons: Ext.MessageBox.OK
                        });
                    }
                },
                failure: function (error) {
                    myMask.hide();
                    Ext.MessageBox.show(
                        {
                            title: 'Ошибка получения отчета',
                            msg: error.statusText,
                            buttons: Ext.MessageBox.OK,
                            width: 250
                        });
                },
                params: this._getQueryParams({
                    reportId: tab.reportId,
                    userParams: userParams,
                    userFilters: userFilters
                })
            });
        }
    },
    collectUserFilterValues: function () {
        var result = [];
        this.tabPanel.getActiveTab().filterContainer.items.each(function (field) {
            if (field.paramCode) {
                var value = field.getValue();
                value = !Ext.isEmpty(value) ? value : field.defaultValue;
                result.push({ Code: field.paramCode, Value: value ? (Ext.isString(value) ? value : Ext.encode(value)) : null });
            }
        });

        return result;
    },
    collectUserParamValues: function () {
        var result = [];
        this.tabPanel.getActiveTab().paramsContainer.items.each(function (field) {
            if (field.paramCode) {
                var value = field.getValue();
                value = !Ext.isEmpty(value) ? value : field.defaultValue;
                result.push({ Code: field.paramCode, Value: value ? (Ext.isString(value) ? value : Ext.encode(value)) : null });
            }
        });

        return result;
    },
    getValidationErrors: function () {
        var errors = [];
        if (this.tabPanel.getActiveTab().filterContainer != null)
        this.tabPanel.getActiveTab().filterContainer.items.each(function (field) {
            if (field.paramCode)
                this.getFieldValidationErrors(field, errors);
        }, this);
        this.tabPanel.getActiveTab().paramsContainer.items.each(function (field) {
            if (field.paramCode)
                this.getFieldValidationErrors(field, errors);
        }, this);
        return errors;
    },
    getFieldValidationErrors: function (field, result) {
        if (typeof field.validate == 'function' && !field.validate()) {
            var errors = field.getErrors();
            var errorsH = {};
            Ext.each(errors, function(txt) { errorsH[txt] = true; });
            errors = [];
            Ext.iterate(errorsH, function (key) { errors.push(key); });
            Ext.each(errors, function (e) { result.push({ fieldLabel: field.fieldLabel, msg: e }); });
        }
    },
    showValidationErrorMessages: function (errors) {
        var msg = 'Параметры отчета указаны неполностью или неверно.<br/><br/>';
        msg += '<ol>';
        for (var i = 0, n = errors.length; i < n; ++i) {
            var err = errors[i];
            msg += String.format('<li style="list-style: initial; list-style-position: inside; list-style-type: decimal;"><strong style="font-weight: bold;">{0}</strong>: {1}</li>', err.fieldLabel, err.msg);
        }
        msg += '</ol>';
        Ext.Msg.show({
            buttons: Ext.MessageBox.OK,
            icon: Ext.MessageBox.ERROR,
            minWidth: 300,
            msg: msg,
            title: 'Сообщение'
        });
    },
    viewReport: function (nzpExcelUtility) {
        var tab = this.tabPanel.add({
            xtype: 'panel',
            title: 'Просмотр отчета',
            closable: true,
            autoLoad: {
                mode: 'iframe',
                text: "Загрузка...",
                nocache: true,
                scripts: true,
                showMask: true,
                url: 'fast_report.aspx?nzpExcelUtility=' + nzpExcelUtility
            }
        });
        this.tabPanel.setActiveTab(tab);
    },
    _getQueryParams: function (baseParams) {
        var hrefs = location.href.split('?');

        var queryParams = {};
        if (hrefs.length === 2) {
            var values = hrefs[1].split('&');
            Ext.each(values, function (value) {
                var vals = value.split('=');
                if (!!vals[0]) {
                    queryParams[vals[0]] = vals[1];
                }
            });
        }
        
        return Ext.apply(baseParams || {}, queryParams);
    }
});