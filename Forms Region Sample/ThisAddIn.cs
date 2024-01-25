using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace Forms_Region_Sample
{
    public partial class ThisAddIn
    {
        //This add-in will popup a Toast Form using a BackgroundWorker component
        //During ThisAddIn_Startup, the BackgroundWorker will be initialized and started
        //the BackgroundWorker will run in the background and will popup the Toast Form
       
        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
           
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            // Note: Outlook no longer raises this event. If you have code that 
            //    must run when Outlook shuts down, see https://go.microsoft.com/fwlink/?LinkId=506785
        }

        
        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
