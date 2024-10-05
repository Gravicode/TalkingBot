namespace TalkingBot
{
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            BtnClear = new Button();
            LogTxt = new RichTextBox();
            BtnStop = new Button();
            BtnStart = new Button();
            statusStrip1 = new StatusStrip();
            StatusLbl = new ToolStripStatusLabel();
            label1 = new Label();
            groupBox1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(BtnClear);
            groupBox1.Controls.Add(LogTxt);
            groupBox1.Controls.Add(BtnStop);
            groupBox1.Controls.Add(BtnStart);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(334, 238);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Voice Bot Panel";
            // 
            // BtnClear
            // 
            BtnClear.Location = new Point(168, 22);
            BtnClear.Name = "BtnClear";
            BtnClear.Size = new Size(75, 23);
            BtnClear.TabIndex = 3;
            BtnClear.Text = "&Clear Log";
            BtnClear.UseVisualStyleBackColor = true;
            // 
            // LogTxt
            // 
            LogTxt.Location = new Point(6, 66);
            LogTxt.Name = "LogTxt";
            LogTxt.Size = new Size(322, 166);
            LogTxt.TabIndex = 2;
            LogTxt.Text = "";
            // 
            // BtnStop
            // 
            BtnStop.Location = new Point(87, 22);
            BtnStop.Name = "BtnStop";
            BtnStop.Size = new Size(75, 23);
            BtnStop.TabIndex = 1;
            BtnStop.Text = "&Stop";
            BtnStop.UseVisualStyleBackColor = true;
            // 
            // BtnStart
            // 
            BtnStart.Location = new Point(6, 22);
            BtnStart.Name = "BtnStart";
            BtnStart.Size = new Size(75, 23);
            BtnStart.TabIndex = 0;
            BtnStart.Text = "&Start";
            BtnStart.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { StatusLbl });
            statusStrip1.Location = new Point(0, 253);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(354, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // StatusLbl
            // 
            StatusLbl.Name = "StatusLbl";
            StatusLbl.Size = new Size(123, 17);
            StatusLbl.Text = "Welcome to Voice Bot";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 48);
            label1.Name = "label1";
            label1.Size = new Size(32, 15);
            label1.TabIndex = 4;
            label1.Text = "Logs";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(354, 275);
            Controls.Add(statusStrip1);
            Controls.Add(groupBox1);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Voice Bot v0.1";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel StatusLbl;
        private RichTextBox LogTxt;
        private Button BtnStop;
        private Button BtnStart;
        private Button BtnClear;
        private Label label1;
    }
}
