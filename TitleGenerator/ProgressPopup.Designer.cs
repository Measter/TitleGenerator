namespace TitleGenerator
{
	partial class ProgressPopup
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.pbProgress = new System.Windows.Forms.ProgressBar();
			this.lblMessage = new System.Windows.Forms.Label();
			this.bwTaskMaster = new System.ComponentModel.BackgroundWorker();
			this.SuspendLayout();
			// 
			// pbProgress
			// 
			this.pbProgress.Location = new System.Drawing.Point(12, 12);
			this.pbProgress.Name = "pbProgress";
			this.pbProgress.Size = new System.Drawing.Size(399, 23);
			this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.pbProgress.TabIndex = 0;
			// 
			// lblMessage
			// 
			this.lblMessage.Location = new System.Drawing.Point(12, 40);
			this.lblMessage.Name = "lblMessage";
			this.lblMessage.Size = new System.Drawing.Size(399, 13);
			this.lblMessage.TabIndex = 1;
			this.lblMessage.Text = "label1";
			// 
			// bwTaskMaster
			// 
			this.bwTaskMaster.WorkerSupportsCancellation = true;
			this.bwTaskMaster.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwTaskMaster_DoWork);
			this.bwTaskMaster.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bwTaskMaster_RunWorkerCompleted);
			// 
			// ProgressPopup
			// 
			this.ClientSize = new System.Drawing.Size(423, 62);
			this.Controls.Add(this.lblMessage);
			this.Controls.Add(this.pbProgress);
			this.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressPopup";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "ProgressPopup";
			this.Shown += new System.EventHandler(this.ProgressPopup_Shown);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ProgressBar pbProgress;
		private System.Windows.Forms.Label lblMessage;
		private System.ComponentModel.BackgroundWorker bwTaskMaster;
	}
}