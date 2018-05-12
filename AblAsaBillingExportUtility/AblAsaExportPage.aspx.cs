using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;

using log4net;

namespace AblAsaBillingExportUtility
{
    public partial class AblAsaExportPage : System.Web.UI.Page
    {
        List<DetailBillingRecord> detailBillingRecordList = new List<DetailBillingRecord>();
        static ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public string AblAsaOutPath
        {
            get
            {
                // Returns the output path
                return WebConfigurationManager.AppSettings["AblAsaOutPath"];
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                AbleDatabaseWrapper.SetLogger(logger);
                AbleDatabaseWrapper.SetDetailBillingRecordList(detailBillingRecordList);
                BindGridView();
            }
        }

        protected void ExportButton_Click(object sender, EventArgs e)
        {
            List<BillingTableRecord> billingTableRecordList = AbleDatabaseWrapper.GetBillingTableRecords();
            logger.Info("Billing Table Record Count is " + billingTableRecordList.Count);

            //Check to see if any Billing records were found in the date range
            if (billingTableRecordList.Count > 0)
            {
                foreach (BillingTableRecord billingTableRecord in billingTableRecordList)
                {
                    logger.Info("Get Detail Billing Export for binder " + billingTableRecord.Binder + " Lot ID " + billingTableRecord.LotId + " Account " + billingTableRecord.Account);

                    //RMH 04-10-2018 OBSOLETE????? if ((billingTableRecord.Department.TrimEnd().Length == 0 && billingTableRecord.billByDept == false) || billingTableRecord.billByDept == true)
                        AbleDatabaseWrapper.GetDetailBillingExport(billingTableRecord.Account, billingTableRecord.Binder, billingTableRecord.LotId  
                                                               /*,billingTableRecord.billByDept, billingTableRecord.Department.TrimEnd()*/);
                }

                logger.Info("Get Detail Billing Record List");
                detailBillingRecordList = AbleDatabaseWrapper.GetDetailBillingRecordList();

                logger.Info("Bind the Grid View");
                BindGridView();

                //Create flat file for Vax
                logger.Info("Create AblAsa flat file");

                FlatFileGenerator.CreateAblAsaFlatFile(AblAsaOutPath, detailBillingRecordList);

                AbleDatabaseWrapper.ClearDetailBillingRecordList();
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnRowDataBound(object sender, GridViewRowEventArgs e)
        {
             
        }
  

        /// <summary>
        /// 
        /// </summary>
        public void BindGridView()
        {
            gvAbleAsa.DataSource = detailBillingRecordList;
            gvAbleAsa.DataBind();
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            AbleDatabaseWrapper.ClearDetailBillingRecordList();
            gvAbleAsa.DataBind();
        }
    }
}