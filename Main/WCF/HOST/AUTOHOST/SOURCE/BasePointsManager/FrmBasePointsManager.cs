using System.Collections.Generic;
using Microsoft.Win32;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using STCLINE.KP50.HostMan.SOURCE.BasePointsManager;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Server;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmBasePointsManager : Form
    {

        /// <summary>
        /// Вспомогательная структура 
        /// для передачи параметров нового банка
        /// </summary>
        private struct NewBankPrms
        {
            public string newPref;
            public string newName;
            public int newYear;

            public string kernelFullName;
            public string dataFullName;
            public string chargeFullName;
            public string finFullName;
        }

        private IDbConnection connection = null;
        private DataTable spoints = null;
        private DataTable baselist = null;
        private StringBuilder infoLog = new StringBuilder();
        private NewBankPrms newBankPrms = new NewBankPrms();
        private int main_bank_index = 0;

        public FrmBasePointsManager()
        {
            InitializeComponent();
        }

        private void LoadBanks()
        {
            try
            {
                comboBoxBase.Items.Clear();

                string baseName = String.Empty;
                // считывание банков
                String sqlString = " SELECT * " +
                                   " FROM " + Points.Pref + DBManager.sKernelAliasRest + " s_point " +
                                   " WHERE nzp_graj >= 0 ORDER BY nzp_graj, point ";
                this.spoints = DBManager.ExecSQLToTable(this.connection, sqlString);
                for (int i = 0; i < this.spoints.Rows.Count; i++)
                {
                    if (int.Parse(this.spoints.Rows[i]["nzp_graj"].ToString()) == 0)
                    {
                        // это главный банк
                        this.main_bank_index = comboBoxBase.Items.Count;
                    }

                    //склеиваем название с префиксом(для наглядности)
                    baseName = String.Format("{0} ({1})", this.spoints.Rows[i]["point"].ToString().Trim(),
                        this.spoints.Rows[i]["bd_kernel"].ToString().Trim());
                    comboBoxBase.Items.Add(baseName);
                }
                comboBoxBase.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при считывании параметров банка данных! Проверьте подключение к БД и см. error.log" +
                    ex.Message, "Ошибка!", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FrmBankPointsManager_Load(object sender, EventArgs e)
        {
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                //запуск хоста
                SrvRun.StartHostProgram(false);
            }
            // подключиться к базе и считать банки
            this.connection = DBManager.GetConnection(Constants.cons_Kernel);
            this.connection.Open();
            LoadBanks();
        }

        private void FrmBankPointsManager_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.connection.Close();
        }

        private void comboBoxBank_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // прочитать разделы выбранного банка
                String bank_id = this.spoints.Rows[comboBoxBase.SelectedIndex]["nzp_wp"].ToString();
                String bank_name = this.spoints.Rows[comboBoxBase.SelectedIndex]["bd_kernel"].ToString().Trim();
                string sqlString;
                // если это главный банк, то условия будут иные
                if (comboBoxBase.SelectedIndex == this.main_bank_index)
                    sqlString = String.Format(
                        "SELECT * FROM {0}s_baselist " +
                        " WHERE idtype IN ({1}) AND nzp_wp IS NULL  ORDER BY frec_id",
                        bank_name + "_kernel" + DBManager.tableDelimiter,
                        BaselistTypes.Charge.GetHashCode() + "," + BaselistTypes.Fin.GetHashCode().ToString());
                else
                    sqlString = String.Format(
                        "SELECT * FROM {0}s_baselist WHERE idtype IN ({1})",
                        bank_name + "_kernel" + DBManager.tableDelimiter,
                        BaselistTypes.Charge.GetHashCode().ToString());
                this.baselist = DBManager.ExecSQLToTable(this.connection, sqlString);
                this.gridBaseList.Rows.Clear();
                
                for (int i = 0; i < this.baselist.Rows.Count; i++)
                {
                    int row = this.gridBaseList.Rows.Add();
                    this.gridBaseList.Rows[row].Cells["ColumnPeriod"].Value = this.baselist.Rows[i]["yearr"].ToString();
                    this.gridBaseList.Rows[row].Cells["ColumnType"].Value = this.baselist.Rows[i]["idtype"];
                    this.gridBaseList.Rows[row].Cells["ColumnName"].Value =
                        this.baselist.Rows[i]["dbname"].ToString().Trim();
                    this.gridBaseList.Rows[row].Cells["ColumnComment"].Value =
                        this.baselist.Rows[i]["comment"].ToString().Trim();
                    this.gridBaseList.Rows[row].Cells["ColumnID"].Value =
                        this.baselist.Rows[i]["nzp_bl"].ToString().Trim();
                }
                this.gridBaseList.Enabled = this.baselist.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при чтении раздела выбранного банка! Ошибка:" + Environment.NewLine + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gridBaseList_SelectionChanged(object sender, EventArgs e)
        {
            this.btnNewBase.Enabled = this.gridBaseList.SelectedRows.Count > 0;
            this.btnEdit.Enabled = this.gridBaseList.SelectedRows.Count > 0;
            this.btnDelete.Enabled = this.gridBaseList.SelectedRows.Count > 0;
            // главный банк нельзя копировать
            btnNewBank.Enabled = !(comboBoxBase.SelectedIndex == this.main_bank_index) &&
                                 (this.gridBaseList.SelectedRows.Count > 0);
        }


        /// <summary>
        /// Изменить название банка
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEdit_Click(object sender, EventArgs e)
        {
            String newName;
            //переименовать банк
            var frm = new FrmRenameBank();
            DialogResult res = frm.ShowDialog(out newName);
            if (res == DialogResult.OK)
            {
                // теперь можно изменить период
                String bank_id = this.spoints.Rows[(int) comboBoxBase.SelectedIndex]["nzp_wp"].ToString();
                string sql = String.Format("UPDATE {0}s_point SET point = '{1}' WHERE nzp_wp = {2}",
                     Points.Pref + "_kernel" + DBManager.tableDelimiter,
                    newName.Trim(),
                    bank_id);
                Returns ret = DBManager.ExecSQL(this.connection, sql, true);
                if (!ret.result)
                {
                    MessageBox.Show(
                   "Ошибка при переименовании банка данных! Текст ошибки: " + Environment.NewLine +
                   ret.text, "Ошибка!");

                    MonitorLog.WriteLog("Ошибка при переименовании банка данных! Текст ошибки: " + Environment.NewLine + ret.text,
                   MonitorLog.typelog.Info, true);
                    return;
                }
                else
                {
                    //обновим список
                    LoadBanks();
                    comboBoxBank_SelectedIndexChanged(sender, e);
                    MessageBox.Show("Банк данных успешно переименован! Теперь необходимо перезапустить 'Host' и 'Web'", "Успешно!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Режим удаления схемы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            String bankPref = this.spoints.Rows[comboBoxBase.SelectedIndex]["bd_kernel"].ToString().Trim();

            //уточняющий вопрос
            if (MessageBox.Show(
                String.Format("Сейчас будет удалена схема '{0}'.\nПродолжить?",
                    this.gridBaseList.SelectedRows[0].Cells["ColumnName"].Value),
                "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            
#if DEBUG
            //ввод пароля
            FrmEnterPwd pwdFrm = new FrmEnterPwd();
            pwdFrm.ShowDialog();
            if (pwdFrm.DialogResult != DialogResult.OK)
            {
                return;
            }

#endif 
            
            
            MonitorLog.WriteLog(String.Format("Старт удаления схемы '{0}'",bankPref), MonitorLog.typelog.Info, true);


            // удалить запись из справочника
            String sql = String.Format("DELETE FROM {0}s_baselist WHERE nzp_bl = {1}",
                bankPref + "_kernel" + DBManager.tableDelimiter,
                this.gridBaseList.SelectedRows[0].Cells["ColumnID"].Value);
            Returns ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении схемы из справочника! Текст ошибки: " +Environment.NewLine+ ret.text,
                    MonitorLog.typelog.Info, true);
                MessageBox.Show(
                    "Ошибка при удалении схемы из справочника! Ошибки: " + Environment.NewLine +
                    ret.text, "Ошибка!");
                return;
            }
            else
            {
                // удалить схему из базы
                sql = String.Format("DROP SCHEMA {0} CASCADE;",
                    this.gridBaseList.SelectedRows[0].Cells["ColumnName"].Value);
                ret = DBManager.ExecSQL(this.connection, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при физическом удалении схемы!\n Текст ошибки: " + Environment.NewLine + ret.text,
                   MonitorLog.typelog.Info, true);
                    MessageBox.Show("Ошибка при физическом удалении схемы!\n Текст ошибки:" +Environment.NewLine+ ret.text, "Ошибка!",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBoxBank_SelectedIndexChanged(sender, e);
                    return;
                }
                MonitorLog.WriteLog(String.Format("Схема '{0}' успешно удалена!", bankPref), MonitorLog.typelog.Info, true);

                MessageBox.Show("Схема успешно удалена!", "Успешно!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // обновим список периодов
                comboBoxBank_SelectedIndexChanged(sender, e);
            }
        }


        /// <summary>
        /// Создание нового периода (_charge_XX или _fin_XX)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNewBase_Click(object sender, EventArgs e)
        {
            String newName, newComment;
            int newYear;
            String bankPref = this.spoints.Rows[comboBoxBase.SelectedIndex]["bd_kernel"].ToString().Trim();
            //создание нового банка для нового периода
            var frm = new FrmNewBase();
            DialogResult res = frm.ShowDialog(this.connection, this.gridBaseList.SelectedRows[0], true, bankPref,
                out newName, out newComment, out newYear);
            if (res == DialogResult.OK)
            {
                // теперь можно создать новый период

                // скопировать текущую схему
                var ret = CopySchema(this.gridBaseList.SelectedRows[0].Cells["ColumnName"].Value.ToString(), newName, newYear);
                if (!ret.result)
                {
                    MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //проверяем - создалась ли схема
                string sql =
                    " SELECT schema_name FROM information_schema.schemata " +
                    " WHERE  LOWER( TRIM(schema_name) ) = '" + newName.Trim().ToLower() + "'";
                if (DBManager.ExecSQLToTable(this.connection, sql).Rows.Count < 1)
                {
                    ret.text = "Схема " + bankPref + DBManager.sKernelAliasRest + " не создалась! Работа прекращена.";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ;
                }

                if (ret.result)
                {
                    // теперь можно добавить в список периодов
                     sql =
                        String.Format(" INSERT INTO {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                      " SELECT '{1}', idtype, {2}, attr, frec_id, '{3}' " +
                                      " FROM {5}s_baselist where nzp_bl = {4}",
                            Points.Pref + DBManager.sKernelAliasRest,
                            newName,
                            newYear,
                            newComment,
                            this.gridBaseList.SelectedRows[0].Cells["ColumnID"].Value,
                            bankPref + DBManager.sKernelAliasRest);
                    ret = DBManager.ExecSQL(this.connection, sql, true);
                    if (!ret.result)
                    {
                        MessageBox.Show(
                            "Ошибка при добавлении нового периода в верхний банк! Ошибки: " + Environment.NewLine +
                            ret.text, "Ошибка!");
                        //выходим, т.к. без этой вставки не пройдут миграции
                        return;
                    }

                    //если банк центральный, то не нужен данный insert
                    if (Points.Pref != bankPref)
                    {
                        sql =
                            String.Format(
                                " INSERT INTO {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                " SELECT '{1}', idtype, {2}, attr, frec_id, '{3}' " +
                                " FROM {0}s_baselist where nzp_bl = {4}",
                                bankPref + DBManager.sKernelAliasRest,
                                newName,
                                newYear,
                                newComment,
                                this.gridBaseList.SelectedRows[0].Cells["ColumnID"].Value);
                        ret.result = DBManager.ExecSQL(this.connection, sql, true).result;
                    }
                    if (!ret.result)
                    {
                        MessageBox.Show(
                            "Ошибка при добавлении нового периода в локальный банк! Ошибки: " + Environment.NewLine +
                            ret.text, "Ошибка!");
                    }
                    else
                    {
                        MessageBox.Show("Новый период успешно создан! Теперь необходимо перезапустить 'Host' и 'Web'",
                            "Успешно!",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // обновим список периодов
                        comboBoxBank_SelectedIndexChanged(sender, e);
                    }
                }
            }
        }


        private void btnNewBank_Click(object sender, EventArgs e)
        {

            //копировать нормативы из старого банка данных
            bool saveNormatifs = false;
            //префикс банка, с которого делаем копию 
            string oldBankPref = this.spoints.Rows[comboBoxBase.SelectedIndex]["bd_kernel"].ToString().Trim();

            // создадим новый банк данных
            FrmNewBank frm = new FrmNewBank();
            DialogResult res = frm.ShowDialog(this.connection, out newBankPrms.newPref, out newBankPrms.newName,
                out newBankPrms.newYear, out saveNormatifs);
            if (res == DialogResult.OK)
            {
                try
                {
                    Returns ret = new Returns() {result = false};
                    //год периода старого банка в формате YY
                    string oldBankYear =
                        (int.Parse(this.gridBaseList.SelectedRows[0].Cells["ColumnPeriod"].Value.ToString()) - 2000)
                            .ToString();

                    //проверка данных в БД на корректность
                    ret = CheckSystemTables(oldBankPref, oldBankYear);
                    if (!ret.result)
                    {
                        MessageBox.Show(
                            "В БД есть некорректные данные! Работа прекращена. Ошибки: " + Environment.NewLine +
                            ret.text, "Ошибка!");
                        return;
                    }


                    MonitorLog.WriteLog(" Старт создания нового локального банка." + Environment.NewLine +
                                        " Префикс банка, с которого делается копия: " + oldBankPref +
                                        Environment.NewLine +
                                        " Префикс нового банка: " + newBankPrms.newPref + Environment.NewLine +
                                        " Название нового банка: " + newBankPrms.newName + Environment.NewLine +
                                        " Начальный период нового банка: " + newBankPrms.newYear + Environment.NewLine +
                                        " Выбранный период старого банка: " + oldBankYear, MonitorLog.typelog.Info, true);



                    newBankPrms.kernelFullName = String.Format("{0}_kernel", newBankPrms.newPref);
                    newBankPrms.dataFullName = String.Format("{0}_data", newBankPrms.newPref);
                    newBankPrms.chargeFullName = String.Format("{0}_charge_{1}", newBankPrms.newPref,
                        (newBankPrms.newYear - 2000).ToString());
                    newBankPrms.finFullName = String.Format("{0}_fin_{1}", newBankPrms.newPref,
                        (newBankPrms.newYear - 2000).ToString());

                    // создать банк со схемами: 
                    // kernel
                    if (!CopySchema(String.Format("{0}_kernel", oldBankPref), newBankPrms.kernelFullName).result)
                        return;

                    // data
                    if (!CopySchema(String.Format("{0}_data", oldBankPref), newBankPrms.dataFullName).result)
                        return;

                    // charge
                    if (
                        !CopySchema(String.Format("{0}_charge_{1}", oldBankPref, oldBankYear),
                            newBankPrms.chargeFullName).result)
                        return;

                    MonitorLog.WriteLog("Успешно выполнено создание всех схем локального банка!",
                        MonitorLog.typelog.Info, true);

                    //если все хорошо, то пытаемся заполнить данные (копируем данные из старого банка) 
                    FillNewBank(newBankPrms, oldBankPref, saveNormatifs);

                    //независимо от результата пытаемся подключить банк 
                    if (!IncludeBank(newBankPrms))
                    {
                        //не получилось подключить
                        MessageBox.Show("Ошибка при подключении нового банка! См. логи. " +
                                        Environment.NewLine + "Придется подключать вручную :С ", "Ошибка!",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (infoLog.Length > 0)
                {
                    MessageBox.Show("Банк подключен с ошибками!" + Environment.NewLine + infoLog, "Выполнено!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Банк успешно подключен! Теперь необходимо перезапустить 'Host' и 'Web'", "Успешно!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Проверка используемых таблиц и данных в БД на корректность 
        /// (s_point, saldo_date и тд)
        /// </summary>
        /// <param name="pref">префикс банка, в котором проверяем</param>
        /// <param name="year">год периода в формате YY</param>
        /// <returns></returns>
        private Returns CheckSystemTables(string pref, string year)
        {

            MonitorLog.WriteLog(
                String.Format(
                    "Старт проверки используемых таблиц и данных в БД (префикс '{0}', год '{1}') на корректность  ",
                    pref.Trim(), year),
                MonitorLog.typelog.Info, true);
            Returns ret = new Returns() {result = true};

            try
            {
                //проверяем расчетный месяц (должна быть 1 активная запись)
                string sql =
                    " SELECT COUNT(*) AS count " +
                    " FROM " + pref + DBManager.sDataAliasRest + "saldo_date " +
                    " WHERE iscurrent = 0";

                if (Convert.ToInt32(DBManager.ExecSQLToTable(connection, sql).Rows[0]["count"]) != 1)
                {
                    ret.result = false;
                    ret.text += " Расчетный месяц в старом банке выставлен некорректно! Проверьте таблицу '" + pref + DBManager.sDataAliasRest + "saldo_date' ";
                }


                //проверяем charge_MM (есть ли таблицы в этой схеме)
                string chargeSchemaName = pref + "_charge_" + year;

                sql =
                    " SELECT  " +
                    " COUNT(*) AS count " +
                    " FROM information_schema.tables " +
                    " WHERE LOWER( TRIM(table_schema)) = '" + chargeSchemaName.Trim() + "' ";
                if (Convert.ToInt32(DBManager.ExecSQLToTable(connection, sql).Rows[0]["count"]) < 1)
                {
                    ret.result = false;
                    ret.text += " В старом банке нет таблиц для начислений (в схеме " + chargeSchemaName +
                               ")! Выберите другой период! ";
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            return ret;

        }

        private Returns CopySchema(string oldSchemaFullName, string newSchemaFullName, int? year = null)
        {
            Returns ret = new Returns() {result = true};
            MonitorLog.WriteLog("Старт создания схемы: '" + newSchemaFullName + "'", MonitorLog.typelog.Info, true);
            // копирование структуры схемы
            STCLINE.KP50.HostMan.Loading.Loading loadForm =
                new Loading.Loading("Выполняется создание схемы. Подождите...");
            try
            {
                #region Проверка на существование финансовой схемы
                if (oldSchemaFullName.Contains("charge"))
                {
                    //Проверка на существование схемы fin_xx
                    var schemaFin = string.Format("{0}_fin_{1}", Points.Pref, (year % 1000));
                    var exists =
                        string.Format("select exists (select * from pg_catalog.pg_namespace where nspname = '{0}')", schemaFin);
                    var result = DBManager.ExecScalar<bool>(connection, exists);
                    if (!result)
                    {
                        ret.result = false;
                        ret.text = "Перед созданием схемы charge_xx необходимо создать схему fin_xx";
                        return ret;
                    }
                }
                #endregion

                loadForm.Show();
                MonitorLog.WriteLog("Старт считывания параметров подключения БД из Host.config", MonitorLog.typelog.Info,
                    true);
                System.Data.Common.DbConnectionStringBuilder builder =
                    DBManager.getDbStringBuilder(this.connection.ConnectionString);
                string tempFileFullName = Path.GetTempFileName();
                string host = builder["Server"].ToString();
                string port = builder["Port"].ToString();
                string user = builder["User Id"].ToString();
                string password = builder["Password"].ToString();
                string database = builder["Database"].ToString();
                MonitorLog.WriteLog("Успешно выполнено считывание параметров подключения БД из Host.config ",
                    MonitorLog.typelog.Info, true);

                // снимем бэкап схемы
                string psqlCommand = "";
                if (newSchemaFullName == newBankPrms.kernelFullName)
                {
                    //если kernel, то бэкапим с данными
                    psqlCommand =
                        String.Format(
                            "pg_dump.exe --host {0} --port {1} --username {2} -F p --file \"{3}\" --serializable-deferrable --schema {4} {5} ",
                            host, port, user, tempFileFullName, oldSchemaFullName, database);
                    MonitorLog.WriteLog("Выбран бэкап '" + newBankPrms.kernelFullName + "'", MonitorLog.typelog.Info,
                        true);
                }
                else
                {
                    psqlCommand =
                        String.Format(
                            "pg_dump.exe --host {0} --port {1} --username {2} -F p --file \"{3}\" --schema-only --schema {4} {5} ",
                            host, port, user, tempFileFullName, oldSchemaFullName, database);
                }
                MonitorLog.WriteLog(
                    "Старт снятия бэкапа схем, которую копируем. Выполняемая команда: '" + psqlCommand + "'",
                    MonitorLog.typelog.Info, true);

                if (!ExecuteCommand(psqlCommand, password))
                {
                    ret.text = "Ошибка выполнения psql-команды! См. error.log ";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ret.result = false;
                    return ret;
                }

                // теперь заменим в файле старую схему на новую
                MonitorLog.WriteLog(
                    "Старт редактирования бэкапа схемы (изменение префикса схемы с '" + oldSchemaFullName + "' на '" +
                    newSchemaFullName + "' )" + " в файле " + tempFileFullName,
                    MonitorLog.typelog.Info, true);
                string text = File.ReadAllText(tempFileFullName);

                text = text.Replace(String.Format("CREATE SCHEMA {0};", oldSchemaFullName),
                    String.Format("CREATE SCHEMA {0};", newSchemaFullName));
                text = text.Replace(String.Format("ALTER SCHEMA {0} OWNER", oldSchemaFullName),
                    String.Format("ALTER SCHEMA {0} OWNER", newSchemaFullName));
                text = text.Replace(String.Format("SET search_path = {0}", oldSchemaFullName),
                    String.Format("SET search_path = {0}", newSchemaFullName));
                text = text.Replace(String.Format("ALTER FUNCTION {0}.", oldSchemaFullName),
                    String.Format("ALTER FUNCTION {0}.", newSchemaFullName));
                text = text.Replace(String.Format("ALTER TABLE {0}.", oldSchemaFullName),
                    String.Format("ALTER TABLE {0}.", newSchemaFullName));


                File.WriteAllText(tempFileFullName, text);
                MonitorLog.WriteLog("Успешно выполнено редактирование бэкапа схемы ",
                    MonitorLog.typelog.Info, true);


                psqlCommand =
                    String.Format(
                        "psql --host {0} --port {1} --username {2} --dbname {3} --file {4} ",
                        host, port, user, database, tempFileFullName);

                MonitorLog.WriteLog(
                    "Старт восстановления бэкапа схемы. Выполняемая команда: '" + psqlCommand + "' из файла " +
                    tempFileFullName, MonitorLog.typelog.Info, true);
                if (!ExecuteCommand(psqlCommand, password))
                {
                    ret.text = "Ошибка выполнения psql-команды! См. error.log ";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ret.result = false;
                }
                //проверяем - создалась ли схема
                string sql =
                    " SELECT schema_name FROM information_schema.schemata " +
                    " WHERE  LOWER( TRIM(schema_name) ) = '" + newSchemaFullName.Trim().ToLower() + "'";
                if (DBManager.ExecSQLToTable(this.connection, sql).Rows.Count < 1)
                {
                    ret.text = "Схема " + newSchemaFullName + " не создалась! Работа прекращена.";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ret.result = false;
                    return ret;
                }

                #region Дополнительные действия при создании схемы charge_xx
                if (oldSchemaFullName.Contains("charge"))
                {
                    //Выбираем все констрейнты из схемы charge_xx связанные с fin_xx
                    sql = string.Format(@"SELECT trim(tc.constraint_name) constraint_name, trim(tc.table_name) table_name FROM  information_schema.table_constraints AS tc
	                        WHERE constraint_type = 'FOREIGN KEY'  
                                AND tc.table_schema='{0}' AND tc.table_catalog = '{1}'", newSchemaFullName, connection.Database);
                    MyDataReader reader;
                    DBManager.ExecRead(connection, null, out reader, sql, true, 6000);
                    //Удалим все констрейнты с новой созданной charge_xx схемы
                    while (reader.Read())
                    {
                        sql = string.Format("ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}", newSchemaFullName + "." + reader["table_name"], reader["constraint_name"]);
                        DBManager.ExecSQL(connection, sql, true);
                    }
                    reader.Close();
                    //Создадим констрейнты
                    using (var db = new DbAdmin())
                    {
                        ret = db.AddForeignKey(1);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка фукции CopySchema! См. error.log";
                MonitorLog.WriteLog(ex.Message + ex.StackTrace, MonitorLog.typelog.Info, true);
                MessageBox.Show(ret.text, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ret.result = false;
            }
            finally
            {
                loadForm.Close();
            }
            return ret;
        }

        private bool ExecuteCommand(string command, string password)
        {
            try
            {
                string args = String.Format("/c {0}", command);
                // найдем место где есть утилиты postgresql
                string pgDirectory = ReadRegKey(Registry.CurrentUser, "SOFTWARE\\pgAdmin III", "PostgreSQLPath");
                if (String.IsNullOrEmpty(pgDirectory))
                {
                    pgDirectory = ReadRegKey(Registry.LocalMachine,
                        "SOFTWARE\\PostgreSQL Global Development Group\\PostgreSQL", "Location");
                    if (String.IsNullOrEmpty(pgDirectory))
                    {
                        // путь не найден, пытаемся найти dll рядом с hostman.exe в папке pg_library
                        pgDirectory = Path.Combine(Environment.CurrentDirectory, "pg_library\\");
                    }
                    else
                    {
                        pgDirectory += "\\bin";
                    }
                }

                MonitorLog.WriteLog(" Выбран путь для выполнения psql-команды: " + pgDirectory, MonitorLog.typelog.Info,
                    true);

                Environment.SetEnvironmentVariable("PGPASSWORD", password);
                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo("cmd.exe", args);
                info.WorkingDirectory = pgDirectory;
                //info.RedirectStandardError = true;
                //info.RedirectStandardOutput = true;
                //info.CreateNoWindow = true;
                //info.UseShellExecute = false;
                info.RedirectStandardError = false;
                info.RedirectStandardOutput = false;
                info.CreateNoWindow = false;
                info.UseShellExecute = false;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = true;
                proc.StartInfo = info;
                proc.Start();
                proc.WaitForExit();
                proc.Close();
                MonitorLog.WriteLog("Успешно выполнена psql-команда: " + command, MonitorLog.typelog.Info, true);
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    " Ошибка процедуры создании нового локального банка при выполнении функции ExecuteCommand. " +
                    Environment.NewLine +
                    " Выполняемая команда: " + command + Environment.NewLine +
                    " Текст ошибки: " + ex.Message + Environment.NewLine + ex.StackTrace, MonitorLog.typelog.Error, true);
                return false;
            }
        }

        public string ReadRegKey(RegistryKey baseKey, string subKey, string keyName)
        {
            // Opening the registry key
            RegistryKey rk = baseKey;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(subKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return null;
            }
            else
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    return (string) sk1.GetValue(keyName.ToUpper());
                }
                catch (Exception ex)
                {
                    //ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
                    MonitorLog.WriteException("Ошибка функции ReadRegKey", ex);
                    return null;
                }
            }
        }

        private bool IncludeBank(NewBankPrms bankPrms)
        {
            bool result = true;
            string sql = "";
            int newN = -100;
            int newNzpWp = -100;
            string newBankNumber = "null";

            DataTable dt = new DataTable();
            try
            {
                //выбираем новые nzp_wp, n, bank_number для fXXX_kernel.s_point
                sql =
                    " SELECT " +
                    " MAX(p.nzp_wp)+1 AS new_nzp_wp, " +
                    " MAX(p.n)+1 AS new_n, " +
                    " MAX(p.bank_number)+1 AS new_bank_number " +
                    " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point p";
                dt = DBManager.ExecSQLToTable(this.connection, sql);
                newNzpWp = Convert.ToInt32(dt.Rows[0]["new_nzp_wp"]);
                newN = Convert.ToInt32(dt.Rows[0]["new_n"]);
                newBankNumber = Utils.EStrNull(dt.Rows[0]["new_bank_number"].ToString());
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при подключении банка! (ф-ция IncludeBank). Текст Ошибки:" + ex.Message,
                    MonitorLog.typelog.Error, true);
                return false;
            }

            // прописать банк в fXXX_kernel.s_point
            sql =
                String.Format(
                    " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                    " (nzp_wp, nzp_graj, n, point, bd_kernel, flag, bank_number) " +
                    " VALUES ({0}, 1, {1}, '{2}', '{3}', 2, {4}) ",
                    newNzpWp,
                    newN,
                    bankPrms.newName,
                    bankPrms.newPref,
                    newBankNumber);
            Returns ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }

            //очищаем s_baselist (на всякий случай)
            sql = String.Format("truncate {0}s_baselist", bankPrms.kernelFullName + DBManager.tableDelimiter);
            DBManager.ExecSQL(this.connection, sql, true);

            // Теперь можно добавить в список периодов

            //Кладем в верхний банк
            sql = String.Format("insert into {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment, nzp_wp) " +
                                " values('{1}', {4}, {2}, '##wbc', 1, '{3}', " + newNzpWp + ")",
                Points.Pref + DBManager.sKernelAliasRest, bankPrms.chargeFullName,
                bankPrms.newYear, String.Format("Начисления {0}", bankPrms.newYear.ToString()),
                BaselistTypes.Charge.GetHashCode().ToString());
            ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }
            //Кладем в нижний банк
            // Системный
            sql = String.Format("insert into {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                " values('{1}', {4}, {2}, '##wac', 0, '{3}')",
                bankPrms.kernelFullName + DBManager.tableDelimiter, bankPrms.kernelFullName,
                bankPrms.newYear, "Системный", BaselistTypes.Kernel.GetHashCode().ToString());
            ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }
            // Характеристики
            sql = String.Format("insert into {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                " values('{1}', {4}, {2}, '##wac', 0, '{3}')",
                bankPrms.kernelFullName + DBManager.tableDelimiter, bankPrms.dataFullName,
                bankPrms.newYear, "Характеристики", BaselistTypes.Data.GetHashCode().ToString());
            ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }
            // начисления 
            sql = String.Format("insert into {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                " values('{1}', {4}, {2}, '##wbc', 1, '{3}')",
                bankPrms.kernelFullName + DBManager.tableDelimiter, bankPrms.chargeFullName,
                bankPrms.newYear, String.Format("Начисления {0}", bankPrms.newYear.ToString()),
                BaselistTypes.Charge.GetHashCode().ToString());
            ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }
            // финансы
            sql = String.Format("insert into {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                " values('{1}', {4}, {2}, '##wbc', 1, '{3}')",
                bankPrms.kernelFullName + DBManager.tableDelimiter, bankPrms.finFullName,
                bankPrms.newYear, String.Format("Финансы {0}", bankPrms.newYear.ToString()),
                BaselistTypes.Fin.GetHashCode().ToString());
            ret = DBManager.ExecSQL(this.connection, sql, true);
            if (!ret.result)
            {
                result = false;
            }

            LoadBanks();

            return result;
        }

        private bool FillNewBank(NewBankPrms newBankPrms, string oldBankPref, bool saveNormatifs)
        {
            MonitorLog.WriteLog("Старт заполнения нового локального банка данными", MonitorLog.typelog.Info, true);
            var result = true;
            
            try
            {
                #region Добавление новой УК
                /*
                // Убрал добавление новой УК, пусть добавляют через интерфейс :3                 
                var dbAdres = new DbAdresHard();
                MonitorLog.WriteLog("Старт добавления нового УК в верхний банк", MonitorLog.typelog.Info, true);
                string newAreaName = String.Format("НЕТ ДАННЫХ О УК ({0})", newBankPrms.newName);
                //обрезаем до 40 символов
                newAreaName = newAreaName.Length > 40 ? newAreaName.Remove(39) : newAreaName;
                var dbArea = new DbAdresKernel();
                var ret = dbArea.SaveArea(new Area()
                {
                    area = newAreaName,
                    nzp_area = -100,
                    nzp_user = 777
                });
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при добавлении нового УК в верхний банк: " + ret.text,
                        MonitorLog.typelog.Info, true);
                    infoLog.Append("Ошибка при добавлении нового УК в верхний банк!" + Environment.NewLine);
                    result = false;
                }
                else
                {
                    MonitorLog.WriteLog(
                        "Успешно выполнено добавления нового УК(nzp_area = '" + ret.tag + "') в верхний банк",
                        MonitorLog.typelog.Info, true);

                    MonitorLog.WriteLog("Старт спуска нового УК из верхнего банка в локальный", MonitorLog.typelog.Info,
                        true);
                    if (
                        !dbAdres.InsAreaFromCenterToLocal(connection,
                            new Dom() {pref = newBankPrms.newPref, nzp_area = ret.tag}).result)
                    {
                        MonitorLog.WriteLog("Ошибка при спуске нового УК из верхнего банка в локальный: " + ret.text,
                            MonitorLog.typelog.Info, true);
                        infoLog.Append("Ошибка при спуске нового УК из верхнего банка в локальный!" +
                                       Environment.NewLine);
                        result = false;
                    }
                    else
                    {
                        MonitorLog.WriteLog(
                            "Успешно выполнен спуск нового УК (nzp_area = '" + ret.tag +
                            "') из верхнего банка в локальный",
                            MonitorLog.typelog.Info, true);
                    }
                }
                */
                #endregion Добавление новой УК

                #region Установка расчетного месяца (saldo_date)

                MonitorLog.WriteLog("Старт установки расчетного месяца (saldo_date)", MonitorLog.typelog.Info, true);
                string sql =
                    " INSERT INTO " + newBankPrms.newPref + DBManager.sDataAliasRest + "saldo_date " +
                    " SELECT * FROM " + oldBankPref + DBManager.sDataAliasRest + "saldo_date ";
                if (!DBManager.ExecSQL(connection, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка установки расчетного месяца (saldo_date)! ", MonitorLog.typelog.Info,
                        true);
                    infoLog.Append("Ошибка при заполнении данных о рачетном месяце!");
                    result = false;
                }
                else
                {
                    MonitorLog.WriteLog("Успешно выполнена установка расчетного месяца (saldo_date)",
                        MonitorLog.typelog.Info, true);
                }

                #endregion Установка расчетного месяца (saldo_date)

                #region Заполнение схемы '_data' данными из верхнего банка (fXXX_data)

                List<string> tblNames = new List<string>();

                //Страны
                tblNames.Add("s_land");
                    //Регионы
                tblNames.Add("s_stat");

                    //Города/Районы
                tblNames.Add(    "s_town");

                    //Населенные пункты
                    tblNames.Add("s_rajon");

                    //Районы домов
                    tblNames.Add("s_rajon_dom");

                    //Улицы
                    tblNames.Add("s_ulica");

                    //Причины создания карточки жителей
                    tblNames.Add("s_cel");
                
                    //Типы удостоверений личности
                    tblNames.Add("s_dok");
                
                    //Государства
                    tblNames.Add("s_grgd");
                
                    //Справочник льгот
                    tblNames.Add("s_lgota");
                
                    //Органы регистрации
                    tblNames.Add("s_namereg");
                
                    //Национальности
                    tblNames.Add("s_nat");
                
                    //Родственные отношения
                    tblNames.Add("s_rod");
                
                    //Признаки судимости
                    tblNames.Add("s_sud");
                
                    //Типы карточек жителей
                    tblNames.Add("s_typkrt");
                
                    //Места выдачи документов
                    tblNames.Add("s_vid_mes");
                
                    //Справочник типов недопоставок
                    tblNames.Add("upg_s_kind_nedop");
                
                    //Подтипы недопоставок
                tblNames.Add("upg_s_nedop_type");
            
                FillTable(tblNames, Points.Pref + "_data", newBankPrms.dataFullName);

                #endregion Заполнение схемы '_data' данными из верхнего банка (fXXX_data)

                #region Заполнение схемы '_data' данными из нижнего банка (fXXX_data)

                tblNames.Clear();

                if (saveNormatifs)
                {
                    //Нормативы
                    tblNames.Add("prm_13");
                }

                //Типы удостоверений на собственность
                tblNames.Add("s_dok_sv");

                FillTable(tblNames, oldBankPref + "_data", newBankPrms.dataFullName);
                #endregion Заполнение схемы '_data' данными из нижнего банка (fXXX_data)
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }
        
        /// <summary>
        /// Ф-ция заполнения таблиц данными из другой схемы
        /// </summary>
        /// <param name="tblNamesArray">массив названий таблиц, которые надо заполнить</param>
        /// <param name="shemaFullNameFrom">префикс схемы из которой берем данные</param>
        /// <param name="shemaFullNameTo">префикс схемы в которую кладем данные</param>
        /// <returns></returns>
        private Returns FillTable(IEnumerable<string> tblNamesArray, string shemaFullNameFrom, string shemaFullNameTo)
        {

            Returns ret = new Returns();
            string sql = "";
            string message = "";
            int affectedRowsCount = -100;

            foreach (string oneTblName in tblNamesArray)
            {
                sql =
                    " INSERT   INTO " + shemaFullNameTo   + DBManager.tableDelimiter + oneTblName +
                    " SELECT * FROM " + shemaFullNameFrom + DBManager.tableDelimiter + oneTblName;

                if (!DBManager.ExecSQL(connection, null, sql, true, out affectedRowsCount).result)
                {
                    ret.result = false;
                    infoLog.Append("Ошибка при вставке в таблицу '" + oneTblName + "'. См. error.log " +
                                   Environment.NewLine);
                }
                else
                {
                    message = String.Format("Успешно вставлено из '{0}' в  '{1}' в кол-ве: {2} ",
                        shemaFullNameFrom + DBManager.tableDelimiter + oneTblName,
                        shemaFullNameTo + DBManager.tableDelimiter + oneTblName,
                        affectedRowsCount
                        );
                    MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                }
            }

            return ret;
        }

    }
}
