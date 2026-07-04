namespace AudioConverter
{
    partial class Form1
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tabControl = new TabControl();
            tabPageAllFiles = new TabPage();
            dataGridView1 = new DataGridView();
            InputFiles = new DataGridViewTextBoxColumn();
            tabPageFailedFiles = new TabPage();
            dataGridViewFailed = new DataGridView();
            FailedFilePath = new DataGridViewTextBoxColumn();
            FailedErrorMessage = new DataGridViewTextBoxColumn();
            buttonConvert = new Button();
            textBoxOutput = new TextBox();
            labelOutput = new Label();
            numericUpDownBitrate = new NumericUpDown();
            labelBitrate = new Label();
            update = new Button();
            labelNumFiles = new Label();
            labelCompleted = new Label();
            numFiles = new Label();
            completedFiles = new Label();
            numericUpDownThreads = new NumericUpDown();
            labelThreads = new Label();
            cancelButton = new Button();
            buttonOpen = new Button();
            labelDoubleClick = new Label();
            splitContainer1 = new SplitContainer();
            labelElapsed = new Label();
            labelSaved = new Label();
            afterSizeLabel = new Label();
            labelAfter = new Label();
            mbPerSecLabel = new Label();
            percentSavedLabel = new Label();
            elapsedLabel = new Label();
            filesPerSecLabel = new Label();
            beforeSizeLabel = new Label();
            labelBefore = new Label();
            labelFormat = new Label();
            comboBoxFormat = new ComboBox();
            comboBoxSampleRate = new ComboBox();
            labelSampleRate = new Label();
            checkBoxOutputFolder = new CheckBox();
            checkBoxUseSourceSampleRate = new CheckBox();
            comboBoxBitDepth = new ComboBox();
            labelBitDepth = new Label();
            labelMaxBitrate = new Label();
            labelChannelMode = new Label();
            comboBoxChannelMode = new ComboBox();
            buttonAbout = new Button();
            clearListButton = new Button();
            processingProgress = new ProgressBar();
            tabControl.SuspendLayout();
            tabPageAllFiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabPageFailedFiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewFailed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownBitrate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabPageAllFiles);
            tabControl.Controls.Add(tabPageFailedFiles);
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(559, 490);
            tabControl.TabIndex = 0;
            // 
            // tabPageAllFiles
            // 
            tabPageAllFiles.Controls.Add(dataGridView1);
            tabPageAllFiles.Location = new Point(4, 24);
            tabPageAllFiles.Name = "tabPageAllFiles";
            tabPageAllFiles.Padding = new Padding(3);
            tabPageAllFiles.Size = new Size(551, 462);
            tabPageAllFiles.TabIndex = 0;
            tabPageAllFiles.Text = "All Files";
            tabPageAllFiles.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowDrop = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { InputFiles });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 3);
            dataGridView1.Margin = new Padding(0);
            dataGridView1.MinimumSize = new Size(65, 48);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 25;
            dataGridView1.Size = new Size(545, 456);
            dataGridView1.TabIndex = 0;
            dataGridView1.DragDrop += dataGridView1_DragDrop;
            dataGridView1.DragEnter += dataGridView1_DragEnter;
            // 
            // InputFiles
            // 
            InputFiles.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            InputFiles.DataPropertyName = "InputFiles";
            InputFiles.HeaderText = "InputFiles               (Drag and Drop)";
            InputFiles.Name = "InputFiles";
            // 
            // tabPageFailedFiles
            // 
            tabPageFailedFiles.Controls.Add(dataGridViewFailed);
            tabPageFailedFiles.Location = new Point(4, 24);
            tabPageFailedFiles.Name = "tabPageFailedFiles";
            tabPageFailedFiles.Padding = new Padding(3);
            tabPageFailedFiles.Size = new Size(552, 462);
            tabPageFailedFiles.TabIndex = 1;
            tabPageFailedFiles.Text = "Failed Files";
            tabPageFailedFiles.UseVisualStyleBackColor = true;
            // 
            // dataGridViewFailed
            // 
            dataGridViewFailed.AllowUserToAddRows = false;
            dataGridViewFailed.AllowUserToDeleteRows = false;
            dataGridViewFailed.AllowUserToOrderColumns = true;
            dataGridViewFailed.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewFailed.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewFailed.Columns.AddRange(new DataGridViewColumn[] { FailedFilePath, FailedErrorMessage });
            dataGridViewFailed.Dock = DockStyle.Fill;
            dataGridViewFailed.Location = new Point(3, 3);
            dataGridViewFailed.Margin = new Padding(0);
            dataGridViewFailed.Name = "dataGridViewFailed";
            dataGridViewFailed.ReadOnly = true;
            dataGridViewFailed.RowHeadersWidth = 25;
            dataGridViewFailed.Size = new Size(546, 456);
            dataGridViewFailed.TabIndex = 1;
            dataGridViewFailed.DoubleClick += dataGridViewFailed_DoubleClick;
            // 
            // FailedFilePath
            // 
            FailedFilePath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FailedFilePath.DataPropertyName = "FilePath";
            FailedFilePath.HeaderText = "File Path";
            FailedFilePath.Name = "FailedFilePath";
            FailedFilePath.ReadOnly = true;
            // 
            // FailedErrorMessage
            // 
            FailedErrorMessage.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FailedErrorMessage.DataPropertyName = "ErrorMessage";
            FailedErrorMessage.HeaderText = "Error Message";
            FailedErrorMessage.Name = "FailedErrorMessage";
            FailedErrorMessage.ReadOnly = true;
            // 
            // buttonConvert
            // 
            buttonConvert.Anchor = AnchorStyles.Bottom;
            buttonConvert.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonConvert.Font = new Font("Arial", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonConvert.Location = new Point(62, 384);
            buttonConvert.Margin = new Padding(4, 3, 4, 3);
            buttonConvert.MaximumSize = new Size(1120, 588);
            buttonConvert.MinimumSize = new Size(112, 59);
            buttonConvert.Name = "buttonConvert";
            buttonConvert.Size = new Size(199, 80);
            buttonConvert.TabIndex = 1;
            buttonConvert.Text = "Convert";
            buttonConvert.UseVisualStyleBackColor = true;
            buttonConvert.Click += buttonConvert_Click;
            // 
            // textBoxOutput
            // 
            textBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOutput.Location = new Point(6, 22);
            textBoxOutput.Margin = new Padding(4, 3, 4, 3);
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.Size = new Size(190, 23);
            textBoxOutput.TabIndex = 2;
            textBoxOutput.DoubleClick += textBoxOutput_DoubleClick;
            // 
            // labelOutput
            // 
            labelOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelOutput.AutoSize = true;
            labelOutput.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelOutput.Location = new Point(4, 0);
            labelOutput.Margin = new Padding(4, 0, 4, 0);
            labelOutput.Name = "labelOutput";
            labelOutput.Size = new Size(90, 16);
            labelOutput.TabIndex = 3;
            labelOutput.Text = "Output Path:";
            // 
            // numericUpDownBitrate
            // 
            numericUpDownBitrate.Anchor = AnchorStyles.Bottom;
            numericUpDownBitrate.Location = new Point(80, 248);
            numericUpDownBitrate.Margin = new Padding(4, 3, 4, 3);
            numericUpDownBitrate.Maximum = new decimal(new int[] { 320, 0, 0, 0 });
            numericUpDownBitrate.MaximumSize = new Size(59, 0);
            numericUpDownBitrate.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numericUpDownBitrate.MinimumSize = new Size(59, 0);
            numericUpDownBitrate.Name = "numericUpDownBitrate";
            numericUpDownBitrate.Size = new Size(59, 23);
            numericUpDownBitrate.TabIndex = 4;
            numericUpDownBitrate.Value = new decimal(new int[] { 192, 0, 0, 0 });
            // 
            // labelBitrate
            // 
            labelBitrate.Anchor = AnchorStyles.Bottom;
            labelBitrate.AutoSize = true;
            labelBitrate.Font = new Font("Segoe UI", 9F);
            labelBitrate.Location = new Point(6, 250);
            labelBitrate.Margin = new Padding(4, 0, 4, 0);
            labelBitrate.Name = "labelBitrate";
            labelBitrate.Size = new Size(44, 15);
            labelBitrate.TabIndex = 5;
            labelBitrate.Text = "Bitrate:";
            // 
            // update
            // 
            update.Anchor = AnchorStyles.Bottom;
            update.Enabled = false;
            update.Location = new Point(4, 437);
            update.Margin = new Padding(4, 3, 4, 3);
            update.Name = "update";
            update.Size = new Size(58, 27);
            update.TabIndex = 6;
            update.Text = "Update";
            update.UseVisualStyleBackColor = true;
            // 
            // labelNumFiles
            // 
            labelNumFiles.Anchor = AnchorStyles.Top;
            labelNumFiles.AutoSize = true;
            labelNumFiles.Location = new Point(4, 48);
            labelNumFiles.Margin = new Padding(4, 0, 4, 0);
            labelNumFiles.Name = "labelNumFiles";
            labelNumFiles.Size = new Size(94, 15);
            labelNumFiles.TabIndex = 7;
            labelNumFiles.Text = "Number of Files:";
            // 
            // labelCompleted
            // 
            labelCompleted.Anchor = AnchorStyles.Top;
            labelCompleted.AutoSize = true;
            labelCompleted.Location = new Point(4, 63);
            labelCompleted.Margin = new Padding(4, 0, 4, 0);
            labelCompleted.Name = "labelCompleted";
            labelCompleted.Size = new Size(95, 15);
            labelCompleted.TabIndex = 8;
            labelCompleted.Text = "Completed Files:";
            // 
            // numFiles
            // 
            numFiles.Anchor = AnchorStyles.Top;
            numFiles.AutoSize = true;
            numFiles.Location = new Point(101, 48);
            numFiles.Margin = new Padding(4, 0, 4, 0);
            numFiles.Name = "numFiles";
            numFiles.Size = new Size(13, 15);
            numFiles.TabIndex = 9;
            numFiles.Text = "0";
            // 
            // completedFiles
            // 
            completedFiles.Anchor = AnchorStyles.Top;
            completedFiles.AutoSize = true;
            completedFiles.Location = new Point(101, 63);
            completedFiles.Margin = new Padding(4, 0, 4, 0);
            completedFiles.Name = "completedFiles";
            completedFiles.Size = new Size(13, 15);
            completedFiles.TabIndex = 10;
            completedFiles.Text = "0";
            // 
            // numericUpDownThreads
            // 
            numericUpDownThreads.Anchor = AnchorStyles.Bottom;
            numericUpDownThreads.Location = new Point(80, 306);
            numericUpDownThreads.Margin = new Padding(4, 3, 4, 3);
            numericUpDownThreads.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            numericUpDownThreads.MaximumSize = new Size(59, 0);
            numericUpDownThreads.MinimumSize = new Size(59, 0);
            numericUpDownThreads.Name = "numericUpDownThreads";
            numericUpDownThreads.Size = new Size(59, 23);
            numericUpDownThreads.TabIndex = 11;
            numericUpDownThreads.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // labelThreads
            // 
            labelThreads.Anchor = AnchorStyles.Bottom;
            labelThreads.AutoSize = true;
            labelThreads.Font = new Font("Segoe UI", 9F);
            labelThreads.Location = new Point(6, 308);
            labelThreads.Margin = new Padding(4, 0, 4, 0);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(49, 15);
            labelThreads.TabIndex = 12;
            labelThreads.Text = "Threads";
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom;
            cancelButton.Location = new Point(4, 384);
            cancelButton.Margin = new Padding(4, 3, 4, 3);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(58, 47);
            cancelButton.TabIndex = 13;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            // 
            // buttonOpen
            // 
            buttonOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpen.Location = new Point(197, 21);
            buttonOpen.Margin = new Padding(4, 3, 4, 3);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(64, 26);
            buttonOpen.TabIndex = 15;
            buttonOpen.Text = "Open";
            buttonOpen.UseVisualStyleBackColor = true;
            buttonOpen.Click += buttonOpen_Click;
            // 
            // labelDoubleClick
            // 
            labelDoubleClick.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelDoubleClick.AutoSize = true;
            labelDoubleClick.Font = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelDoubleClick.Location = new Point(114, 3);
            labelDoubleClick.Margin = new Padding(4, 0, 4, 0);
            labelDoubleClick.MaximumSize = new Size(118, 14);
            labelDoubleClick.MinimumSize = new Size(118, 14);
            labelDoubleClick.Name = "labelDoubleClick";
            labelDoubleClick.Size = new Size(118, 14);
            labelDoubleClick.TabIndex = 15;
            labelDoubleClick.Text = "Double Click to Change";
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(14, 14);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tabControl);
            splitContainer1.Panel1MinSize = 400;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(labelElapsed);
            splitContainer1.Panel2.Controls.Add(labelSaved);
            splitContainer1.Panel2.Controls.Add(afterSizeLabel);
            splitContainer1.Panel2.Controls.Add(labelAfter);
            splitContainer1.Panel2.Controls.Add(mbPerSecLabel);
            splitContainer1.Panel2.Controls.Add(percentSavedLabel);
            splitContainer1.Panel2.Controls.Add(elapsedLabel);
            splitContainer1.Panel2.Controls.Add(filesPerSecLabel);
            splitContainer1.Panel2.Controls.Add(beforeSizeLabel);
            splitContainer1.Panel2.Controls.Add(labelBefore);
            splitContainer1.Panel2.Controls.Add(labelFormat);
            splitContainer1.Panel2.Controls.Add(comboBoxFormat);
            splitContainer1.Panel2.Controls.Add(comboBoxSampleRate);
            splitContainer1.Panel2.Controls.Add(labelSampleRate);
            splitContainer1.Panel2.Controls.Add(checkBoxOutputFolder);
            splitContainer1.Panel2.Controls.Add(checkBoxUseSourceSampleRate);
            splitContainer1.Panel2.Controls.Add(comboBoxBitDepth);
            splitContainer1.Panel2.Controls.Add(labelBitDepth);
            splitContainer1.Panel2.Controls.Add(labelChannelMode);
            splitContainer1.Panel2.Controls.Add(comboBoxChannelMode);
            splitContainer1.Panel2.Controls.Add(labelMaxBitrate);
            splitContainer1.Panel2.Controls.Add(buttonAbout);
            splitContainer1.Panel2.Controls.Add(clearListButton);
            splitContainer1.Panel2.Controls.Add(buttonOpen);
            splitContainer1.Panel2.Controls.Add(labelDoubleClick);
            splitContainer1.Panel2.Controls.Add(numericUpDownBitrate);
            splitContainer1.Panel2.Controls.Add(labelBitrate);
            splitContainer1.Panel2.Controls.Add(cancelButton);
            splitContainer1.Panel2.Controls.Add(textBoxOutput);
            splitContainer1.Panel2.Controls.Add(buttonConvert);
            splitContainer1.Panel2.Controls.Add(completedFiles);
            splitContainer1.Panel2.Controls.Add(labelOutput);
            splitContainer1.Panel2.Controls.Add(labelCompleted);
            splitContainer1.Panel2.Controls.Add(numFiles);
            splitContainer1.Panel2.Controls.Add(update);
            splitContainer1.Panel2.Controls.Add(labelNumFiles);
            splitContainer1.Panel2.Controls.Add(numericUpDownThreads);
            splitContainer1.Panel2.Controls.Add(labelThreads);
            splitContainer1.Panel2MinSize = 100;
            splitContainer1.Size = new Size(836, 493);
            splitContainer1.SplitterDistance = 559;
            splitContainer1.SplitterIncrement = 4;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 16;
            // 
            // labelElapsed
            // 
            labelElapsed.Anchor = AnchorStyles.Top;
            labelElapsed.AutoSize = true;
            labelElapsed.Location = new Point(4, 123);
            labelElapsed.Name = "labelElapsed";
            labelElapsed.Size = new Size(77, 15);
            labelElapsed.TabIndex = 31;
            labelElapsed.Text = "Elapsed Time";
            // 
            // labelSaved
            // 
            labelSaved.Anchor = AnchorStyles.Top;
            labelSaved.AutoSize = true;
            labelSaved.Location = new Point(4, 108);
            labelSaved.Name = "labelSaved";
            labelSaved.Size = new Size(41, 15);
            labelSaved.TabIndex = 30;
            labelSaved.Text = "Saved:";
            // 
            // afterSizeLabel
            // 
            afterSizeLabel.Anchor = AnchorStyles.Top;
            afterSizeLabel.AutoSize = true;
            afterSizeLabel.Location = new Point(101, 93);
            afterSizeLabel.Margin = new Padding(4, 0, 4, 0);
            afterSizeLabel.Name = "afterSizeLabel";
            afterSizeLabel.Size = new Size(13, 15);
            afterSizeLabel.TabIndex = 24;
            afterSizeLabel.Text = "0";
            // 
            // labelAfter
            // 
            labelAfter.Anchor = AnchorStyles.Top;
            labelAfter.AutoSize = true;
            labelAfter.Location = new Point(4, 93);
            labelAfter.Margin = new Padding(4, 0, 4, 0);
            labelAfter.Name = "labelAfter";
            labelAfter.Size = new Size(59, 15);
            labelAfter.TabIndex = 23;
            labelAfter.Text = "After Size:";
            // 
            // mbPerSecLabel
            // 
            mbPerSecLabel.Anchor = AnchorStyles.Top;
            mbPerSecLabel.AutoSize = true;
            mbPerSecLabel.Location = new Point(101, 153);
            mbPerSecLabel.Name = "mbPerSecLabel";
            mbPerSecLabel.Size = new Size(59, 15);
            mbPerSecLabel.TabIndex = 26;
            mbPerSecLabel.Text = "0.00 MB/s";
            // 
            // percentSavedLabel
            // 
            percentSavedLabel.Anchor = AnchorStyles.Top;
            percentSavedLabel.AutoSize = true;
            percentSavedLabel.Location = new Point(101, 108);
            percentSavedLabel.Name = "percentSavedLabel";
            percentSavedLabel.Size = new Size(38, 15);
            percentSavedLabel.TabIndex = 27;
            percentSavedLabel.Text = "0.00%";
            // 
            // elapsedLabel
            // 
            elapsedLabel.Anchor = AnchorStyles.Top;
            elapsedLabel.AutoSize = true;
            elapsedLabel.Location = new Point(101, 123);
            elapsedLabel.Name = "elapsedLabel";
            elapsedLabel.Size = new Size(49, 15);
            elapsedLabel.TabIndex = 28;
            elapsedLabel.Text = "00:00:00";
            // 
            // filesPerSecLabel
            // 
            filesPerSecLabel.Anchor = AnchorStyles.Top;
            filesPerSecLabel.AutoSize = true;
            filesPerSecLabel.Location = new Point(101, 138);
            filesPerSecLabel.Name = "filesPerSecLabel";
            filesPerSecLabel.Size = new Size(62, 15);
            filesPerSecLabel.TabIndex = 29;
            filesPerSecLabel.Text = "0.00 files/s";
            // 
            // beforeSizeLabel
            // 
            beforeSizeLabel.Anchor = AnchorStyles.Top;
            beforeSizeLabel.AutoSize = true;
            beforeSizeLabel.Location = new Point(101, 78);
            beforeSizeLabel.Margin = new Padding(4, 0, 4, 0);
            beforeSizeLabel.Name = "beforeSizeLabel";
            beforeSizeLabel.Size = new Size(13, 15);
            beforeSizeLabel.TabIndex = 22;
            beforeSizeLabel.Text = "0";
            // 
            // labelBefore
            // 
            labelBefore.Anchor = AnchorStyles.Top;
            labelBefore.AutoSize = true;
            labelBefore.Location = new Point(4, 78);
            labelBefore.Margin = new Padding(4, 0, 4, 0);
            labelBefore.Name = "labelBefore";
            labelBefore.Size = new Size(67, 15);
            labelBefore.TabIndex = 21;
            labelBefore.Text = "Before Size:";
            // 
            // labelFormat
            // 
            labelFormat.Anchor = AnchorStyles.Bottom;
            labelFormat.AutoSize = true;
            labelFormat.Font = new Font("Segoe UI", 9F);
            labelFormat.Location = new Point(6, 222);
            labelFormat.Margin = new Padding(4, 0, 4, 0);
            labelFormat.Name = "labelFormat";
            labelFormat.Size = new Size(48, 15);
            labelFormat.TabIndex = 20;
            labelFormat.Text = "Format:";
            // 
            // comboBoxFormat
            // 
            comboBoxFormat.Anchor = AnchorStyles.Bottom;
            comboBoxFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFormat.FormattingEnabled = true;
            comboBoxFormat.Items.AddRange(new object[] { "mp3", "aac", "flac", "wav", "ogg", "opus", "m4a" });
            comboBoxFormat.Location = new Point(80, 219);
            comboBoxFormat.Margin = new Padding(4, 3, 4, 3);
            comboBoxFormat.Name = "comboBoxFormat";
            comboBoxFormat.Size = new Size(59, 23);
            comboBoxFormat.TabIndex = 19;
            // 
            // comboBoxSampleRate
            // 
            comboBoxSampleRate.Anchor = AnchorStyles.Bottom;
            comboBoxSampleRate.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSampleRate.FormattingEnabled = true;
            comboBoxSampleRate.Items.AddRange(new object[] { "8000", "16000", "22050", "44100", "48000", "96000", "192000" });
            comboBoxSampleRate.Location = new Point(80, 277);
            comboBoxSampleRate.Margin = new Padding(4, 3, 4, 3);
            comboBoxSampleRate.Name = "comboBoxSampleRate";
            comboBoxSampleRate.Size = new Size(59, 23);
            comboBoxSampleRate.TabIndex = 17;
            // 
            // labelSampleRate
            // 
            labelSampleRate.Anchor = AnchorStyles.Bottom;
            labelSampleRate.AutoSize = true;
            labelSampleRate.Font = new Font("Segoe UI", 9F);
            labelSampleRate.Location = new Point(6, 279);
            labelSampleRate.Margin = new Padding(4, 0, 4, 0);
            labelSampleRate.Name = "labelSampleRate";
            labelSampleRate.Size = new Size(75, 15);
            labelSampleRate.TabIndex = 18;
            labelSampleRate.Text = "Sample Rate:";
            // 
            // checkBoxOutputFolder
            // 
            checkBoxOutputFolder.Anchor = AnchorStyles.Bottom;
            checkBoxOutputFolder.AutoSize = true;
            checkBoxOutputFolder.Location = new Point(4, 183);
            checkBoxOutputFolder.Margin = new Padding(4, 3, 4, 3);
            checkBoxOutputFolder.Name = "checkBoxOutputFolder";
            checkBoxOutputFolder.Size = new Size(173, 19);
            checkBoxOutputFolder.TabIndex = 16;
            checkBoxOutputFolder.Text = "Output Full Folder Structure";
            checkBoxOutputFolder.UseVisualStyleBackColor = true;
            // 
            // checkBoxUseSourceSampleRate
            // 
            checkBoxUseSourceSampleRate.Anchor = AnchorStyles.Bottom;
            checkBoxUseSourceSampleRate.AutoSize = true;
            checkBoxUseSourceSampleRate.Font = new Font("Segoe UI", 8F);
            checkBoxUseSourceSampleRate.Location = new Point(145, 280);
            checkBoxUseSourceSampleRate.Margin = new Padding(4, 3, 4, 3);
            checkBoxUseSourceSampleRate.Name = "checkBoxUseSourceSampleRate";
            checkBoxUseSourceSampleRate.Size = new Size(68, 17);
            checkBoxUseSourceSampleRate.TabIndex = 32;
            checkBoxUseSourceSampleRate.Text = "Original";
            checkBoxUseSourceSampleRate.UseVisualStyleBackColor = true;
            // 
            // comboBoxBitDepth
            // 
            comboBoxBitDepth.Anchor = AnchorStyles.Bottom;
            comboBoxBitDepth.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxBitDepth.FormattingEnabled = true;
            comboBoxBitDepth.Items.AddRange(new object[] { "8", "16", "24", "32" });
            comboBoxBitDepth.Location = new Point(80, 334);
            comboBoxBitDepth.Margin = new Padding(4, 3, 4, 3);
            comboBoxBitDepth.Name = "comboBoxBitDepth";
            comboBoxBitDepth.Size = new Size(59, 23);
            comboBoxBitDepth.TabIndex = 26;
            // 
            // labelBitDepth
            // 
            labelBitDepth.Anchor = AnchorStyles.Bottom;
            labelBitDepth.AutoSize = true;
            labelBitDepth.Font = new Font("Segoe UI", 9F);
            labelBitDepth.Location = new Point(6, 337);
            labelBitDepth.Margin = new Padding(4, 0, 4, 0);
            labelBitDepth.Name = "labelBitDepth";
            labelBitDepth.Size = new Size(59, 15);
            labelBitDepth.TabIndex = 27;
            labelBitDepth.Text = "Bit Depth:";
            //
            // labelChannelMode
            //
            labelChannelMode.Anchor = AnchorStyles.Bottom;
            labelChannelMode.AutoSize = true;
            labelChannelMode.Font = new Font("Segoe UI", 9F);
            labelChannelMode.Location = new Point(145, 337);
            labelChannelMode.Margin = new Padding(4, 0, 4, 0);
            labelChannelMode.Name = "labelChannelMode";
            labelChannelMode.Size = new Size(58, 15);
            labelChannelMode.TabIndex = 35;
            labelChannelMode.Text = "Channels:";
            //
            // comboBoxChannelMode
            //
            comboBoxChannelMode.Anchor = AnchorStyles.Bottom;
            comboBoxChannelMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxChannelMode.FormattingEnabled = true;
            comboBoxChannelMode.Items.AddRange(new object[] { "Original", "Mono", "Stereo" });
            comboBoxChannelMode.Location = new Point(204, 334);
            comboBoxChannelMode.Margin = new Padding(4, 3, 4, 3);
            comboBoxChannelMode.Name = "comboBoxChannelMode";
            comboBoxChannelMode.Size = new Size(64, 23);
            comboBoxChannelMode.TabIndex = 36;
            //
            // labelMaxBitrate
            //
            labelMaxBitrate.Location = new Point(0, 0);
            labelMaxBitrate.Name = "labelMaxBitrate";
            labelMaxBitrate.Size = new Size(100, 23);
            labelMaxBitrate.TabIndex = 34;
            //
            // buttonAbout
            //
            buttonAbout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonAbout.Location = new Point(197, 81);
            buttonAbout.Margin = new Padding(4, 3, 4, 3);
            buttonAbout.Name = "buttonAbout";
            buttonAbout.Size = new Size(64, 26);
            buttonAbout.TabIndex = 37;
            buttonAbout.Text = "About";
            buttonAbout.UseVisualStyleBackColor = true;
            buttonAbout.Click += buttonAbout_Click;
            //
            // clearListButton
            //
            clearListButton.Anchor = AnchorStyles.Top;
            clearListButton.Location = new Point(197, 51);
            clearListButton.Margin = new Padding(4, 3, 4, 3);
            clearListButton.Name = "clearListButton";
            clearListButton.Size = new Size(64, 26);
            clearListButton.TabIndex = 14;
            clearListButton.Text = "Clear List";
            clearListButton.UseVisualStyleBackColor = true;
            clearListButton.Click += clearListButton_Click;
            // 
            // processingProgress
            // 
            processingProgress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            processingProgress.Location = new Point(0, 484);
            processingProgress.Name = "processingProgress";
            processingProgress.Size = new Size(860, 37);
            processingProgress.Style = ProgressBarStyle.Continuous;
            processingProgress.TabIndex = 25;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(860, 520);
            Controls.Add(processingProgress);
            Controls.Add(splitContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(876, 559);
            Name = "Form1";
            tabControl.ResumeLayout(false);
            tabPageAllFiles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabPageFailedFiles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewFailed).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownBitrate).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button buttonConvert;
        private TextBox textBoxOutput;
        private Label labelOutput;
        public DataGridView dataGridView1;
        private NumericUpDown numericUpDownBitrate;
        private Label labelBitrate;
        private Button update;
        private Label labelNumFiles;
        private Label labelCompleted;
        private Label numFiles;
        private Label completedFiles;
        private NumericUpDown numericUpDownThreads;
        private Label labelThreads;
        private Button cancelButton;
        private Button buttonOpen;
        private Label labelDoubleClick;
        private SplitContainer splitContainer1;
        private CheckBox checkBoxOutputFolder;
        private CheckBox checkBoxUseSourceSampleRate;
        private Label labelElapsed;
        private Label labelSaved;
        private Label afterSizeLabel;
        private Label labelAfter;
        private Label mbPerSecLabel;
        private Label percentSavedLabel;
        private Label elapsedLabel;
        private Label filesPerSecLabel;
        private Label beforeSizeLabel;
        private Label labelBefore;
        private Label labelFormat;
        private ComboBox comboBoxFormat;
        private ComboBox comboBoxSampleRate;
        private Label labelSampleRate;
        private ComboBox comboBoxBitDepth;
        private Label labelBitDepth;
        private Label labelChannelMode;
        private ComboBox comboBoxChannelMode;
        private Button buttonAbout;
        private ProgressBar processingProgress;
        private Button clearListButton;
        private Label labelMaxBitrate;
        private TabControl tabControl;
        private TabPage tabPageAllFiles;
        private TabPage tabPageFailedFiles;
        private DataGridView dataGridViewFailed;
        private DataGridViewTextBoxColumn InputFiles;
        private DataGridViewTextBoxColumn FailedFilePath;
        private DataGridViewTextBoxColumn FailedErrorMessage;
    }
}
