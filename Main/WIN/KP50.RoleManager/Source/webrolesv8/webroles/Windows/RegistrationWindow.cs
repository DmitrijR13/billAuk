using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;


namespace webroles
{
    public partial class RegistrationWindow : Form
    {
 
        private readonly TextBox userNameTextBox;
        private readonly TextBox loginTextBox;
        private readonly TextBox passwordTextBox;
        private readonly TextBox confirmPasswordTextBox;
        private string login = String.Empty;
        private string userName = string.Empty;
        private int userId = 0;


        public string Login
        {
            get { return login; }
        }
        public string UserName
        {
            get { return userName; }
        }
        public int UserId
        {
            get { return userId; }
        }

        public RegistrationWindow()
        {
            InitializeComponent();
            Text = "Регистрация";
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MaximizeBox = false;
            MinimizeBox = false;

            TableLayoutPanel tableLayPan = new TableLayoutPanel();
            tableLayPan.Parent = this;
            tableLayPan.Padding = new Padding(5);
            tableLayPan.AutoSize = true;
            tableLayPan.ColumnCount = 2;
            //  tableLayPan.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            tableLayPan.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            tableLayPan.SuspendLayout();
            var labels = new string[] { "Имя пользователя:", "Логин:", "Пароль:", "Подтвердить пароль:" };
            for (int i = 0; i < 4; i++)
            {
                Label lbl = new Label();
                lbl.Parent = tableLayPan;
                lbl.AutoSize = true;
                lbl.Text = labels[i];
                lbl.Margin = new System.Windows.Forms.Padding(10);
                lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
                tableLayPan.Controls.Add(lbl, 0, i);
            }


            userNameTextBox = new TextBox();
            userNameTextBox.Parent = tableLayPan;
            userNameTextBox.Margin = new System.Windows.Forms.Padding(8);
            tableLayPan.Controls.Add(userNameTextBox, 1, 0);
          
            loginTextBox = new TextBox();
            loginTextBox.Parent = tableLayPan;
            loginTextBox.Margin = new System.Windows.Forms.Padding(8);
            tableLayPan.Controls.Add(loginTextBox, 1, 1);

             passwordTextBox = new TextBox();
            passwordTextBox.Parent = tableLayPan;
            passwordTextBox.Margin = new System.Windows.Forms.Padding(8);
            passwordTextBox.UseSystemPasswordChar = true;
            tableLayPan.Controls.Add(passwordTextBox, 1, 2);


            confirmPasswordTextBox = new TextBox();
            confirmPasswordTextBox.Parent = tableLayPan;
            confirmPasswordTextBox.Margin = new System.Windows.Forms.Padding(8);
            confirmPasswordTextBox.UseSystemPasswordChar = true;
            tableLayPan.Controls.Add(confirmPasswordTextBox, 1, 3);

            Button okButtton = new Button();
            okButtton.Parent = tableLayPan;
            okButtton.Text = "ОК";
            okButtton.AutoSize = true;
            okButtton.Margin = new System.Windows.Forms.Padding(10, 20, 20, 10);
            okButtton.Anchor = AnchorStyles.Left;
            okButtton.Click += okButtton_Click;

            Button cancelButtton = new Button();
            cancelButtton.AutoSize = true;
            cancelButtton.Parent = tableLayPan;
            cancelButtton.Text = "Отмена";
            cancelButtton.AutoSize = true;
            cancelButtton.Margin = new System.Windows.Forms.Padding(30, 20, 8, 10);
            cancelButtton.Click += cancelButtton_Click;
            tableLayPan.ResumeLayout();

            AcceptButton = okButtton;
            CancelButton = CancelButton;
        }

        void okButtton_Click(object sender, EventArgs e)
        {
            # region Проверка полей ввода
            if (alarmMessage(userNameTextBox.Text, "Введите имя пользователя")) return;
            if (alarmMessage(loginTextBox.Text, "Введите логин")) return;
            if (alarmMessage(passwordTextBox.Text, "Введите пароль")) return;
            if (alarmMessage(confirmPasswordTextBox.Text, "Подтвердите пароль")) return;
            if (!confirmPasswordTextBox.Text.Equals(passwordTextBox.Text))
            {
                MessageBox.Show("Пароли не совпадают", "Пароли не совпадают", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            #endregion

            var connection = ConnectionToPostgreSqlDb.GetConnection();
            NpgsqlCommand command;
            string selectSameLogin = "Select login from users " + "Where users.login=" + "'"+loginTextBox.Text+"'";
            command = new NpgsqlCommand(selectSameLogin, connection);
            IDataReader reader = null;
            try
            {
                connection.Open();
                reader = command.ExecuteReader();
                // Проверка логина на уникальность
                while (reader.Read())
                {
                    //reader["login"].ToString();
                    MessageBox.Show("Введенный логин уже существует", "Логин уже существует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Помещение данных нового пользователя в базу
                string passwordHash = PasswordHashCode.createHash(passwordTextBox.Text);
                string insertStatement = "Insert into users (login, user_name, pwd) " + "Values (" + "'" + loginTextBox.Text + "'" + "," + "'" + userNameTextBox.Text + "'" + "," + "'" + passwordHash + "'" + ")";
                command = new NpgsqlCommand(insertStatement, connection);
                var rowCount = command.ExecuteNonQuery();

                //извлечение id вновь созданного пользователя
                string selectNewUserId = "Select id from users where login=" + "'" + loginTextBox.Text + "'";
                command = new NpgsqlCommand(selectNewUserId, connection);
                command.CommandTimeout = 50;
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    userId = (int) reader["id"];
                    break;
                }
            }
            catch (NpgsqlException except)
            {
                MessageBox.Show("Ошибка " + except.ErrorCode.ToString() + ". " + except.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                if (reader!=null) reader.Close();
                if (connection!=null) connection.Close();
            }
            userName = userNameTextBox.Text;
            login = loginTextBox.Text;
            DialogResult = DialogResult.OK;
        }

        void cancelButtton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private bool alarmMessage(string checkedString, string WarninString)
        {
            if (checkedString.Equals(String.Empty))
            {
                MessageBox.Show(WarninString, WarninString, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            return false;
        }
    }
}
