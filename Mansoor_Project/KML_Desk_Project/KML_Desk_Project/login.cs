using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KML_Desk_Project
{
    public partial class login : Form
    {
        public login()
        {
            InitializeComponent();
        }

        private void Username_TextChanged(object sender, EventArgs e)
        {

        }

        private void Password_TextChanged(object sender, EventArgs e)
        {

        }

        private void Showpassword_CheckedChanged(object sender, EventArgs e)
        {
            Password.PasswordChar = Showpassword.Checked ? '\0' : '*';
        }

        private void clear_Click(object sender, EventArgs e)
        {
            Username.Clear();
            Password.Clear();
        }

        private void login_Click(object sender, EventArgs e)
        {
            if (Username.Text == "mansoor" && Password.Text == "12345")
            {

                MessageBox.Show("Login Successful");
                HomePage Home = new HomePage();
                this.Hide();
                Home.Show();

            }
            else
            {
                MessageBox.Show("Incorrect Username and Password");
            }
        }
        private bool isValid()
        {
            if (Username.Text.TrimStart() == string.Empty || Password.Text.TrimStart() == string.Empty)
            {
                MessageBox.Show("Enter Username and Password");
                return false;
            }
            return true;
        }

        private void exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
