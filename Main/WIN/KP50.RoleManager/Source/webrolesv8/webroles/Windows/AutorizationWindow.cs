using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;

namespace webroles
{
    public partial class AutorizationWindow : Form
    {
        private TextBox loginTextBox;
        private TextBox passwordTextBox;
        private string login = String.Empty;
        private string userName = string.Empty;
        private int userId = 0;
        public string Login
        {
            get {return login;}
        }
        public string UserName
        {
            get { return userName; }
        }
        public int UserId
        {
            get { return userId; }
        }
        public AutorizationWindow()
        {
            InitializeComponent();

            Text = "Авторизация";
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


            Label loginLabel = new Label();
            loginLabel.Parent = tableLayPan;
            loginLabel.AutoSize = true;
            loginLabel.Text = "Логин:";
            loginLabel.Margin = new System.Windows.Forms.Padding(10);
            loginLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));

            loginTextBox = new TextBox();
            loginTextBox.Parent = tableLayPan;
            loginTextBox.Margin = new System.Windows.Forms.Padding(8);

            Label passwordLabel = new Label();
            passwordLabel.Parent = tableLayPan;
            passwordLabel.AutoSize = true;
            passwordLabel.Text = "Пароль:";
            passwordLabel.Margin = new System.Windows.Forms.Padding(10);
            passwordLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            passwordTextBox = new TextBox();

            passwordTextBox.Parent = tableLayPan;
            passwordTextBox.Margin = new System.Windows.Forms.Padding(8);
            passwordTextBox.UseSystemPasswordChar = true;
            Button okButtton = new Button();
            okButtton.Parent = tableLayPan;
            okButtton.Text = "ОК";
            okButtton.AutoSize = true;
            okButtton.Margin = new System.Windows.Forms.Padding(10, 20, 20, 10);
            okButtton.Anchor = AnchorStyles.Left;
            okButtton.Click += okButtton_Click;
           // tableLayPan.Controls.Add(okButtton, 0, 2);
            Button cancelButtton = new Button();
            cancelButtton.AutoSize = true;
            cancelButtton.Parent = tableLayPan;
            cancelButtton.Text = "Отмена";
            cancelButtton.AutoSize = true;
            cancelButtton.Margin = new System.Windows.Forms.Padding(30, 20, 8, 10);
            cancelButtton.Click += cancelButtton_Click;
            // tableLayPan.Controls.Add(cancelButtton, 1, 4);
            Button registrationButtton = new Button();
            registrationButtton.Text = "Регистрация";
            registrationButtton.AutoSize = true;
            registrationButtton.Parent = tableLayPan;
            registrationButtton.Width =  tableLayPan.Size.Width;
            registrationButtton.Margin = new System.Windows.Forms.Padding(10, 7, 8, 7);
            tableLayPan.SetColumnSpan(registrationButtton, 2);
            registrationButtton.Click += registrationButtton_Click;
            tableLayPan.ResumeLayout();
            AcceptButton = okButtton;
            CancelButton = CancelButton;
         }

        void cancelButtton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void registrationButtton_Click(object sender, EventArgs e)
        {
            RegistrationWindow registration = new RegistrationWindow();
            if (registration.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            userName = registration.UserName;
            login = registration.Login;
            userId = registration.UserId;
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        void okButtton_Click(object sender, EventArgs e)
        {

            #region Проверка введенных имени пользователя и пароля
            if (loginTextBox.Text.Equals(String.Empty))
            {
                MessageBox.Show("Введите логин", "Введите логин", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (passwordTextBox.Text.Equals(String.Empty))
            {
                MessageBox.Show("Введите пароль", "Введите пароль", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            #endregion

           var connection=  ConnectionToPostgreSqlDb.GetConnection();
           string passwordHash = PasswordHashCode.createHash(passwordTextBox.Text);
           string selectStatement = "Select id, login, user_name, pwd from users " + "Where users.login=" + "'" + loginTextBox.Text + "'" + " and " + "users.pwd=" + "'" + passwordHash + "'";
           NpgsqlCommand selectCommand = new NpgsqlCommand(selectStatement, connection);
            IDataReader reader = null;

            try
            {
                connection.Open();
                reader = selectCommand.ExecuteReader();
                if (!reader.Read())
                {
                    MessageBox.Show("Неверный логин или пароль", "Неверный логин или пароль", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                login = reader["login"].ToString();
                userName = reader["user_name"].ToString();
                userId = (int) reader["id"];
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
           DialogResult = DialogResult.OK;
        }
    }
}
