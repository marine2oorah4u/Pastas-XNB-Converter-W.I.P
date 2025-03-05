// MainForm.cs

using System.Text;
using System.Drawing;


using System.Diagnostics;
using System.Windows.Forms;

using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace XNBConverter
{

    public partial class MainForm : Form
    {
        private const string FOLDER_CONVERT_TO_XNB = "XNB Converter - Output XNBs";
        private const string FOLDER_CONVERT_FROM_XNB = "XNB Converter - Decompressed Files";


        private string _outputDirectoryPathToXNB;
        private string _outputDirectoryPathFromXNB;

        private bool isDragging = false;
        private Point dragStartPoint;

        private int dragStartIndex = -1;
        private int lastSelectedIndex = -1;






        public MainForm()
        {
            InitializeComponent();
            SetupDefaultFolders();
            SetupInitialState();
            ApplyTheme();
        }



        private string DetermineOutputExtension(string inputFile)
        {
            string fileName = Path.GetFileName(inputFile).ToLower();

            if (fileName.Contains("texture") || fileName.Contains("sprite") ||
                fileName.Contains("palette") || fileName.Contains("avatar"))
                return ".png";
            if (fileName.Contains("sound") || fileName.Contains("audio"))
                return ".wav";
            if (fileName.Contains("font"))
                return ".spritefont";

            return ".bin"; // Default extension
        }


        private void ConvertFromXNB(string inputFile, string outputDirectory)
        {
            try
            {
                using (var fileStream = File.OpenRead(inputFile))
                using (var reader = new BinaryReader(fileStream))
                {
                    string magic = new string(reader.ReadChars(3));
                    if (magic != "XNB")
                        throw new Exception("Invalid XNB file format");

                    reader.ReadByte(); // Platform
                    reader.ReadByte(); // Version
                    byte flags = reader.ReadByte(); // Flags
                    uint fileSize = reader.ReadUInt32(); // File size

                    bool isCompressed = (flags & 0x80) != 0;
                    if (isCompressed)
                    {
                        byte[] compressedData = reader.ReadBytes((int)fileSize - 10);
                        byte[] decompressedData = DecompressLZ4(compressedData, (int)fileSize - 10);
                        using (var decompressedStream = new MemoryStream(decompressedData))
                        using (var decompressedReader = new BinaryReader(decompressedStream))
                        {
                            ReadXNBData(decompressedReader, outputDirectory, inputFile);
                        }
                    }
                    else
                    {
                        ReadXNBData(reader, outputDirectory, inputFile);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Conversion Error", $"Error converting {Path.GetFileName(inputFile)}\n\nError Details:\n{ex.Message}\n\n{ex.StackTrace}");
            }
        }


        private void ReadXNBData(BinaryReader reader, string outputDirectory, string inputFile)
        {
            int typeReaderCount = Read7BitEncodedInt(reader);
            List<string> typeReaders = new List<string>();

            for (int i = 0; i < typeReaderCount; i++)
            {
                string typeReaderName = ReadString(reader);
                int versionNumber = Read7BitEncodedInt(reader);
                typeReaders.Add(typeReaderName);
                Debug.WriteLine($"Type Reader {i}: {typeReaderName}, Version: {versionNumber}");
            }

            int sharedResourceCount = Read7BitEncodedInt(reader);
            Debug.WriteLine($"Shared Resource Count: {sharedResourceCount}");

            string extension = DetermineOutputExtensionFromTypeReaders(typeReaders);

            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".bmp":
                    ConvertTextureData(reader, outputDirectory, inputFile);
                    break;
                case ".wav":
                case ".mp3":
                    ConvertSoundEffectData(reader, outputDirectory, inputFile);
                    break;
                case ".spritefont":
                    ConvertSpriteFontData(reader, outputDirectory, inputFile);
                    break;
                default:
                    MessageBox.Show($"Unsupported file format: {extension}");
                    break;
            }
        }




        private string DetermineOutputExtensionFromTypeReaders(List<string> typeReaders)
        {
            foreach (var typeReader in typeReaders)
            {
                if (typeReader.Contains("Texture2DReader"))
                    return ".png";
                if (typeReader.Contains("SoundEffectReader"))
                    return ".wav";
                if (typeReader.Contains("SpriteFontReader"))
                    return ".spritefont";
            }
            return ".bin"; // Default to binary if no known type reader found
        }




        private void ConvertSpriteFontData(BinaryReader reader, string outputDirectory, string inputFile)
        {
            int characterCount = reader.ReadInt32();
            Dictionary<char, SpriteFontCharacter> characters = new Dictionary<char, SpriteFontCharacter>();

            for (int i = 0; i < characterCount; i++)
            {
                char character = reader.ReadChar();
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int xOffset = reader.ReadInt32();
                int yOffset = reader.ReadInt32();
                int xAdvance = reader.ReadInt32();

                SpriteFontCharacter spriteFontCharacter = new SpriteFontCharacter
                {
                    Character = character,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    XOffset = xOffset,
                    YOffset = yOffset,
                    XAdvance = xAdvance
                };

                characters.Add(character, spriteFontCharacter);
            }

            int textureWidth = reader.ReadInt32();
            int textureHeight = reader.ReadInt32();
            int textureDataSize = reader.ReadInt32();
            byte[] textureData = reader.ReadBytes(textureDataSize);

            using (Bitmap bitmap = new Bitmap(textureWidth, textureHeight, PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, textureWidth, textureHeight),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    byte[] pixelData = new byte[textureWidth * textureHeight * 4];

                    for (int i = 0; i < textureDataSize; i += 4)
                    {
                        pixelData[i] = textureData[i + 2];     // B
                        pixelData[i + 1] = textureData[i + 1]; // G
                        pixelData[i + 2] = textureData[i];     // R
                        pixelData[i + 3] = textureData[i + 3]; // A
                    }

                    Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                string texturePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFile) + "_texture.png");
                bitmap.Save(texturePath, ImageFormat.Png);
            }

            string spriteFontPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".spritefont");
            using (StreamWriter writer = new StreamWriter(spriteFontPath))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<XnaContent xmlns:Graphics=\"Microsoft.Xna.Framework.Content.Pipeline.Graphics\">");
                writer.WriteLine("  <Asset Type=\"Graphics:FontDescription\">");
                writer.WriteLine("    <FontName>Segoe UI Mono</FontName>");
                writer.WriteLine("    <Size>14</Size>");
                writer.WriteLine("    <Spacing>0</Spacing>");
                writer.WriteLine("    <UseKerning>true</UseKerning>");
                writer.WriteLine("    <Style>Regular</Style>");
                writer.WriteLine("    <DefaultCharacter>*</DefaultCharacter>");
                writer.WriteLine("    <CharacterRegions>");
                writer.WriteLine("      <CharacterRegion>");
                writer.WriteLine("        <Start>&#32;</Start>");
                writer.WriteLine("        <End>&#126;</End>");
                writer.WriteLine("      </CharacterRegion>");
                writer.WriteLine("    </CharacterRegions>");
                writer.WriteLine("  </Asset>");
                writer.WriteLine("</XnaContent>");
            }
        }

        private class SpriteFontCharacter
        {
            public char Character { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int XOffset { get; set; }
            public int YOffset { get; set; }
            public int XAdvance { get; set; }
        }



        private void ConvertSoundEffectData(BinaryReader reader, string outputDirectory, string inputFile)
        {
            int format = reader.ReadInt32();
            int sampleRate = reader.ReadInt32();
            short channels = reader.ReadInt16();
            short blockAlign = reader.ReadInt16();

            int dataSize = reader.ReadInt32();
            byte[] soundData = reader.ReadBytes(dataSize);

            using (FileStream fs = File.Create(Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".wav")))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + dataSize);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((short)1);  // PCM
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);  // bytes per second
                writer.Write((short)(channels * 2));      // block align
                writer.Write((short)16);                  // bits per sample
                writer.Write("data".ToCharArray());
                writer.Write(dataSize);
                writer.Write(soundData);
            }
        }




        private void ConvertTextureData(BinaryReader reader, string outputDirectory, string inputFile)
        {
            try
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                // Read and validate the XNB header
                char[] header = reader.ReadChars(3);
                if (new string(header) != "XNB")
                {
                    Debug.WriteLine("Not a valid XNB file.");
                    return;
                }

                byte platform = reader.ReadByte();
                byte version = reader.ReadByte();
                byte flags = reader.ReadByte();
                int fileSize = reader.ReadInt32();

                Debug.WriteLine($"Platform: {platform}, Version: {version}, Flags: {flags}, File Size: {fileSize}");

                // Read Type Reader count using 7-bit encoded int
                int typeReaderCount = Read7BitEncodedInt(reader);
                Debug.WriteLine($"Type Reader Count: {typeReaderCount}");

                for (int i = 0; i < typeReaderCount; i++)
                {
                    string typeReader = reader.ReadString();
                    Debug.WriteLine($"Type Reader {i}: {typeReader}");
                }

                // Read Shared Resource Count
                int sharedResourceCount = Read7BitEncodedInt(reader);
                Debug.WriteLine($"Shared Resource Count: {sharedResourceCount}");

                // Assuming the next data should be the width and height after shared resources
                // If the position is not correct, adjust this offset
                reader.BaseStream.Seek(4, SeekOrigin.Current); // Adjust if necessary

                // Read width and height
                uint width = reader.ReadUInt32();
                uint height = reader.ReadUInt32();
                Debug.WriteLine($"Width: {width}, Height: {height}");

                // Validate width and height
                if (width == 0 || height == 0 || width > 4096 || height > 4096)
                {
                    Debug.WriteLine("Invalid width or height");
                    return;
                }

                int mipCount = reader.ReadInt32();
                Debug.WriteLine($"Mip Count: {mipCount}");

                int dataSize = reader.ReadInt32();
                Debug.WriteLine($"Data Size: {dataSize}");

                byte[] textureData = reader.ReadBytes(dataSize);

                string texturePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".png");
                Debug.WriteLine($"Texture Path: {texturePath}");

                // Create the bitmap and save it
                using (Bitmap bitmap = new Bitmap((int)width, (int)height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = (y * (int)width * 4) + (x * 4);
                            if (index + 3 < textureData.Length)
                            {
                                Color color = Color.FromArgb(textureData[index + 3], textureData[index + 2], textureData[index + 1], textureData[index]);
                                bitmap.SetPixel(x, y, color);
                            }
                        }
                    }

                    using (FileStream fileStream = new FileStream(texturePath, FileMode.Create))
                    {
                        bitmap.Save(fileStream, ImageFormat.Png);
                    }
                    Debug.WriteLine($"File exists: {File.Exists(texturePath)}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting texture: {ex.Message}");
            }
        }

        // 7-bit encoded integer reading method
        private int Read7BitEncodedInt(BinaryReader reader)
        {
            int count = 0;
            int shift = 0;
            byte b;

            do
            {
                if (shift == 35) // More than 5 bytes is too large for an int
                {
                    throw new FormatException("Too many bytes in what should have been a 7-bit encoded integer.");
                }

                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;

            } while ((b & 0x80) != 0);

            return count;
        }








        public partial class ErrorDialog : Form
        {
            private string errorMessage;

            public ErrorDialog(string message)
            {
                InitializeComponent();
                errorMessage = message;
                txtErrorMessage.Text = message;
            }

            private void btnCopy_Click(object sender, EventArgs e)
            {
                Clipboard.SetText(errorMessage);
                MessageBox.Show("Error text copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            private void InitializeComponent()
            {
                this.txtErrorMessage = new TextBox();
                this.btnCopy = new Button();
                var layoutPanel = new TableLayoutPanel();

                this.SuspendLayout();

                // layoutPanel
                layoutPanel.Dock = DockStyle.Fill;
                layoutPanel.RowCount = 2;
                layoutPanel.ColumnCount = 1;
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // TextBox fills the remaining space
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Button height

                // txtErrorMessage
                this.txtErrorMessage.Multiline = true;
                this.txtErrorMessage.ReadOnly = true;
                this.txtErrorMessage.ScrollBars = ScrollBars.Vertical;
                this.txtErrorMessage.Dock = DockStyle.Fill;

                // btnCopy
                this.btnCopy.Text = "Copy";
                this.btnCopy.Dock = DockStyle.Fill;
                this.btnCopy.Click += new EventHandler(this.btnCopy_Click);

                // Add controls to layoutPanel
                layoutPanel.Controls.Add(this.txtErrorMessage, 0, 0);
                layoutPanel.Controls.Add(this.btnCopy, 0, 1);

                // ErrorDialog
                this.ClientSize = new Size(600, 600);
                this.Controls.Add(layoutPanel);
                this.Text = "Error Details";
                this.ResumeLayout(false);
            }

            private TextBox txtErrorMessage;
            private Button btnCopy;
        }

        private void ShowErrorDialog(string title, string message)
        {
            using (var errorDialog = new ErrorDialog(message))
            {
                errorDialog.ShowDialog();
            }
        }


        private string MakeSafeFileName(string fileName)
        {
            // Remove invalid characters
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            // Ensure the filename isn't too long
            if (fileName.Length > 255)
            {
                string extension = Path.GetExtension(fileName);
                fileName = fileName.Substring(0, 255 - extension.Length) + extension;
            }

            return fileName;
        }



        private void ProcessAsSimpleContent(BinaryReader reader, long contentLength, string outputDirectory, string fileName)
        {
            // Read content directly, skipping type reader information
            byte[] content = reader.ReadBytes((int)contentLength);

            // Try to determine content type from the data
            string extension = ".bin";
            if (IsImageContent(content))
                extension = ".png";
            else if (IsAudioContent(content))
                extension = ".wav";

            string outputPath = Path.Combine(outputDirectory, fileName + extension);
            File.WriteAllBytes(outputPath, content);
        }

        private bool IsImageContent(byte[] content)
        {
            // Simple check for common image headers
            if (content.Length < 8) return false;

            // Check for PNG signature
            if (content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47)
                return true;

            // Check for JPEG signature
            if (content[0] == 0xFF && content[1] == 0xD8)
                return true;

            return false;
        }

        private bool IsAudioContent(byte[] content)
        {
            // Simple check for WAV header
            if (content.Length < 12) return false;

            // Check for "RIFF" and "WAVE" markers
            return content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46 &&
                   content[8] == 0x57 && content[9] == 0x41 && content[10] == 0x56 && content[11] == 0x45;
        }


        private void lstFiles_MouseDown(object sender, MouseEventArgs e)
        {
            if (lstFiles.Items.Count == 0) return;

            isDragging = true;
            dragStartPoint = e.Location;

            // Clear selection if not holding Ctrl
            if ((ModifierKeys & Keys.Control) == 0)
            {
                lstFiles.ClearSelected();
            }
        }

        private void lstFiles_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || lstFiles.Items.Count == 0) return;

            // Get the indices of items at both the start and current points
            int startIndex = lstFiles.IndexFromPoint(dragStartPoint);
            int currentIndex = lstFiles.IndexFromPoint(e.Location);

            // Handle cases where mouse is above or below the ListBox
            if (e.Y < 0)
                currentIndex = 0;
            else if (e.Y > lstFiles.Height)
                currentIndex = lstFiles.Items.Count - 1;

            // If clicking started in empty space, use nearest item
            if (startIndex == -1)
            {
                startIndex = NearestItemIndex(dragStartPoint.Y);
            }
            if (currentIndex == -1)
            {
                currentIndex = NearestItemIndex(e.Y);
            }

            try
            {
                int start = Math.Min(startIndex, currentIndex);
                int end = Math.Max(startIndex, currentIndex);

                // Ensure start and end are within bounds
                start = Math.Max(0, Math.Min(start, lstFiles.Items.Count - 1));
                end = Math.Max(0, Math.Min(end, lstFiles.Items.Count - 1));

                // Clear previous selection if not using modifier keys
                if ((ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
                {
                    lstFiles.ClearSelected();
                }

                // Select all items in the range
                for (int i = start; i <= end; i++)
                {
                    lstFiles.SetSelected(i, true);
                }

                UpdateButtonStates();
            }
            catch (ArgumentOutOfRangeException)
            {
                isDragging = false;
            }
        }

        private void lstFiles_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            UpdateButtonStates();
        }

        private int NearestItemIndex(int y)
        {
            int itemHeight = lstFiles.ItemHeight;
            int index = y / itemHeight;

            // Constrain to valid range
            if (index < 0) return 0;
            if (index >= lstFiles.Items.Count) return lstFiles.Items.Count - 1;

            return index;
        }






        private void SetupDefaultFolders()
        {
            // Get user's Documents folder path
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Setup To XNB output folder
            _outputDirectoryPathToXNB = Path.Combine(documentsPath, FOLDER_CONVERT_TO_XNB);
            if (!Directory.Exists(_outputDirectoryPathToXNB))
            {
                Directory.CreateDirectory(_outputDirectoryPathToXNB);
            }
            txtOutputDirectoryToXNB.Text = _outputDirectoryPathToXNB;

            // Setup From XNB output folder
            _outputDirectoryPathFromXNB = Path.Combine(documentsPath, FOLDER_CONVERT_FROM_XNB);
            if (!Directory.Exists(_outputDirectoryPathFromXNB))
            {
                Directory.CreateDirectory(_outputDirectoryPathFromXNB);
            }
            txtOutputDirectoryFromXNB.Text = _outputDirectoryPathFromXNB;
        }




        private void SetupInitialState()
        {
            comboBoxFormats.SelectedIndex = 0;
            btnConvertToXNB.Enabled = false;
            btnConvertFromXNB.Enabled = false;
            btnDelete.Enabled = false;
            btnRename.Enabled = false;
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeColors.Background;
            this.ForeColor = ThemeColors.TextColor;
            ApplyThemeToControls(this.Controls);
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Button btn)
                {
                    btn.BackColor = ThemeColors.ControlBackground;
                    btn.ForeColor = ThemeColors.TextColor;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = ThemeColors.BorderColor;
                    btn.FlatAppearance.MouseOverBackColor = ThemeColors.AccentColor;
                    btn.FlatAppearance.MouseDownBackColor = ThemeColors.DarkBackground;
                }
                else if (control is TextBox txt)
                {
                    txt.BackColor = ThemeColors.DarkBackground;
                    txt.ForeColor = ThemeColors.TextColor;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ListBox lst)
                {
                    lst.BackColor = ThemeColors.DarkBackground;
                    lst.ForeColor = ThemeColors.TextColor;
                    lst.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is TabControl tab)
                {
                    tab.BackColor = ThemeColors.Background;
                    tab.ForeColor = ThemeColors.TextColor;
                }
                else if (control is TabPage tabPage)
                {
                    tabPage.BackColor = ThemeColors.Background;
                    tabPage.ForeColor = ThemeColors.TextColor;
                }
                else if (control is Label lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    lbl.ForeColor = ThemeColors.TextColor;
                }
                else if (control is ComboBox combo)
                {
                    combo.BackColor = ThemeColors.DarkBackground;
                    combo.ForeColor = ThemeColors.TextColor;
                    combo.FlatStyle = FlatStyle.Flat;
                }
                else if (control is ProgressBar prog)
                {
                    prog.BackColor = ThemeColors.DarkBackground;
                    prog.ForeColor = ThemeColors.AccentColor;
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }


        private void btnDelete_Click(object sender, EventArgs e)
        {
            ListBox activeList = tabControl.SelectedIndex == 0 ? lstFiles : lstXNBFiles;

            if (activeList.SelectedItems.Count > 0)
            {
                var itemsToRemove = activeList.SelectedItems.Cast<string>().ToList();
                foreach (var item in itemsToRemove)
                {
                    activeList.Items.Remove(item);
                }
                UpdateButtonStates();
            }
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            ListBox activeList = tabControl.SelectedIndex == 0 ? lstFiles : lstXNBFiles;

            if (activeList.SelectedItem != null)
            {
                string currentPath = activeList.SelectedItem.ToString();
                string currentFileName = Path.GetFileName(currentPath);

                using (var renameForm = new RenameForm(currentFileName))
                {
                    if (renameForm.ShowDialog() == DialogResult.OK)
                    {
                        string newFileName = renameForm.NewFileName;
                        string directory = Path.GetDirectoryName(currentPath);
                        string newPath = Path.Combine(directory, newFileName);

                        int index = activeList.SelectedIndex;
                        activeList.Items[index] = newPath;
                    }
                }
            }
        }



        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }


        private void UpdateButtonStates()
        {
            bool hasFiles = lstFiles.Items.Count > 0;
            bool hasSelection = lstFiles.SelectedItems.Count > 0;
            bool hasSingleSelection = lstFiles.SelectedItems.Count == 1;

            btnDelete.Enabled = hasSelection;
            btnRename.Enabled = hasSingleSelection;
            btnConvertToXNB.Enabled = hasFiles && !string.IsNullOrEmpty(_outputDirectoryPathToXNB);
        }



        private void btnBrowseFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Supported Files|*.png;*.jpg;*.jpeg;*.bmp;*.wav;*.mp3;*.spritefont|" +
                                       "Image Files|*.png;*.jpg;*.jpeg;*.bmp|" +
                                       "Audio Files|*.wav;*.mp3|" +
                                       "Font Files|*.spritefont|" +
                                       "All Files|*.*";
                openFileDialog.Title = "Select Files to Convert";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {
                        if (!lstFiles.Items.Contains(file))
                        {
                            lstFiles.Items.Add(file);
                        }
                    }
                    UpdateButtonStates(); // Make sure this method exists to enable/disable buttons
                }
            }
        }

        private void btnBrowseXNBFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "XNB files (*.xnb)|*.xnb";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    lstXNBFiles.Items.Clear();
                    lstXNBFiles.Items.AddRange(openFileDialog.FileNames);
                    for (int i = 0; i < lstXNBFiles.Items.Count; i++)
                    {
                        lstXNBFiles.SetSelected(i, true);
                    }
                    UpdateConvertFromXNBButton();
                }
            }
        }

        private void btnBrowseOutputToXNB_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select output folder for XNB files";
                folderDialog.SelectedPath = _outputDirectoryPathToXNB; // Use current path as default

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _outputDirectoryPathToXNB = folderDialog.SelectedPath;
                    txtOutputDirectoryToXNB.Text = _outputDirectoryPathToXNB;
                    UpdateButtonStates();
                }
            }
        }

        private void btnBrowseOutputFromXNB_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select output folder for decompressed files";
                folderDialog.SelectedPath = _outputDirectoryPathFromXNB; // Use current path as default

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _outputDirectoryPathFromXNB = folderDialog.SelectedPath;
                    txtOutputDirectoryFromXNB.Text = _outputDirectoryPathFromXNB;
                    UpdateButtonStates();
                }
            }
        }

        private void UpdateConvertToXNBButton()
        {
            btnConvertToXNB.Enabled = lstFiles.Items.Count > 0 &&
                                     !string.IsNullOrEmpty(_outputDirectoryPathToXNB);
        }

        private void UpdateConvertFromXNBButton()
        {
            btnConvertFromXNB.Enabled = lstXNBFiles.Items.Count > 0 &&
                                       comboBoxFormats.SelectedIndex >= 0 &&
                                       !string.IsNullOrEmpty(_outputDirectoryPathFromXNB);
        }

        private void btnConvertToXNB_Click(object sender, EventArgs e)
        {
            if (lstFiles.Items.Count == 0)
            {
                MessageBox.Show("Please select files to convert.");
                return;
            }

            progressBarToXNB.Value = 0;
            progressBarToXNB.Maximum = lstFiles.Items.Count;
            List<string> failedFiles = new List<string>();

            try
            {
                foreach (string filePath in lstFiles.Items)
                {
                    try
                    {
                        ConvertToXNB(filePath);
                        progressBarToXNB.Value++;
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                if (failedFiles.Count > 0)
                {
                    MessageBox.Show($"Failed to convert the following files:\n{string.Join("\n", failedFiles)}");
                }
                else
                {
                    MessageBox.Show("All files converted successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during conversion: {ex.Message}");
            }
            finally
            {
                progressBarToXNB.Value = 0;
            }
        }

        private void btnConvertFromXNB_Click(object sender, EventArgs e)
        {
            if (lstXNBFiles.Items.Count == 0)
            {
                MessageBox.Show("Please select XNB files to convert.");
                return;
            }

            string selectedFormat = comboBoxFormats.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFormat))
            {
                MessageBox.Show("Please select a conversion format.");
                return;
            }

            progressBarFromXNB.Value = 0;
            progressBarFromXNB.Maximum = lstXNBFiles.Items.Count;
            List<string> failedFiles = new List<string>();

            try
            {
                foreach (string filePath in lstXNBFiles.Items)
                {
                    try
                    {
                        ConvertFromXNB(filePath, selectedFormat);
                        progressBarFromXNB.Value++;
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                if (failedFiles.Count > 0)
                {
                    MessageBox.Show($"Failed to convert the following files:\n{string.Join("\n", failedFiles)}");
                }
                else
                {
                    MessageBox.Show("All files converted successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during conversion: {ex.Message}");
            }
            finally
            {
                progressBarFromXNB.Value = 0;
            }
        }


        private void ConvertToXNB(string inputFilePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string outputFilePath = Path.Combine(_outputDirectoryPathToXNB, fileName + ".xnb");
            string extension = Path.GetExtension(inputFilePath).ToLower();

            try
            {
                byte[] contentData;
                string typeReader;

                switch (extension)
                {
                    case ".png":
                    case ".jpg":
                    case ".bmp":
                        using (Bitmap bitmap = new Bitmap(inputFilePath))
                        {
                            contentData = ConvertImageToXNB(bitmap);
                        }
                        typeReader = "Microsoft.Xna.Framework.Content.Texture2DReader";
                        break;

                    case ".wav":
                    case ".mp3":
                        contentData = ConvertAudioToXNB(inputFilePath);
                        typeReader = "Microsoft.Xna.Framework.Content.SoundEffectReader";
                        break;

                    default:
                        contentData = File.ReadAllBytes(inputFilePath);
                        typeReader = "Microsoft.Xna.Framework.Content.ObjectReader";
                        break;
                }

                using (FileStream fs = File.Create(outputFilePath))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write("XNB".ToCharArray());
                    writer.Write((byte)'w');    // Windows platform
                    writer.Write((byte)5);      // XNA 4.0 version
                    writer.Write((byte)0);      // No compression

                    int headerSize = 6;
                    int contentSize = contentData.Length;
                    int metadataSize = typeReader.Length + 5 + 1 + 1; // type reader + version + reader count + shared resource count
                    writer.Write((uint)(headerSize + contentSize + metadataSize));

                    writer.Write((byte)1);  // One type reader
                    writer.Write7BitEncodedInt(typeReader.Length);
                    writer.Write(typeReader.ToCharArray());
                    writer.Write((int)0);   // Version 0

                    writer.Write7BitEncodedInt(0);

                    writer.Write(contentData);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating XNB file: {ex.Message}");
            }
        }


        private byte[] ConvertImageToXNB(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((int)0); // Surface format (Color = 0)

                writer.Write(bitmap.Width);
                writer.Write(bitmap.Height);
                writer.Write((int)1);  // Mipmap count

                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                int imageSize = bmpData.Stride * bitmap.Height;
                byte[] imageData = new byte[imageSize];
                Marshal.Copy(bmpData.Scan0, imageData, 0, imageSize);
                bitmap.UnlockBits(bmpData);

                writer.Write(imageSize);
                writer.Write(imageData);

                return ms.ToArray();
            }
        }

        private byte[] ConvertAudioToXNB(string audioFilePath)
        {
            using (FileStream fs = File.OpenRead(audioFilePath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                string riff = new string(reader.ReadChars(4));
                if (riff != "RIFF")
                    throw new Exception("Invalid WAV file");

                int fileSize = reader.ReadInt32();
                string wave = new string(reader.ReadChars(4));
                if (wave != "WAVE")
                    throw new Exception("Invalid WAV file");

                string fmt = new string(reader.ReadChars(4));
                if (fmt != "fmt ")
                    throw new Exception("Invalid WAV file");

                int formatSize = reader.ReadInt32();
                short format = reader.ReadInt16();
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                string data = new string(reader.ReadChars(4));
                if (data != "data")
                    throw new Exception("Invalid WAV file");

                int dataSize = reader.ReadInt32();
                byte[] audioData = reader.ReadBytes(dataSize);

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write((int)format);
                    writer.Write((int)sampleRate);
                    writer.Write((short)channels);
                    writer.Write((short)blockAlign);
                    writer.Write((int)dataSize);
                    writer.Write(audioData);

                    return ms.ToArray();
                }
            }
        }







        private void DumpFileContents(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var reader = new BinaryReader(stream))
                {
                    byte[] contents = reader.ReadBytes((int)stream.Length);
                    StringBuilder hex = new StringBuilder();
                    StringBuilder ascii = new StringBuilder();

                    for (int i = 0; i < Math.Min(contents.Length, 256); i++)
                    {
                        if (i % 16 == 0)
                        {
                            if (i > 0)
                            {
                                Debug.WriteLine($"{hex.ToString()} {ascii.ToString()}");
                                hex.Clear();
                                ascii.Clear();
                            }
                            hex.Append($"{i:X4}: ");
                        }

                        hex.Append($"{contents[i]:X2} ");
                        ascii.Append(contents[i] >= 32 && contents[i] <= 126 ? (char)contents[i] : '.');
                    }

                    if (hex.Length > 0)
                    {
                        Debug.WriteLine($"{hex.ToString().PadRight(50)} {ascii.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error dumping file contents: {ex.Message}");
            }
        }


        private void InitializeButtons()
        {
            btnRename = new Button
            {
                Text = "Rename",
                Location = new Point(120, 210),
                Size = new Size(90, 30),
                Enabled = false
            };
            btnRename.Click += BtnRename_Click;

            btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(220, 210),
                Size = new Size(90, 30),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(btnRename);
            this.Controls.Add(btnDelete);
        }

        private void BtnRename_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null) return;

            string oldPath = lstFiles.SelectedItem.ToString();
            string oldFileName = Path.GetFileName(oldPath);

            using (var renameDialog = new Form())
            {
                renameDialog.Text = "Rename File";
                renameDialog.Size = new Size(300, 150);
                renameDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                renameDialog.StartPosition = FormStartPosition.CenterParent;
                renameDialog.MaximizeBox = false;
                renameDialog.MinimizeBox = false;

                var textBox = new TextBox
                {
                    Text = oldFileName,
                    Location = new Point(10, 20),
                    Size = new Size(260, 20)
                };
                renameDialog.Controls.Add(textBox);

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 60)
                };
                renameDialog.Controls.Add(okButton);

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(180, 60)
                };
                renameDialog.Controls.Add(cancelButton);

                renameDialog.AcceptButton = okButton;
                renameDialog.CancelButton = cancelButton;

                if (renameDialog.ShowDialog() == DialogResult.OK)
                {
                    string newFileName = textBox.Text.Trim();
                    if (string.IsNullOrEmpty(newFileName))
                    {
                        MessageBox.Show("Filename cannot be empty.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newFileName);
                        File.Move(oldPath, newPath);

                        int index = lstFiles.SelectedIndex;
                        lstFiles.Items[index] = newPath;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming file: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count == 0) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete {lstFiles.SelectedItems.Count} file(s)?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var itemsToRemove = new List<object>();
                foreach (var item in lstFiles.SelectedItems)
                {
                    try
                    {
                        string filePath = item.ToString();
                        File.Delete(filePath);
                        itemsToRemove.Add(item);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting {item}: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                foreach (var item in itemsToRemove)
                {
                    lstFiles.Items.Remove(item);
                }
            }
        }



        // Add this method to validate and fix paths
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty");

            // Convert to absolute path if relative
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(path);

            // Clean up the path
            path = Path.GetFullPath(path);

            return path;
        }




        private void ReadTextureContent(BinaryReader reader, string outputPath)
        {
            int format = reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int mipCount = reader.ReadInt32();

            int dataSize = reader.ReadInt32();
            byte[] textureData = reader.ReadBytes(dataSize);

            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    byte[] pixelData = new byte[width * height * 4];

                    // Convert from XNA format to BGRA
                    for (int i = 0; i < dataSize; i += 4)
                    {
                        if (i + 3 < dataSize)
                        {
                            pixelData[i] = textureData[i + 2];     // B
                            pixelData[i + 1] = textureData[i + 1]; // G
                            pixelData[i + 2] = textureData[i];     // R
                            pixelData[i + 3] = textureData[i + 3]; // A
                        }
                    }

                    Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                // Save in requested format
                ImageFormat imageFormat = ImageFormat.Png;  // Renamed from 'format' to 'imageFormat'
                if (outputPath.EndsWith(".jpg"))
                    imageFormat = ImageFormat.Jpeg;
                else if (outputPath.EndsWith(".bmp"))
                    imageFormat = ImageFormat.Bmp;

                bitmap.Save(outputPath, imageFormat);
            }
        }


        private void ReadAudioContent(BinaryReader reader, string outputPath)
        {
            int format = reader.ReadInt32();
            int sampleRate = reader.ReadInt32();
            short channels = reader.ReadInt16();
            short blockAlign = reader.ReadInt16();

            int dataSize = reader.ReadInt32();
            byte[] audioData = reader.ReadBytes(dataSize);

            using (FileStream fs = File.Create(outputPath))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Write WAV header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);                           // Format chunk size
                writer.Write((short)1);                     // Audio format (PCM)
                writer.Write(channels);                     // Channels
                writer.Write(sampleRate);                   // Sample rate
                writer.Write(sampleRate * channels * 2);    // Byte rate
                writer.Write(blockAlign);                   // Block align
                writer.Write((short)16);                    // Bits per sample
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                writer.Write(audioData);
            }
        }




        private void ConvertTexture2D(byte[] data, string outputPath)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                // Read texture format (SurfaceFormat enum in XNA)
                int format = reader.ReadInt32();

                // Read dimensions
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int mipCount = reader.ReadInt32();

                // Read actual image data
                int dataSize = reader.ReadInt32();
                byte[] imageData = reader.ReadBytes(dataSize);

                // Convert to standard image format
                using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    BitmapData bmpData = bitmap.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppArgb);

                    // Convert the pixel data based on the format
                    ConvertPixelData(imageData, format, bmpData);

                    bitmap.UnlockBits(bmpData);

                    // Save in the requested format
                    if (outputPath.EndsWith(".png"))
                        bitmap.Save(outputPath, ImageFormat.Png);
                    else if (outputPath.EndsWith(".jpg"))
                        bitmap.Save(outputPath, ImageFormat.Jpeg);
                    else if (outputPath.EndsWith(".bmp"))
                        bitmap.Save(outputPath, ImageFormat.Bmp);
                }
            }
        }

        private void ConvertSoundEffect(byte[] data, string outputPath)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                // Read WAV format
                int format = reader.ReadInt32();
                int sampleRate = reader.ReadInt32();
                int channels = reader.ReadInt16();
                int blockAlign = reader.ReadInt16();

                // Read audio data
                int dataSize = reader.ReadInt32();
                byte[] audioData = reader.ReadBytes(dataSize);

                // Create WAV file
                using (FileStream fs = File.Create(outputPath))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    // Write WAV header
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + dataSize);
                    writer.Write("WAVE".ToCharArray());
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((short)1);  // PCM
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * channels * 2);  // bytes per second
                    writer.Write((short)(channels * 2));      // block align
                    writer.Write((short)16);                  // bits per sample
                    writer.Write("data".ToCharArray());
                    writer.Write(dataSize);
                    writer.Write(audioData);
                }
            }
        }

        private void ConvertPixelData(byte[] sourceData, int format, BitmapData targetData)
        {
            unsafe
            {
                byte* targetPtr = (byte*)targetData.Scan0;
                fixed (byte* sourcePtr = sourceData)
                {
                    switch (format)
                    {
                        case 0: // Color
                            for (int i = 0; i < sourceData.Length; i += 4)
                            {
                                targetPtr[i + 3] = sourcePtr[i + 3]; // Alpha
                                targetPtr[i + 2] = sourcePtr[i + 2]; // Red
                                targetPtr[i + 1] = sourcePtr[i + 1]; // Green
                                targetPtr[i] = sourcePtr[i];         // Blue
                            }
                            break;

                        case 1: // BGR565
                                // Add BGR565 conversion
                            break;

                        case 2: // BGRA4444
                                // Add BGRA4444 conversion
                            break;

                            // Add other format conversions as needed
                    }
                }
            }
        }


 



        private string ReadString(BinaryReader reader)
        {
            int length = Read7BitEncodedInt(reader);
            return new string(reader.ReadChars(length));
        }



        private byte[] DecompressLZ4(byte[] compressedData, int decompressedSize)
        {
            byte[] decompressedData = new byte[decompressedSize];
            int sourcePos = 0;
            int destPos = 0;

            while (sourcePos < compressedData.Length && destPos < decompressedSize)
            {
                byte token = compressedData[sourcePos++];

                int literalLength = token >> 4;
                if (literalLength == 15)
                {
                    byte lengthByte;
                    do
                    {
                        lengthByte = compressedData[sourcePos++];
                        literalLength += lengthByte;
                    } while (lengthByte == 255 && sourcePos < compressedData.Length);
                }

                if (literalLength > 0)
                {
                    Buffer.BlockCopy(compressedData, sourcePos, decompressedData, destPos, Math.Min(literalLength, decompressedSize - destPos));
                    sourcePos += literalLength;
                    destPos += literalLength;
                }

                if (sourcePos >= compressedData.Length || destPos >= decompressedSize)
                    break;

                int offset = compressedData[sourcePos++] | (compressedData[sourcePos++] << 8);

                int matchLength = token & 0xF;
                if (matchLength == 15)
                {
                    byte lengthByte;
                    do
                    {
                        lengthByte = compressedData[sourcePos++];
                        matchLength += lengthByte;
                    } while (lengthByte == 255 && sourcePos < compressedData.Length);
                }
                matchLength += 4;

                int matchPos = destPos - offset;
                while (matchLength-- > 0 && destPos < decompressedSize)
                {
                    decompressedData[destPos++] = decompressedData[matchPos++];
                }
            }

            return decompressedData;
        }


        private string GetTypeReader(string extension)
        {
            switch (extension.ToLower())
            {
                case ".png":
                case ".jpg":
                case ".bmp":
                    return "Microsoft.Xna.Framework.Content.Texture2DReader";
                case ".wav":
                case ".mp3":
                    return "Microsoft.Xna.Framework.Content.SoundEffectReader";
                case ".spritefont":
                    return "Microsoft.Xna.Framework.Content.SpriteFontReader";
                default:
                    return "Microsoft.Xna.Framework.Content.ObjectReader";
            }
        }

        private void DebugXNBFile(string filePath)
        {
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    string debugInfo = $"File: {Path.GetFileName(filePath)}\n";
                    debugInfo += $"Size: {fs.Length} bytes\n";
                    debugInfo += $"Header: {new string(reader.ReadChars(3))}\n";
                    debugInfo += $"Platform: {reader.ReadByte()}\n";
                    debugInfo += $"Version: {reader.ReadByte()}\n";
                    debugInfo += $"Flags: {reader.ReadByte()}\n";
                    debugInfo += $"FileSize: {reader.ReadUInt32()}\n";

                    MessageBox.Show(debugInfo, "XNB Debug Info");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Debug Error: {ex.Message}");
            }
        }
    }





    }
