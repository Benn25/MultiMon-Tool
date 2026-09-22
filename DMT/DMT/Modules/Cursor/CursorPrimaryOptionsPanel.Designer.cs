namespace DMT.Modules.Cursor
{
	partial class CursorPrimaryOptionsPanel
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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.picPreview = new System.Windows.Forms.PictureBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.radioButtonClick = new System.Windows.Forms.RadioButton();
			this.radioButtonNames = new System.Windows.Forms.RadioButton();
			this.textBoxNames = new System.Windows.Forms.TextBox();
			this.buttonNames = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(335, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "This specifies the area you want to treat as your primary working area.";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 17);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(460, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "This is used by the sticky/locked cursor options to allow free movement into and " +
    "within this area.";
			// 
			// picPreview
			// 
			this.picPreview.Location = new System.Drawing.Point(3, 88);
			this.picPreview.Name = "picPreview";
			this.picPreview.Size = new System.Drawing.Size(494, 200);
			this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.picPreview.TabIndex = 2;
			this.picPreview.TabStop = false;
			this.picPreview.MouseClick += new System.Windows.Forms.MouseEventHandler(this.picPreview_MouseClick);
			// 
			// label4
			// 
			this.label4.ForeColor = System.Drawing.Color.Red;
			this.label4.Location = new System.Drawing.Point(0, 377);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(497, 18);
			this.label4.TabIndex = 4;
			this.label4.Text = "warning";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.label4.Visible = false;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 34);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(383, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Monitors higlighted in blue are considered to be part of the primary working area" +
    ".";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(6, 51);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(491, 35);
			this.label5.TabIndex = 6;
			this.label5.Text = "If no monitors are selected, then only the primary screen will be considered to b" +
    "e in the primary working area.";
			// 
			// radioButtonClick
			// 
			this.radioButtonClick.AutoSize = true;
			this.radioButtonClick.Location = new System.Drawing.Point(3, 294);
			this.radioButtonClick.Name = "radioButtonClick";
			this.radioButtonClick.Size = new System.Drawing.Size(326, 17);
			this.radioButtonClick.TabIndex = 7;
			this.radioButtonClick.TabStop = true;
			this.radioButtonClick.Text = "Click a monitor to add or remove it from the primary working area";
			this.radioButtonClick.UseVisualStyleBackColor = true;
			this.radioButtonClick.CheckedChanged += new System.EventHandler(this.radioButtonClick_CheckedChanged);
			// 
			// radioButtonNames
			// 
			this.radioButtonNames.AutoSize = true;
			this.radioButtonNames.Location = new System.Drawing.Point(3, 317);
			this.radioButtonNames.Name = "radioButtonNames";
			this.radioButtonNames.Size = new System.Drawing.Size(146, 17);
			this.radioButtonNames.TabIndex = 8;
			this.radioButtonNames.TabStop = true;
			this.radioButtonNames.Text = "Following named monitors";
			this.radioButtonNames.UseVisualStyleBackColor = true;
			this.radioButtonNames.CheckedChanged += new System.EventHandler(this.radioButtonNames_CheckedChanged);
			// 
			// textBoxNames
			// 
			this.textBoxNames.Location = new System.Drawing.Point(20, 340);
			this.textBoxNames.Name = "textBoxNames";
			this.textBoxNames.ReadOnly = true;
			this.textBoxNames.Size = new System.Drawing.Size(382, 20);
			this.textBoxNames.TabIndex = 9;
			// 
			// buttonNames
			// 
			this.buttonNames.Location = new System.Drawing.Point(408, 337);
			this.buttonNames.Name = "buttonNames";
			this.buttonNames.Size = new System.Drawing.Size(75, 23);
			this.buttonNames.TabIndex = 10;
			this.buttonNames.Text = "Edit...";
			this.buttonNames.UseVisualStyleBackColor = true;
			this.buttonNames.Click += new System.EventHandler(this.buttonNames_Click);
			// 
			// CursorPrimaryOptionsPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonNames);
			this.Controls.Add(this.textBoxNames);
			this.Controls.Add(this.radioButtonNames);
			this.Controls.Add(this.radioButtonClick);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.picPreview);
			this.Name = "CursorPrimaryOptionsPanel";
			this.Size = new System.Drawing.Size(500, 400);
			((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PictureBox picPreview;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.RadioButton radioButtonClick;
		private System.Windows.Forms.RadioButton radioButtonNames;
		private System.Windows.Forms.TextBox textBoxNames;
		private System.Windows.Forms.Button buttonNames;


	}
}
