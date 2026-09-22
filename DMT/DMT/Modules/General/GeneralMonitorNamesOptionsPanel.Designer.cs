namespace DMT.Modules.General
{
	partial class GeneralMonitorNamesOptionsPanel
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.picPreview = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.buttonChangeName = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.labelBrand = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.labelGpu = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.labelSize = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
			this.SuspendLayout();
			// 
			// picPreview
			// 
			this.picPreview.Location = new System.Drawing.Point(3, 3);
			this.picPreview.Name = "picPreview";
			this.picPreview.Size = new System.Drawing.Size(494, 200);
			this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.picPreview.TabIndex = 2;
			this.picPreview.TabStop = false;
			this.picPreview.MouseClick += new System.Windows.Forms.MouseEventHandler(this.picPreview_MouseClick);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 257);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(101, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Your Monitor Name:";
			// 
			// textBoxName
			// 
			this.textBoxName.Location = new System.Drawing.Point(127, 254);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.ReadOnly = true;
			this.textBoxName.Size = new System.Drawing.Size(200, 20);
			this.textBoxName.TabIndex = 4;
			// 
			// buttonChangeName
			// 
			this.buttonChangeName.Location = new System.Drawing.Point(333, 252);
			this.buttonChangeName.Name = "buttonChangeName";
			this.buttonChangeName.Size = new System.Drawing.Size(114, 23);
			this.buttonChangeName.TabIndex = 5;
			this.buttonChangeName.Text = "Change Name...";
			this.buttonChangeName.UseVisualStyleBackColor = true;
			this.buttonChangeName.Click += new System.EventHandler(this.buttonChangeName_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(20, 280);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(71, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Brand/Name:";
			// 
			// labelBrand
			// 
			this.labelBrand.Location = new System.Drawing.Point(124, 280);
			this.labelBrand.Name = "labelBrand";
			this.labelBrand.Size = new System.Drawing.Size(200, 20);
			this.labelBrand.TabIndex = 7;
			this.labelBrand.Text = "label3";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(20, 304);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(33, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "GPU:";
			// 
			// labelGpu
			// 
			this.labelGpu.Location = new System.Drawing.Point(124, 304);
			this.labelGpu.Name = "labelGpu";
			this.labelGpu.Size = new System.Drawing.Size(200, 20);
			this.labelGpu.TabIndex = 9;
			this.labelGpu.Text = "label5";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(20, 329);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(30, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "Size:";
			// 
			// labelSize
			// 
			this.labelSize.Location = new System.Drawing.Point(124, 329);
			this.labelSize.Name = "labelSize";
			this.labelSize.Size = new System.Drawing.Size(200, 20);
			this.labelSize.TabIndex = 11;
			this.labelSize.Text = "label7";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(3, 206);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(494, 43);
			this.label3.TabIndex = 12;
			this.label3.Text = "This is optional, but allows you to give each physical monitor a name of your own" +
    " choice which can then be used when selecting monitor order and when selecting t" +
    "he primary working area. \r\n";
			// 
			// GeneralMonitorNamesOptionsPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.labelSize);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.labelGpu);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.labelBrand);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonChangeName);
			this.Controls.Add(this.textBoxName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.picPreview);
			this.Name = "GeneralMonitorNamesOptionsPanel";
			this.Size = new System.Drawing.Size(500, 360);
			((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox picPreview;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Button buttonChangeName;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label labelBrand;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label labelGpu;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label labelSize;
		private System.Windows.Forms.Label label3;
	}
}
