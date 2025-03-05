using System;
using System.Windows.Forms;
using System.Drawing;

namespace XNBConverter
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private TabControl tabControl;
        private TabPage tabConvertToXNB;
        private TabPage tabConvertFromXNB;
        private ListBox lstFiles;
        private ListBox lstXNBFiles;
        private TextBox txtOutputDirectoryToXNB;
        private TextBox txtOutputDirectoryFromXNB;
        private Button btnBrowseFiles;
        private Button btnBrowseXNBFiles;
        private Button btnBrowseOutputToXNB;
        private Button btnBrowseOutputFromXNB;
        private Button btnConvertToXNB;
        private Button btnConvertFromXNB;
        private Button btnDelete;
        private Button btnRename;
        private ProgressBar progressBarToXNB;
        private ProgressBar progressBarFromXNB;
        private Label lblSupportedFiles;
        private ComboBox comboBoxFormats;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Form Settings
            this.Text = "XNB Converter";
            this.Size = new System.Drawing.Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(800, 500);

            // Initialize all controls
            this.tabControl = new TabControl();
            this.tabConvertToXNB = new TabPage();
            this.tabConvertFromXNB = new TabPage();
            this.lstFiles = new ListBox();
            this.lstXNBFiles = new ListBox();
            this.txtOutputDirectoryToXNB = new TextBox();
            this.txtOutputDirectoryFromXNB = new TextBox();
            this.btnBrowseFiles = new Button();
            this.btnBrowseXNBFiles = new Button();
            this.btnBrowseOutputToXNB = new Button();
            this.btnBrowseOutputFromXNB = new Button();
            this.btnConvertToXNB = new Button();
            this.btnConvertFromXNB = new Button();
            this.progressBarToXNB = new ProgressBar();
            this.progressBarFromXNB = new ProgressBar();
            this.lblSupportedFiles = new Label();
            this.comboBoxFormats = new ComboBox();

            // TabControl
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Location = new Point(12, 12);
            this.tabControl.Size = new Size(760, 430);

            // TabPages
            this.tabConvertToXNB.Text = "Convert to XNB";
            this.tabConvertFromXNB.Text = "Convert from XNB";
            this.tabConvertToXNB.Padding = new Padding(10);
            this.tabConvertFromXNB.Padding = new Padding(10);

            // Label
            this.lblSupportedFiles.Location = new Point(20, 20);
            this.lblSupportedFiles.AutoSize = true;
            this.lblSupportedFiles.Text = "Supported Files: Images (.png, .jpg, .bmp), Audio (.wav, .mp3), Fonts (.spritefont)";

            // ListBoxes
            this.lstFiles = new ListBox();
            this.lstFiles.Location = new Point(20, 50);
            this.lstFiles.Size = new Size(700, 150);
            this.lstFiles.SelectionMode = SelectionMode.MultiExtended;
            this.lstFiles.HorizontalScrollbar = true;
            this.lstFiles.MouseDown += new MouseEventHandler(lstFiles_MouseDown);
            this.lstFiles.MouseMove += new MouseEventHandler(lstFiles_MouseMove);
            this.lstFiles.MouseUp += new MouseEventHandler(lstFiles_MouseUp);
            this.lstFiles.SelectedIndexChanged += new EventHandler(lstFiles_SelectedIndexChanged);

            this.lstXNBFiles.Location = new Point(20, 50);
            this.lstXNBFiles.Size = new Size(700, 150);
            this.lstXNBFiles.SelectionMode = SelectionMode.MultiExtended;
            this.lstXNBFiles.HorizontalScrollbar = true;

            // Browse Files button
            this.btnBrowseFiles.Text = "Browse Files";
            this.btnBrowseFiles.Location = new Point(20, 210);
            this.btnBrowseFiles.Size = new Size(100, 30);
            this.btnBrowseFiles.Click += new EventHandler(btnBrowseFiles_Click); // Add this line


            // Delete and Rename buttons (right of Browse)
            this.btnDelete = new Button();
            this.btnDelete.Text = "Delete Selected";
            this.btnDelete.Location = new Point(130, 210);
            this.btnDelete.Size = new Size(100, 30);
            this.btnDelete.Enabled = false;
            this.btnDelete.Click += new EventHandler(btnDelete_Click);

            this.btnRename = new Button();
            this.btnRename.Text = "Rename";
            this.btnRename.Location = new Point(240, 210);
            this.btnRename.Size = new Size(100, 30);
            this.btnRename.Enabled = false;
            this.btnRename.Click += new EventHandler(btnRename_Click);

            // Output directory controls
            this.txtOutputDirectoryToXNB.Location = new Point(20, 250);
            this.txtOutputDirectoryToXNB.Size = new Size(580, 23);
            this.txtOutputDirectoryToXNB.ReadOnly = true;

            this.btnBrowseOutputToXNB.Location = new Point(620, 250);
            this.btnBrowseOutputToXNB.Size = new Size(100, 23);
            this.btnBrowseOutputToXNB.Text = "Output Folder";
            this.btnBrowseOutputToXNB.Click += new EventHandler(btnBrowseOutputToXNB_Click);

            // Convert button and progress bar
            this.btnConvertToXNB.Location = new Point(20, 290);
            this.btnConvertToXNB.Size = new Size(700, 30);
            this.btnConvertToXNB.Text = "Convert to XNB";
            this.btnConvertToXNB.Click += new EventHandler(btnConvertToXNB_Click);

            this.progressBarToXNB.Location = new Point(20, 330);
            this.progressBarToXNB.Size = new Size(700, 23);

            // From XNB tab controls
            this.btnBrowseXNBFiles.Location = new Point(20, 210);
            this.btnBrowseXNBFiles.Size = new Size(100, 30);
            this.btnBrowseXNBFiles.Text = "Browse XNB Files";
            this.btnBrowseXNBFiles.Click += new EventHandler(btnBrowseXNBFiles_Click);

            this.txtOutputDirectoryFromXNB.Location = new Point(20, 250);
            this.txtOutputDirectoryFromXNB.Size = new Size(580, 23);
            this.txtOutputDirectoryFromXNB.ReadOnly = true;

            this.btnBrowseOutputFromXNB.Location = new Point(620, 250);
            this.btnBrowseOutputFromXNB.Size = new Size(100, 23);
            this.btnBrowseOutputFromXNB.Text = "Output Folder";
            this.btnBrowseOutputFromXNB.Click += new EventHandler(btnBrowseOutputFromXNB_Click);

            this.comboBoxFormats.Location = new Point(130, 210);
            this.comboBoxFormats.Size = new Size(200, 23);
            this.comboBoxFormats.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBoxFormats.Items.AddRange(new object[] {
                ".png", ".jpg", ".bmp", ".wav", ".mp3", ".spritefont"
            });

            this.btnConvertFromXNB.Location = new Point(20, 290);
            this.btnConvertFromXNB.Size = new Size(700, 30);
            this.btnConvertFromXNB.Text = "Convert from XNB";
            this.btnConvertFromXNB.Click += new EventHandler(btnConvertFromXNB_Click);

            this.progressBarFromXNB.Location = new Point(20, 330);
            this.progressBarFromXNB.Size = new Size(700, 23);

            // Add controls to tabs
            this.tabConvertToXNB.Controls.AddRange(new Control[] {
                this.lblSupportedFiles,
                this.lstFiles,
                this.btnBrowseFiles,
                this.btnDelete,
                this.btnRename,
                this.txtOutputDirectoryToXNB,
                this.btnBrowseOutputToXNB,
                this.btnConvertToXNB,
                this.progressBarToXNB
            });

            this.tabConvertFromXNB.Controls.AddRange(new Control[] {
                this.lstXNBFiles,
                this.btnBrowseXNBFiles,
                this.comboBoxFormats,
                this.txtOutputDirectoryFromXNB,
                this.btnBrowseOutputFromXNB,
                this.btnConvertFromXNB,
                this.progressBarFromXNB
            });

            // Add TabControl to form
            this.tabControl.Controls.Add(this.tabConvertToXNB);
            this.tabControl.Controls.Add(this.tabConvertFromXNB);
            this.Controls.Add(this.tabControl);
        }
    }
}
