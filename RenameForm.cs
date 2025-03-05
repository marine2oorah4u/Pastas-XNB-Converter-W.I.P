using System;
using System.Drawing;
using System.Windows.Forms;

namespace XNBConverter
{
    public class RenameForm : Form
    {
        private TextBox txtNewName;
        private Button btnOK;
        private Button btnCancel;

        public string NewFileName { get; private set; }

        public RenameForm(string currentFileName)
        {
            InitializeComponents(currentFileName);
            ApplyTheme();
        }

        private void InitializeComponents(string currentFileName)
        {
            this.Text = "Rename File";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            txtNewName = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(240, 23),
                Text = currentFileName
            };

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 60),
                Size = new Size(75, 23)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(185, 60),
                Size = new Size(75, 23)
            };

            btnOK.Click += (s, e) =>
            {
                NewFileName = txtNewName.Text;
                if (string.IsNullOrWhiteSpace(NewFileName))
                {
                    MessageBox.Show("Please enter a valid filename.");
                    DialogResult = DialogResult.None;
                }
            };

            this.Controls.AddRange(new Control[] { txtNewName, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeColors.Background;
            this.ForeColor = ThemeColors.TextColor;

            txtNewName.BackColor = ThemeColors.DarkBackground;
            txtNewName.ForeColor = ThemeColors.TextColor;
            txtNewName.BorderStyle = BorderStyle.FixedSingle;

            foreach (Button btn in new[] { btnOK, btnCancel })
            {
                btn.BackColor = ThemeColors.ControlBackground;
                btn.ForeColor = ThemeColors.TextColor;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = ThemeColors.BorderColor;
            }
        }
    }
}