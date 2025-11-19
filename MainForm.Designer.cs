namespace DATAFILTER
{
    partial class MainForm
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                filterWorker?.Dispose();
                textChangedTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
        #region PHẦN THIẾT KẾ UI ỨNG DỤNG
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.inputTextBox = new System.Windows.Forms.RichTextBox();
            this.filterButton = new System.Windows.Forms.Button();
            this.resultTextBox = new System.Windows.Forms.RichTextBox();
            this.inputCountLabel = new System.Windows.Forms.Label();
            this.resultCountLabel = new System.Windows.Forms.Label();
            this.clearButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.optionPanel = new System.Windows.Forms.Panel();
            this.lineCountLabel = new System.Windows.Forms.Label();
            this.lineCountComboBox = new System.Windows.Forms.ComboBox();
            this.buttonPanel.SuspendLayout();
            this.optionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputTextBox
            // 
            this.inputTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.inputTextBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.inputTextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputTextBox.ForeColor = System.Drawing.Color.Gray;
            this.inputTextBox.Location = new System.Drawing.Point(0, 0);
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.Size = new System.Drawing.Size(429, 335);
            this.inputTextBox.TabIndex = 0;
            this.inputTextBox.Text = "";
            this.inputTextBox.WordWrap = false;
            this.inputTextBox.TextChanged += new System.EventHandler(this.InputTextBox_TextChanged);
            // 
            // filterButton
            // 
            this.filterButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.filterButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.filterButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.filterButton.ForeColor = System.Drawing.Color.White;
            this.filterButton.Location = new System.Drawing.Point(12, 10);
            this.filterButton.Name = "filterButton";
            this.filterButton.Size = new System.Drawing.Size(404, 35);
            this.filterButton.TabIndex = 1;
            this.filterButton.Text = "LỌC DỮ LIỆU";
            this.filterButton.UseVisualStyleBackColor = false;
            this.filterButton.Click += new System.EventHandler(this.FilterButton_Click);
            // 
            // resultTextBox
            // 
            this.resultTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.resultTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultTextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resultTextBox.Location = new System.Drawing.Point(0, 395);
            this.resultTextBox.Name = "resultTextBox";
            this.resultTextBox.ReadOnly = true;
            this.resultTextBox.Size = new System.Drawing.Size(429, 313);
            this.resultTextBox.TabIndex = 2;
            this.resultTextBox.Text = "";
            this.resultTextBox.WordWrap = false;
            // 
            // inputCountLabel
            // 
            this.inputCountLabel.BackColor = System.Drawing.Color.LightBlue;
            this.inputCountLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.inputCountLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputCountLabel.Location = new System.Drawing.Point(0, 335);
            this.inputCountLabel.Name = "inputCountLabel";
            this.inputCountLabel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.inputCountLabel.Size = new System.Drawing.Size(429, 25);
            this.inputCountLabel.TabIndex = 3;
            this.inputCountLabel.Text = "Số lượng nhập vào: 0";
            this.inputCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // resultCountLabel
            // 
            this.resultCountLabel.BackColor = System.Drawing.Color.LightGreen;
            this.resultCountLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.resultCountLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resultCountLabel.Location = new System.Drawing.Point(0, 708);
            this.resultCountLabel.Name = "resultCountLabel";
            this.resultCountLabel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.resultCountLabel.Size = new System.Drawing.Size(429, 25);
            this.resultCountLabel.TabIndex = 4;
            this.resultCountLabel.Text = "Số lượng kết quả: 0";
            this.resultCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // clearButton
            // 
            this.clearButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.clearButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clearButton.ForeColor = System.Drawing.Color.White;
            this.clearButton.Location = new System.Drawing.Point(12, 52);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(195, 35);
            this.clearButton.TabIndex = 6;
            this.clearButton.Text = "XÓA TOÀN BỘ";
            this.clearButton.UseVisualStyleBackColor = false;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // exportButton
            // 
            this.exportButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.exportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exportButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exportButton.ForeColor = System.Drawing.Color.White;
            this.exportButton.Location = new System.Drawing.Point(221, 52);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(195, 35);
            this.exportButton.TabIndex = 7;
            this.exportButton.Text = "XUẤT FILE TXT";
            this.exportButton.UseVisualStyleBackColor = false;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.filterButton);
            this.buttonPanel.Controls.Add(this.clearButton);
            this.buttonPanel.Controls.Add(this.exportButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 733);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(429, 100);
            this.buttonPanel.TabIndex = 5;
            // 
            // optionPanel
            // 
            this.optionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(243)))), ((int)(((byte)(224)))));
            this.optionPanel.Controls.Add(this.lineCountLabel);
            this.optionPanel.Controls.Add(this.lineCountComboBox);
            this.optionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.optionPanel.Location = new System.Drawing.Point(0, 360);
            this.optionPanel.Name = "optionPanel";
            this.optionPanel.Size = new System.Drawing.Size(429, 35);
            this.optionPanel.TabIndex = 8;
            // 
            // lineCountLabel
            // 
            this.lineCountLabel.AutoSize = true;
            this.lineCountLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lineCountLabel.Location = new System.Drawing.Point(7, 9);
            this.lineCountLabel.Name = "lineCountLabel";
            this.lineCountLabel.Size = new System.Drawing.Size(185, 17);
            this.lineCountLabel.TabIndex = 0;
            this.lineCountLabel.Text = "Số dòng lọc (nếu trùng key):";
            // 
            // lineCountComboBox
            // 
            this.lineCountComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lineCountComboBox.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lineCountComboBox.FormattingEnabled = true;
            this.lineCountComboBox.Items.AddRange(new object[] {
            "1 dòng (mặc định)",
            "2 dòng",
            "3 dòng"});
            this.lineCountComboBox.Location = new System.Drawing.Point(196, 6);
            this.lineCountComboBox.Name = "lineCountComboBox";
            this.lineCountComboBox.Size = new System.Drawing.Size(226, 24);
            this.lineCountComboBox.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 833);
            this.Controls.Add(this.resultTextBox);
            this.Controls.Add(this.resultCountLabel);
            this.Controls.Add(this.optionPanel);
            this.Controls.Add(this.inputCountLabel);
            this.Controls.Add(this.inputTextBox);
            this.Controls.Add(this.buttonPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LỌC TỌA ĐỘ TRÙNG LẶP - Nông Văn Phấn";
            this.buttonPanel.ResumeLayout(false);
            this.optionPanel.ResumeLayout(false);
            this.optionPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox inputTextBox;
        private System.Windows.Forms.Button filterButton;
        private System.Windows.Forms.RichTextBox resultTextBox;
        private System.Windows.Forms.Label inputCountLabel;
        private System.Windows.Forms.Label resultCountLabel;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Panel optionPanel;
        private System.Windows.Forms.ComboBox lineCountComboBox;
        private System.Windows.Forms.Label lineCountLabel;
    }
}