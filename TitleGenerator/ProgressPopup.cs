using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using TitleGenerator.Tasks;

namespace TitleGenerator
{
	public partial class ProgressPopup : Form
	{
		private delegate void UpdateProgress( string message );

		private delegate void UpdateProgressBarDelegate();

		private readonly Queue<ITask> m_taskQueue;
		private int m_taskNum, m_taskDone = 0;
		private object m_lock;
		private Logger m_log;

		public ProgressPopup( Logger log )
		{
			InitializeComponent();

			m_taskQueue = new Queue<ITask>();
			m_lock = new object();
			m_log = log;
		}

		public void SetTasks( List<ITask> taskList, string title )
		{
			if( m_taskQueue.Count > 0 )
				m_taskQueue.Clear();

			foreach( ITask t in taskList )
				m_taskQueue.Enqueue( t );

			m_taskNum = taskList.Count;
			m_taskDone = 0;

			this.Text = title;
			pbProgress.Value = 0;
			lblMessage.Text = "";
			TaskStatus.Abort = false;
		}


		private void ProgressPopup_Shown( object sender, EventArgs e )
		{
			bwTaskMaster.RunWorkerAsync();
		}

		private void UpdateProgressBar()
		{
			if( InvokeRequired )
			{
				BeginInvoke( new UpdateProgressBarDelegate( UpdateProgressBar ) );
				return;
			}

			double progress;
			lock ( m_lock )
				progress = m_taskDone*1.0/m_taskNum;

			pbProgress.Value = (int)( progress*100 );
		}

		private void TaskOnMessage( string message )
		{
			if( InvokeRequired )
			{
				BeginInvoke( new UpdateProgress( TaskOnMessage ), new object[] { message } );
				return;
			}

			lblMessage.Text = message;
			lblMessage.Invalidate();
		}


		private void bwTaskMaster_DoWork( object sender, DoWorkEventArgs e )
		{
			ITask task;

			while( ( task = m_taskQueue.Dequeue() ) != null )
			{
				task.Message += TaskOnMessage;
				bool res = task.Run();
				if( !res )
				{
					TaskStatus.Abort = true;
					foreach( string s in task.Errors )
						m_log.Log( s, Logger.LogType.Error );
					m_log.Dump( "log.txt" );
					break;
				}
				m_taskDone++;
				UpdateProgressBar();
			}
		}

		private void bwTaskMaster_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
		{
			DialogResult = !TaskStatus.Abort ? DialogResult.OK : DialogResult.Abort;
			Hide();
		}
	}
}
