namespace Compiler;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        button1 = new System.Windows.Forms.Button();
        button2 = new System.Windows.Forms.Button();
        button3 = new System.Windows.Forms.Button();
        Clear = new System.Windows.Forms.Button();
        Export = new System.Windows.Forms.Button();
        rtbCode = new System.Windows.Forms.RichTextBox();
        dgvTokens = new System.Windows.Forms.DataGridView();
        dgvErrors = new System.Windows.Forms.DataGridView();
        lblStats = new System.Windows.Forms.Label();
        pnlLines = new System.Windows.Forms.Panel();
        ((System.ComponentModel.ISupportInitialize)dgvTokens).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvErrors).BeginInit();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new System.Drawing.Point(0, 15);
        button1.Name = "button1";
        button1.Size = new System.Drawing.Size(100, 40);
        button1.TabIndex = 0;
        button1.Text = "Open";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // button2
        // 
        button2.Location = new System.Drawing.Point(106, 15);
        button2.Name = "button2";
        button2.Size = new System.Drawing.Size(100, 40);
        button2.TabIndex = 1;
        button2.Text = "Save";
        button2.UseVisualStyleBackColor = true;
        button2.Click += button2_Click;
        // 
        // button3
        // 
        button3.Location = new System.Drawing.Point(212, 15);
        button3.Name = "button3";
        button3.Size = new System.Drawing.Size(100, 40);
        button3.TabIndex = 2;
        button3.Text = "Scan";
        button3.UseVisualStyleBackColor = true;
        button3.Click += button3_Click;
        // 
        // Clear
        // 
        Clear.Location = new System.Drawing.Point(318, 15);
        Clear.Name = "Clear";
        Clear.Size = new System.Drawing.Size(100, 40);
        Clear.TabIndex = 3;
        Clear.Text = "Clear";
        Clear.UseVisualStyleBackColor = true;
        Clear.Click += button4_Click;
        // 
        // Export
        // 
        Export.Location = new System.Drawing.Point(424, 15);
        Export.Name = "Export";
        Export.Size = new System.Drawing.Size(100, 40);
        Export.TabIndex = 4;
        Export.Text = "Export (CSV)";
        Export.UseVisualStyleBackColor = true;
        Export.Click += button5_Click;
        // 
        // rtbCode
        // 
        rtbCode.Location = new System.Drawing.Point(33, 61);
        rtbCode.Name = "rtbCode";
        rtbCode.Size = new System.Drawing.Size(471, 658);
        rtbCode.TabIndex = 5;
        rtbCode.Text = "";
        // 
        // dgvTokens
        // 
        dgvTokens.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvTokens.Location = new System.Drawing.Point(510, 61);
        dgvTokens.Name = "dgvTokens";
        dgvTokens.RowHeadersWidth = 51;
        dgvTokens.Size = new System.Drawing.Size(487, 408);
        dgvTokens.TabIndex = 6;
        dgvTokens.Text = "dataGridView1";
        // 
        // dgvErrors
        // 
        dgvErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvErrors.Location = new System.Drawing.Point(510, 475);
        dgvErrors.Name = "dgvErrors";
        dgvErrors.RowHeadersWidth = 51;
        dgvErrors.Size = new System.Drawing.Size(487, 211);
        dgvErrors.TabIndex = 7;
        dgvErrors.Text = "dataGridView2";
        // 
        // lblStats
        // 
        lblStats.Location = new System.Drawing.Point(510, 700);
        lblStats.Name = "lblStats";
        lblStats.Size = new System.Drawing.Size(487, 19);
        lblStats.TabIndex = 8;
        lblStats.Text = "label1";
        // 
        // pnlLines
        // 
        pnlLines.Location = new System.Drawing.Point(0, 61);
        pnlLines.Name = "pnlLines";
        pnlLines.Size = new System.Drawing.Size(35, 658);
        pnlLines.TabIndex = 9;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1010, 726);
        Controls.Add(pnlLines);
        Controls.Add(lblStats);
        Controls.Add(dgvErrors);
        Controls.Add(dgvTokens);
        Controls.Add(rtbCode);
        Controls.Add(Export);
        Controls.Add(Clear);
        Controls.Add(button3);
        Controls.Add(button2);
        Controls.Add(button1);
        Text = "Form1";
        ((System.ComponentModel.ISupportInitialize)dgvTokens).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvErrors).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlLines;

    private System.Windows.Forms.DataGridView dgvErrors;
    private System.Windows.Forms.Label lblStats;

    private System.Windows.Forms.DataGridView dgvTokens;

    private System.Windows.Forms.RichTextBox rtbCode;

    private System.Windows.Forms.Button Export;

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.Button button3;
    private System.Windows.Forms.Button Clear;

    #endregion
}