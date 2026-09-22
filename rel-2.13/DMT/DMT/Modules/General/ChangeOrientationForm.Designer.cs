namespace DMT.Modules.General
{
	partial class ChangeOrientationForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeOrientationForm));
			this.label1 = new System.Windows.Forms.Label();
			this.radioButtonOrientation0 = new System.Windows.Forms.RadioButton();
			this.radioButtonOrientation90 = new System.Windows.Forms.RadioButton();
			this.radioButtonOrientation180 = new System.Windows.Forms.RadioButton();
			this.radioButtonOrientation270 = new System.Windows.Forms.RadioButton();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.pictureBoxOrientation0 = new System.Windows.Forms.PictureBox();
			this.pictureBoxOrientation90 = new System.Windows.Forms.PictureBox();
			this.pictureBoxOrientation180 = new System.Windows.Forms.PictureBox();
			this.pictureBoxOrientation270 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation0)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation90)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation180)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation270)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(191, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Monitor clockwise rotation from default:";
			// 
			// radioButtonOrientation0
			// 
			this.radioButtonOrientation0.AutoSize = true;
			this.radioButtonOrientation0.Location = new System.Drawing.Point(30, 57);
			this.radioButtonOrientation0.Name = "radioButtonOrientation0";
			this.radioButtonOrientation0.Size = new System.Drawing.Size(35, 17);
			this.radioButtonOrientation0.TabIndex = 1;
			this.radioButtonOrientation0.TabStop = true;
			this.radioButtonOrientation0.Text = "0°";
			this.radioButtonOrientation0.UseVisualStyleBackColor = true;
			// 
			// radioButtonOrientation90
			// 
			this.radioButtonOrientation90.AutoSize = true;
			this.radioButtonOrientation90.Location = new System.Drawing.Point(130, 57);
			this.radioButtonOrientation90.Name = "radioButtonOrientation90";
			this.radioButtonOrientation90.Size = new System.Drawing.Size(41, 17);
			this.radioButtonOrientation90.TabIndex = 2;
			this.radioButtonOrientation90.TabStop = true;
			this.radioButtonOrientation90.Text = "90°";
			this.radioButtonOrientation90.UseVisualStyleBackColor = true;
			// 
			// radioButtonOrientation180
			// 
			this.radioButtonOrientation180.AutoSize = true;
			this.radioButtonOrientation180.Location = new System.Drawing.Point(231, 57);
			this.radioButtonOrientation180.Name = "radioButtonOrientation180";
			this.radioButtonOrientation180.Size = new System.Drawing.Size(47, 17);
			this.radioButtonOrientation180.TabIndex = 3;
			this.radioButtonOrientation180.TabStop = true;
			this.radioButtonOrientation180.Text = "180°";
			this.radioButtonOrientation180.UseVisualStyleBackColor = true;
			// 
			// radioButtonOrientation270
			// 
			this.radioButtonOrientation270.AutoSize = true;
			this.radioButtonOrientation270.Location = new System.Drawing.Point(330, 57);
			this.radioButtonOrientation270.Name = "radioButtonOrientation270";
			this.radioButtonOrientation270.Size = new System.Drawing.Size(47, 17);
			this.radioButtonOrientation270.TabIndex = 4;
			this.radioButtonOrientation270.TabStop = true;
			this.radioButtonOrientation270.Text = "270°";
			this.radioButtonOrientation270.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(103, 159);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 5;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(230, 159);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 6;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// pictureBoxOrientation0
			// 
			this.pictureBoxOrientation0.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxOrientation0.Image")));
			this.pictureBoxOrientation0.Location = new System.Drawing.Point(30, 80);
			this.pictureBoxOrientation0.Name = "pictureBoxOrientation0";
			this.pictureBoxOrientation0.Size = new System.Drawing.Size(48, 48);
			this.pictureBoxOrientation0.TabIndex = 7;
			this.pictureBoxOrientation0.TabStop = false;
			this.pictureBoxOrientation0.Click += new System.EventHandler(this.pictureBoxOrientation0_Click);
			// 
			// pictureBoxOrientation90
			// 
			this.pictureBoxOrientation90.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxOrientation90.Image")));
			this.pictureBoxOrientation90.Location = new System.Drawing.Point(130, 80);
			this.pictureBoxOrientation90.Name = "pictureBoxOrientation90";
			this.pictureBoxOrientation90.Size = new System.Drawing.Size(48, 48);
			this.pictureBoxOrientation90.TabIndex = 8;
			this.pictureBoxOrientation90.TabStop = false;
			this.pictureBoxOrientation90.Click += new System.EventHandler(this.pictureBoxOrientation90_Click);
			// 
			// pictureBoxOrientation180
			// 
			this.pictureBoxOrientation180.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxOrientation180.Image")));
			this.pictureBoxOrientation180.Location = new System.Drawing.Point(230, 80);
			this.pictureBoxOrientation180.Name = "pictureBoxOrientation180";
			this.pictureBoxOrientation180.Size = new System.Drawing.Size(48, 48);
			this.pictureBoxOrientation180.TabIndex = 9;
			this.pictureBoxOrientation180.TabStop = false;
			this.pictureBoxOrientation180.Click += new System.EventHandler(this.pictureBoxOrientation180_Click);
			// 
			// pictureBoxOrientation270
			// 
			this.pictureBoxOrientation270.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxOrientation270.Image")));
			this.pictureBoxOrientation270.Location = new System.Drawing.Point(330, 80);
			this.pictureBoxOrientation270.Name = "pictureBoxOrientation270";
			this.pictureBoxOrientation270.Size = new System.Drawing.Size(48, 48);
			this.pictureBoxOrientation270.TabIndex = 10;
			this.pictureBoxOrientation270.TabStop = false;
			this.pictureBoxOrientation270.Click += new System.EventHandler(this.pictureBoxOrientation270_Click);
			// 
			// ChangeOrientationForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(400, 204);
			this.Controls.Add(this.pictureBoxOrientation270);
			this.Controls.Add(this.pictureBoxOrientation180);
			this.Controls.Add(this.pictureBoxOrientation90);
			this.Controls.Add(this.pictureBoxOrientation0);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.radioButtonOrientation270);
			this.Controls.Add(this.radioButtonOrientation180);
			this.Controls.Add(this.radioButtonOrientation90);
			this.Controls.Add(this.radioButtonOrientation0);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChangeOrientationForm";
			this.ShowInTaskbar = false;
			this.Text = "Change Monitor Orientation";
			this.Load += new System.EventHandler(this.ChangeOrientationForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation0)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation90)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation180)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxOrientation270)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton radioButtonOrientation0;
		private System.Windows.Forms.RadioButton radioButtonOrientation90;
		private System.Windows.Forms.RadioButton radioButtonOrientation180;
		private System.Windows.Forms.RadioButton radioButtonOrientation270;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.PictureBox pictureBoxOrientation0;
		private System.Windows.Forms.PictureBox pictureBoxOrientation90;
		private System.Windows.Forms.PictureBox pictureBoxOrientation180;
		private System.Windows.Forms.PictureBox pictureBoxOrientation270;
	}
}