namespace DMT.Library.GuiUtils
{
	partial class EditNameListForm
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
			this.label1 = new System.Windows.Forms.Label();
			this.listBoxAvailable = new System.Windows.Forms.ListBox();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.labelSelected = new System.Windows.Forms.Label();
			this.listBoxSelected = new System.Windows.Forms.ListBox();
			this.buttonUp = new System.Windows.Forms.Button();
			this.buttonDown = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(22, 35);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(93, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Available Monitors";
			// 
			// listBoxAvailable
			// 
			this.listBoxAvailable.FormattingEnabled = true;
			this.listBoxAvailable.Location = new System.Drawing.Point(25, 51);
			this.listBoxAvailable.Name = "listBoxAvailable";
			this.listBoxAvailable.Size = new System.Drawing.Size(120, 173);
			this.listBoxAvailable.TabIndex = 1;
			this.listBoxAvailable.SelectedIndexChanged += new System.EventHandler(this.listBoxAvailable_SelectedIndexChanged);
			this.listBoxAvailable.DoubleClick += new System.EventHandler(this.listBoxAvailable_DoubleClick);
			// 
			// buttonAdd
			// 
			this.buttonAdd.Image = global::DMT.Properties.Resources.RightArrow;
			this.buttonAdd.Location = new System.Drawing.Point(151, 91);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Size = new System.Drawing.Size(32, 32);
			this.buttonAdd.TabIndex = 2;
			this.buttonAdd.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.buttonAdd.UseVisualStyleBackColor = true;
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			// 
			// buttonRemove
			// 
			this.buttonRemove.Image = global::DMT.Properties.Resources.LeftArrow;
			this.buttonRemove.Location = new System.Drawing.Point(151, 156);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(32, 32);
			this.buttonRemove.TabIndex = 3;
			this.buttonRemove.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
			// 
			// labelSelected
			// 
			this.labelSelected.AutoSize = true;
			this.labelSelected.Location = new System.Drawing.Point(186, 35);
			this.labelSelected.Name = "labelSelected";
			this.labelSelected.Size = new System.Drawing.Size(92, 13);
			this.labelSelected.TabIndex = 4;
			this.labelSelected.Text = "Selected Monitors";
			// 
			// listBoxSelected
			// 
			this.listBoxSelected.FormattingEnabled = true;
			this.listBoxSelected.Location = new System.Drawing.Point(189, 51);
			this.listBoxSelected.Name = "listBoxSelected";
			this.listBoxSelected.Size = new System.Drawing.Size(120, 173);
			this.listBoxSelected.TabIndex = 5;
			this.listBoxSelected.SelectedIndexChanged += new System.EventHandler(this.listBoxSelected_SelectedIndexChanged);
			this.listBoxSelected.DoubleClick += new System.EventHandler(this.listBoxSelected_DoubleClick);
			// 
			// buttonUp
			// 
			this.buttonUp.Image = global::DMT.Properties.Resources.UpArrow;
			this.buttonUp.Location = new System.Drawing.Point(315, 91);
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.Size = new System.Drawing.Size(32, 32);
			this.buttonUp.TabIndex = 6;
			this.buttonUp.UseVisualStyleBackColor = true;
			this.buttonUp.Click += new System.EventHandler(this.buttonUp_Click);
			// 
			// buttonDown
			// 
			this.buttonDown.Image = global::DMT.Properties.Resources.DownArrow;
			this.buttonDown.Location = new System.Drawing.Point(315, 156);
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.Size = new System.Drawing.Size(32, 32);
			this.buttonDown.TabIndex = 7;
			this.buttonDown.UseVisualStyleBackColor = true;
			this.buttonDown.Click += new System.EventHandler(this.buttonDown_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(189, 253);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 8;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(272, 253);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// EditNameListForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(375, 288);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonDown);
			this.Controls.Add(this.buttonUp);
			this.Controls.Add(this.listBoxSelected);
			this.Controls.Add(this.labelSelected);
			this.Controls.Add(this.buttonRemove);
			this.Controls.Add(this.buttonAdd);
			this.Controls.Add(this.listBoxAvailable);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditNameListForm";
			this.Text = "EditNameListForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListBox listBoxAvailable;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Label labelSelected;
		private System.Windows.Forms.ListBox listBoxSelected;
		private System.Windows.Forms.Button buttonUp;
		private System.Windows.Forms.Button buttonDown;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
	}
}