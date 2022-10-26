namespace diagtool
{
    partial class diagtool
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnInfoSelectDir = new System.Windows.Forms.Button();
            this.btnCollectInfo = new System.Windows.Forms.Button();
            this.textBoxOutputDir = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxEEConfig = new System.Windows.Forms.CheckBox();
            this.checkBoxPClog = new System.Windows.Forms.CheckBox();
            this.checkBoxEELog = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnInfoSelectDir);
            this.groupBox1.Controls.Add(this.btnCollectInfo);
            this.groupBox1.Controls.Add(this.textBoxOutputDir);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.checkBoxEEConfig);
            this.groupBox1.Controls.Add(this.checkBoxPClog);
            this.groupBox1.Controls.Add(this.checkBoxEELog);
            this.groupBox1.Location = new System.Drawing.Point(24, 25);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(691, 171);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Collect Information";
            // 
            // btnInfoSelectDir
            // 
            this.btnInfoSelectDir.Location = new System.Drawing.Point(612, 97);
            this.btnInfoSelectDir.Name = "btnInfoSelectDir";
            this.btnInfoSelectDir.Size = new System.Drawing.Size(70, 23);
            this.btnInfoSelectDir.TabIndex = 7;
            this.btnInfoSelectDir.Text = "Browser...";
            this.btnInfoSelectDir.UseVisualStyleBackColor = true;
            this.btnInfoSelectDir.Click += new System.EventHandler(this.btnInfoSelectDir_Click);
            // 
            // btnCollectInfo
            // 
            this.btnCollectInfo.Location = new System.Drawing.Point(20, 137);
            this.btnCollectInfo.Name = "btnCollectInfo";
            this.btnCollectInfo.Size = new System.Drawing.Size(75, 23);
            this.btnCollectInfo.TabIndex = 6;
            this.btnCollectInfo.Text = "Collect";
            this.btnCollectInfo.UseVisualStyleBackColor = true;
            this.btnCollectInfo.Click += new System.EventHandler(this.btnCollectInfo_Click);
            // 
            // textBoxOutputDir
            // 
            this.textBoxOutputDir.Location = new System.Drawing.Point(18, 99);
            this.textBoxOutputDir.Name = "textBoxOutputDir";
            this.textBoxOutputDir.Size = new System.Drawing.Size(588, 20);
            this.textBoxOutputDir.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Output:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select Information:";
            // 
            // checkBoxEEConfig
            // 
            this.checkBoxEEConfig.AutoSize = true;
            this.checkBoxEEConfig.Checked = true;
            this.checkBoxEEConfig.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEEConfig.Location = new System.Drawing.Point(134, 46);
            this.checkBoxEEConfig.Name = "checkBoxEEConfig";
            this.checkBoxEEConfig.Size = new System.Drawing.Size(89, 17);
            this.checkBoxEEConfig.TabIndex = 2;
            this.checkBoxEEConfig.Text = "EE Config file";
            this.checkBoxEEConfig.UseVisualStyleBackColor = true;
            // 
            // checkBoxPClog
            // 
            this.checkBoxPClog.AutoSize = true;
            this.checkBoxPClog.Checked = true;
            this.checkBoxPClog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxPClog.Location = new System.Drawing.Point(255, 46);
            this.checkBoxPClog.Name = "checkBoxPClog";
            this.checkBoxPClog.Size = new System.Drawing.Size(77, 17);
            this.checkBoxPClog.TabIndex = 1;
            this.checkBoxPClog.Text = "PC Log file";
            this.checkBoxPClog.UseVisualStyleBackColor = true;
            // 
            // checkBoxEELog
            // 
            this.checkBoxEELog.AutoSize = true;
            this.checkBoxEELog.Checked = true;
            this.checkBoxEELog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEELog.Location = new System.Drawing.Point(18, 46);
            this.checkBoxEELog.Name = "checkBoxEELog";
            this.checkBoxEELog.Size = new System.Drawing.Size(77, 17);
            this.checkBoxEELog.TabIndex = 0;
            this.checkBoxEELog.Text = "EE Log file";
            this.checkBoxEELog.UseVisualStyleBackColor = true;
            // 
            // diagtool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(718, 415);
            this.Controls.Add(this.groupBox1);
            this.Name = "diagtool";
            this.Text = "Exchange Enforcer Diagnose Tool";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxEEConfig;
        private System.Windows.Forms.CheckBox checkBoxPClog;
        private System.Windows.Forms.CheckBox checkBoxEELog;
        private System.Windows.Forms.Button btnInfoSelectDir;
        private System.Windows.Forms.Button btnCollectInfo;
        private System.Windows.Forms.TextBox textBoxOutputDir;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}

