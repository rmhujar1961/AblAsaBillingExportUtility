using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Data;

using System.Data.SqlClient;

using log4net;

namespace AblAsaBillingExportUtility
{
    class AbleDatabaseWrapper
    {
        static ILog logger;
        private static List<DetailBillingRecord> detailBillingRecordList;
        private static List<JobHeaderRecord> jobHeaderRecordList = new List<JobHeaderRecord>();

        //Bindery Billing codes
        private static string[] BillingCodes = {
                                                 "HT", //No trim
                                                 "CC", //No standard cover color
                                                 "SN", //Volumes with call
                                                 "SNL",//Extra Call lines
                                                 "IL", //Volumes with imprints
                                                 "IE", //Extra Imprint Lines
                                                 "EL", //Volumes with Extra Horz
                                                 "EH", //Extra Horz Title Lines
                                                 "VL", //Volumes with Extra Vert
                                                 "EV", //Extra Vert Title Lines
                                                 "FT", //Vols with cover Placements
                                                 "IF", //Extra Front/Back cover Imprints
                                                 "FL"  //Extra Front/Back cover Titles
                                               };


        public static void SetLogger(ILog log)
        {
            logger = log;
        }

        public static  List<DetailBillingRecord> GetDetailBillingRecordList()
        {
            return detailBillingRecordList;
        }

        public static void SetDetailBillingRecordList(List<DetailBillingRecord> detailBilling)
        {
            detailBillingRecordList = detailBilling;
        }

        public static void  ClearDetailBillingRecordList()
        {
            detailBillingRecordList.Clear();
        }

        public static List<JobHeaderRecord> GetJobHeaderRecordList()
        {
            return jobHeaderRecordList;
        }

        public static void ClearJobHeaderRecordList()
        {
            jobHeaderRecordList.Clear();
        }

        /// <summary>
        ///  Get Billing Table Records 
        /// </summary>
        /// <returns></returns>
        public static List<BillingTableRecord> GetBillingTableRecords()
        {
            DataTable dataTable = new DataTable();

            List<BillingTableRecord> billingTableRecordList = new List<BillingTableRecord>();

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            logger.Info("sp_AbleAsaGetProcesedLots Sql Connection String: " + sqlConnectionString);

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetProcesedLots"; //RMH was sp_AbleAsaGetBillingTableRecords

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetProcesedLots: " + ex.Message);
                    }
                }
            }

            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                BillingTableRecord billingTableRecord = new BillingTableRecord();

                billingTableRecord.Account = row[0].ToString();
                billingTableRecord.Binder = row[1].ToString();
                billingTableRecord.LotId = row[2].ToString();

                billingTableRecordList.Add(billingTableRecord);
            }

            return billingTableRecordList;
        }

        public static void GetDetailRecordsForExport(string Department, bool billByDept, string SubAccount, DataTable dataTable)
        {
            string OrderNumber = "";
            string CustomerNumber = "";
            int TotalJobQuantity = 0; //RMH 04-10-2018 not sure if this is needed

            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                int quantity = 0;
                int minutes = 1;
                string queryDepartment = "";

                if (billByDept == true)
                {
                    queryDepartment = row[8].ToString().Trim();
                }

                string product = row[7].ToString();
                string productCode = row[2].ToString().TrimEnd().ToUpper();

                int unitPrice = Int32.Parse(row[4].ToString());

                OrderNumber = row[0].ToString().TrimEnd();

                //If Leaf attachment and Product code is A change product code to 'NO'
                if ((product == "LEAF") && (productCode == "A"))
                {
                    productCode = "NO";
                }

                //Unit price needs to be greater than zero unless the product code is Notch
                //03-27-2018 All CLASS products need to be output even if the unit price is zero
                //02-26-2018 if ((unitPrice > 0 && (product == "LEAF" && productCode == "NO")) && (product != "EXTRA"))
                if ((unitPrice > 0 && (product != "EXTRA")) || (product == "CLASS"))
                {
                    //If bill by Department need to check that we are only counting the department for the current billing record
                    if ((billByDept == false) || ((billByDept == true) && (queryDepartment.ToUpper() == Department.ToUpper())))
                    {
                        DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                        detailBillingRecord.RecordType = "DTL";
                        detailBillingRecord.OrderNumber = OrderNumber;
                        CustomerNumber = row[1].ToString().TrimEnd();
                        if (billByDept == true)
                        {
                            CustomerNumber = SubAccount.TrimEnd();
                            detailBillingRecord.CustomerNumber = CustomerNumber;
                        }
                        else
                            detailBillingRecord.CustomerNumber = CustomerNumber;

                        detailBillingRecord.ProductCode = productCode;
                        int copies = Int32.Parse(row[5].ToString());

                        quantity = Int32.Parse(row[3].ToString()) * copies * minutes;

                        //Only Add the total quantity for the CLASS product - Total Quantity is used for the HDR Record
                        if (product == "CLASS")
                            TotalJobQuantity += quantity;

                        detailBillingRecord.Quantity = quantity;
                        detailBillingRecord.UnitPrice = unitPrice;
                        detailBillingRecord.JobQuantity = "000000";
                        detailBillingRecord.Filler = "            ";

                        detailBillingRecordList.Add(detailBillingRecord);
                        logger.Info("Product: " + product + " ProductCode: " + productCode + " Quantity: " + quantity.ToString() + " OrderNumber: " + OrderNumber + " CustomerNumber: " + CustomerNumber);
                    }
                }
                else
                    logger.Info("Unit Price " + unitPrice.ToString() + " for Product: " + product + " ProductCode: " + productCode + " Quantity: " + quantity.ToString() + " OrderNumber: " + OrderNumber + " CustomerNumber: " + CustomerNumber);
            }
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="binder"></param>
       /// <param name="lotId"></param>
       /// <param name="billByDept"></param>
       /// <param name="Department"></param>
        public static void GetDetailBillingExport(string account, string binder, string lotId)
        {
            int TotalJobQuantity = 0;

            string SubAccount = "";
            string CustomerNumber = "";
            string OrderNumber = "";
            string JobId = "";
            string BillDepartment = "";
            int BillId = 0;

            //RMH Added 04-09-2018
            bool billByDept = false;
            string Department = "";
            List<string> departmentList = new List<string>(); 

            DataTable dataTable = new DataTable();

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;


            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaBillingExport";
                        sqlCommand.Parameters.Add("@Binder", SqlDbType.VarChar).Value = binder.TrimEnd();
                        sqlCommand.Parameters.Add("@LotId", SqlDbType.VarChar).Value = lotId.TrimEnd();
                        sqlCommand.Parameters.Add("@Account", SqlDbType.VarChar).Value = account.TrimEnd();//Added 04-02-2018

                        logger.Info("call sp_AbleAsaBillingExport stored procedure ");

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaBillingExport: " + ex.Message);
                    }
                }
            }

            //Get Job Quantity for VPQ and HDR Records
            //MOVED TotalJobQuantity = CalculateTotalJobQuanity( billByDept, Department, dataTable);

            foreach (DataRow row in dataTable.Rows)
            {
                //Get Job Quantity for VPQ and HDR Records
                Department = row[8].ToString().Trim();
                billByDept = row[9].ToString() == "True" ? true : false;

                if (departmentList.Contains(Department, StringComparer.OrdinalIgnoreCase) == false)
                {
                    departmentList.Add(Department);
                    TotalJobQuantity = CalculateTotalJobQuanity(billByDept, Department, dataTable);
                    //}
                    //Check for bill by department and substitute 
                    if (billByDept == true && Department.Length > 0)
                    {
                        //Get Department subaccount and set customer number
                        SubAccount = GetSubAccount(account, Department);
                        CustomerNumber = SubAccount;
                    }
                    else
                    {
                        CustomerNumber = row[1].ToString().TrimEnd();
                    }
                    OrderNumber = row[0].ToString().TrimEnd();

                    //break;
                    //}

                    //Check for bill by department and substitute 
                    //if (billByDept == true && Department.Length > 0)
                    //{
                    //Get Department subaccount
                    //    SubAccount = GetSubAccount(account, Department);
                    //    CustomerNumber = SubAccount;
                    //}

                    //Add VPQ record
                    AddOrderHeaderRecord(OrderNumber, CustomerNumber, TotalJobQuantity);

                    //Get Job Id
                    JobId = GetJobFromOrderNumber(OrderNumber);

                    //Add HDR record
                    AddJobHeaderRecord(JobId, CustomerNumber, TotalJobQuantity);

                    //NEW to get the actual detail records returned from the sp_AbleAsaBillingExport stored proc
                    GetDetailRecordsForExport(Department, billByDept, SubAccount, dataTable);

                    //Moved from below
                    if (billByDept == false)
                        BillDepartment = "TOTALS";
                    else
                        BillDepartment = Department;

                    BillId = GetBillId(account, lotId, BillDepartment);
                    //Get detail extras list
                    AddDetailExtrasRecords(OrderNumber, CustomerNumber, BillId);

                    //Add Binding costs
                    AddBindingCostsFromBilling(OrderNumber, CustomerNumber, BillId);

                    //Get Binder table measurements 
                    BinderTableRecords binderTableRecords = GetBinderTableMeasurements(binder);

                    //Height Width and Spine Costs
                    AddBillingSizeCosts(OrderNumber, CustomerNumber, BillId, binderTableRecords);

                    //Check if bill department is false - exit so duplicates are not sent
                    if (billByDept == false)
                        break;
                }
            }
            /**********************************************************************************************************
            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                int quantity = 0;
                int minutes = 1;
                string queryDepartment = "";

                if (billByDept == true)
                {
                    queryDepartment = row[8].ToString().Trim();
                }
                
                string product = row[7].ToString();
                string productCode = row[2].ToString().TrimEnd().ToUpper();

                int unitPrice = Int32.Parse(row[4].ToString());

                OrderNumber = row[0].ToString().TrimEnd();

                //If Leaf attachment and Product code is A change product code to 'NO'
                if ((product == "LEAF") && (productCode == "A"))
                {
                    productCode = "NO";
                }

                //Unit price needs to be greater than zero unless the product code is Notch
                //03-27-2018 All CLASS products need to be output even if the unit price is zero
                //02-26-2018 if ((unitPrice > 0 && (product == "LEAF" && productCode == "NO")) && (product != "EXTRA"))
                if ((unitPrice > 0 && (product != "EXTRA")) || (product == "CLASS"))
                {
                    //If bill by Department need to check that we are only counting the department for the current billing record
                    if ((billByDept == false) || ((billByDept == true) && (queryDepartment == Department)))
                    {
                        DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                        detailBillingRecord.RecordType = "DTL";
                        detailBillingRecord.OrderNumber = OrderNumber;
                        CustomerNumber = row[1].ToString().TrimEnd();
                        if (billByDept == true)
                        {
                            CustomerNumber = SubAccount.TrimEnd();
                            detailBillingRecord.CustomerNumber = CustomerNumber;
                        }
                        else
                            detailBillingRecord.CustomerNumber = CustomerNumber;

                        detailBillingRecord.ProductCode = productCode;
                        int copies = Int32.Parse(row[5].ToString());

                        quantity = Int32.Parse(row[3].ToString()) * copies * minutes;

                        //Only Add the total quantity for the CLASS product - Total Quantity is used for the HDR Record
                        if (product == "CLASS")
                            TotalJobQuantity += quantity;

                        detailBillingRecord.Quantity = quantity;
                        detailBillingRecord.UnitPrice = unitPrice;
                        detailBillingRecord.JobQuantity = "000000";
                        detailBillingRecord.Filler = "            ";

                        detailBillingRecordList.Add(detailBillingRecord);
                        logger.Info("Product: " + product + " ProductCode: " + productCode + " Quantity: " + quantity.ToString() + " OrderNumber: " + OrderNumber + " CustomerNumber: " + CustomerNumber);
                    }
                }
                else
                    logger.Info("Unit Price " +  unitPrice.ToString() +" for Product: " + product + " ProductCode: " + productCode + " Quantity: " + quantity.ToString() + " OrderNumber: " + OrderNumber + " CustomerNumber: " + CustomerNumber);
            }
            
            if (billByDept == false)
                BillDepartment = "TOTALS";
            else
                BillDepartment = Department;

            BillId = GetBillId(account, lotId, BillDepartment);
            //Get detail extras list
            AddDetailExtrasRecords(OrderNumber, CustomerNumber, BillId);

            //Add Binding costs
            AddBindingCostsFromBilling(OrderNumber, CustomerNumber, BillId);

            //Get Binder table measurements 
            BinderTableRecords binderTableRecords = GetBinderTableMeasurements(binder);

            //Height Width and Spine Costs
            AddBillingSizeCosts(OrderNumber, CustomerNumber, BillId, binderTableRecords);
             * *****************/
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="lotId"></param>
        /// <param name="BillDepartment"></param>
        /// <returns></returns>
        private static int GetBillId(string Account, string lotId, string BillDepartment)
        {
            int billId = 0;

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlConnection.Open();

                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetBillId";

                        sqlCommand.Parameters.Add("@Account", SqlDbType.VarChar).Value = Account.TrimEnd();
                        sqlCommand.Parameters.Add("@Lot", SqlDbType.VarChar).Value = lotId.TrimEnd();
                        sqlCommand.Parameters.Add("@BillDepartment", SqlDbType.VarChar).Value = BillDepartment.TrimEnd();

                        SqlParameter sqlBillIdParam = new SqlParameter("@BillId", SqlDbType.Int);
                        sqlBillIdParam.Direction = ParameterDirection.Output;

                        sqlCommand.Parameters.Add(sqlBillIdParam);

                        sqlCommand.ExecuteNonQuery();

                        billId = (int)sqlCommand.Parameters["@BillId"].Value;
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetBillId: " + ex.Message);
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
            }
            return billId;
        }

        private static string GetJobFromOrderNumber(string orderNumber)
        {
            string jobId = string.Empty;

            if (orderNumber.Length > 0)
            {
                string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        try
                        {
                            sqlConnection.Open();

                            sqlCommand.Connection = sqlConnection;
                            sqlCommand.CommandType = CommandType.StoredProcedure;
                            sqlCommand.CommandText = "sp_AbleAsaGetJobFromOrder";

                            sqlCommand.Parameters.Add("@OrderNumber", SqlDbType.VarChar).Value = orderNumber.TrimEnd();

                            logger.Info("sp_AbleAsaGetJobFromOrder parameter orderNumber: " + orderNumber);

                            SqlParameter sqlSubAccountParam = new SqlParameter("@JobId", SqlDbType.VarChar, 8);
                            sqlSubAccountParam.Direction = ParameterDirection.Output;

                            sqlCommand.Parameters.Add(sqlSubAccountParam);

                            sqlCommand.ExecuteNonQuery();

                            jobId = (string)sqlCommand.Parameters["@JobId"].Value;
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Exception in sp_AbleAsaGetJobFromOrder: " + ex.Message);
                        }
                        finally
                        {
                            sqlConnection.Close();
                        }
                    }
                }
            }
            else
                logger.Info("OrderNumber is empty");

            return jobId;
        }

        private static string GetSubAccount(string account, string department)
        {
            string subAccount = string.Empty;
             
            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlConnection.Open();

                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetSubAccount";

                        sqlCommand.Parameters.Add("@Account", SqlDbType.VarChar).Value = account.TrimEnd();
                        sqlCommand.Parameters.Add("@Department", SqlDbType.VarChar).Value = department.TrimEnd();

                        SqlParameter sqlSubAccountParam = new SqlParameter("@SubAccount", SqlDbType.VarChar, 8);
                        sqlSubAccountParam.Direction = ParameterDirection.Output;

                        sqlCommand.Parameters.Add(sqlSubAccountParam);

                        sqlCommand.ExecuteNonQuery();

                        subAccount = (string)sqlCommand.Parameters["@SubAccount"].Value;
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetSubAccount: " + ex.Message);
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
            }
      
            return subAccount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="binder"></param>
        /// <returns></returns>
        private static BinderTableRecords GetBinderTableMeasurements(string binder)
        {
            DataTable dataTable = new DataTable();
            BinderTableRecords binderTableRecord = null;

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetBinderTableMeasurements";
                        sqlCommand.Parameters.Add("@Binder", SqlDbType.Char).Value = binder;

                        logger.Info("call sp_AbleAsaGetBinderTableMeasurements stored procedure ");

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetBinderTableMeasurements: " + ex.Message);
                    }
                }
            }

            foreach (DataRow row in dataTable.Rows)
            {
                int spineWidth = 0;
                int boardHeight = 0; 
                int boardWidth = 0;
              
                Int32.TryParse(row[0].ToString(), out spineWidth);
                Int32.TryParse(row[1].ToString(), out boardHeight);
                Int32.TryParse(row[2].ToString(), out boardWidth);

                binderTableRecord = new BinderTableRecords();
                binderTableRecord.MaxBoardHeight = boardHeight;
                binderTableRecord.MaxBoardWidth = boardWidth;
                binderTableRecord.MaxSpineWidth = spineWidth;
            }

            return binderTableRecord;
        }

        private static void AddBillingSizeCosts(string OrderNumber, string CustomerNumber, int BillId, BinderTableRecords binderTableRecords)
        {
            DataTable dataTable = new DataTable();

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetSizeDtlBillingRecords";
                        sqlCommand.Parameters.Add("@BillId", SqlDbType.Int).Value = BillId;

                        logger.Info("call sp_AbleAsaGetSizeDtlBillingRecords stored procedure ");

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetSizeDtlBillingRecords: " + ex.Message);
                    }
                }
            }

            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                int unitPrice = 0;
                int volumes = 1;
                int size = 1;
                int price = 1;
                int increment = 0;

                string productCode="";
                int productCodeId = 0;

                Int32.TryParse(row[0].ToString(), out size);
                Int32.TryParse(row[1].ToString(), out volumes);
                Int32.TryParse(row[2].ToString(), out price);
                Int32.TryParse(row[3].ToString(), out increment);

                unitPrice = price/volumes;

                if (unitPrice > 0)
                {
                    DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                    detailBillingRecord.RecordType = "DTL";
                    detailBillingRecord.OrderNumber = OrderNumber;
                    detailBillingRecord.CustomerNumber = CustomerNumber;

                    if (size == 1)
                    {
                        productCode = "H";
                        productCodeId = binderTableRecords.MaxBoardHeight/640;
                        productCodeId = productCodeId + increment - 1;
                    }
                    else if (size == 2)
                    {
                        productCode = "W";
                        productCodeId = binderTableRecords.MaxBoardWidth/640;
                        productCodeId = productCodeId + increment;
                    }
                    else
                    {
                        productCode = "T";
                        productCodeId = binderTableRecords.MaxSpineWidth/640;
                        productCodeId = productCodeId + increment - 1;
                    }

                    

                    if (productCodeId.ToString().Length == 2)
                    {
                        productCode += productCodeId.ToString();
                        productCode += "0";
                    }
                    else
                    {
                        productCode += "0";
                        productCode += productCodeId.ToString();
                        productCode += "0";
                    }

                    detailBillingRecord.ProductCode = productCode;//productCode;
                    detailBillingRecord.Quantity = volumes;
                    detailBillingRecord.UnitPrice = unitPrice; //unitPrice;
                    detailBillingRecord.JobQuantity = "000000";
                    detailBillingRecord.Filler = "            ";

                    logger.Info("Add Size Charge: " + " Order Number:" + OrderNumber + " CustomerNumber:" + CustomerNumber + 
                                " ProductCode:" + detailBillingRecord.ProductCode + " Quantity:" + volumes + " Unit Price:" + unitPrice);

                    detailBillingRecordList.Add(detailBillingRecord);
                }
                else
                    logger.Info("Add Size Charge: " + " Order Number:" + OrderNumber + " CustomerNumber:" + CustomerNumber + " ProductCode:" + productCode + " Unit Price: 0");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="OrderNumber"></param>
        /// <param name="CustomerNumber"></param>
        private static void AddDetailExtrasRecords(string OrderNumber, string CustomerNumber, int BillId)
        {
            DataTable dataTable = new DataTable();

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetExtraDtlBillingRecords";
                        sqlCommand.Parameters.Add("@BillId", SqlDbType.Int).Value = BillId;

                        logger.Info("call sp_AbleAsaGetExtraDtlBillingRecords stored procedure ");

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetExtraDtlBillingRecords: " + ex.Message);
                    }
                }
            }

            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                int unitPrice = 0;
                int volumes = 1;
                int minutes = 0;
                int quantity = 0;

                Int32.TryParse(row[3].ToString(), out unitPrice);
                Int32.TryParse(row[1].ToString(), out volumes);
                Int32.TryParse(row[2].ToString(), out minutes);

                if (minutes == 0)
                    quantity = volumes;
                else
                    quantity = minutes;

                if (unitPrice > 0)
                {
                    DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                    detailBillingRecord.RecordType = "DTL";
                    detailBillingRecord.OrderNumber = OrderNumber;
                    detailBillingRecord.CustomerNumber = CustomerNumber;

                    detailBillingRecord.ProductCode = row[0].ToString();//productCode;
                    detailBillingRecord.Quantity = quantity;
                    detailBillingRecord.UnitPrice = unitPrice; //unitPrice;
                    detailBillingRecord.JobQuantity = "000000";
                    detailBillingRecord.Filler = "            ";

                    logger.Info("Add Extra: "+ " Order Number:"+OrderNumber+" CustomerNumber:"+ CustomerNumber+ " ProductCode:"+detailBillingRecord.ProductCode+
                                " Quantity:"+detailBillingRecord.Quantity+" Unit Price:"+detailBillingRecord.UnitPrice);
                    
                    detailBillingRecordList.Add(detailBillingRecord);
                }
                else
                    logger.Info("Add Extra: " + " Order Number:" + OrderNumber + " CustomerNumber:" + CustomerNumber + " ProductCode:" + row[0].ToString() + " Unit Price: 0");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OrderNumber"></param>
        /// <param name="CustomerNumber"></param>
        /// <param name="BillId"></param>
        private static void AddBindingCostsFromBilling(string OrderNumber, string CustomerNumber, int BillId)
        {
            DataTable dataTable = new DataTable();
            int unitPrice = 0;
            int volumes = 0;

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaGetBindingCostsInBilling";
                        sqlCommand.Parameters.Add("@BillId", SqlDbType.Int).Value = BillId;

                        logger.Info("call sp_AbleAsaGetBindingCostsInBilling stored procedure ");

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaGetBindingCostsInBilling: " + ex.Message);
                    }
                }
            }

            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                Int32.TryParse(row[0].ToString(), out volumes);
                Int32.TryParse(row[1].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[0], unitPrice, volumes);

                Int32.TryParse(row[2].ToString(), out volumes);
                Int32.TryParse(row[3].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[1], unitPrice, volumes);

                Int32.TryParse(row[4].ToString(), out volumes);
                Int32.TryParse(row[5].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[2], unitPrice, volumes);

                Int32.TryParse(row[6].ToString(), out volumes);
                Int32.TryParse(row[7].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[3], unitPrice, volumes);

                Int32.TryParse(row[8].ToString(), out volumes);
                Int32.TryParse(row[9].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[4], unitPrice, volumes);

                Int32.TryParse(row[10].ToString(), out volumes);
                Int32.TryParse(row[11].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[5], unitPrice, volumes);

                Int32.TryParse(row[12].ToString(), out volumes);
                Int32.TryParse(row[13].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[6], unitPrice, volumes);

                Int32.TryParse(row[14].ToString(), out volumes);
                Int32.TryParse(row[15].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[7], unitPrice, volumes);

                Int32.TryParse(row[16].ToString(), out volumes);
                Int32.TryParse(row[17].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[8], unitPrice, volumes);

                Int32.TryParse(row[18].ToString(), out volumes);
                Int32.TryParse(row[19].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[9], unitPrice, volumes);

                Int32.TryParse(row[20].ToString(), out volumes);
                Int32.TryParse(row[21].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[10], unitPrice, volumes);

                Int32.TryParse(row[22].ToString(), out volumes);
                Int32.TryParse(row[23].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[11], unitPrice, volumes);

                Int32.TryParse(row[24].ToString(), out volumes);
                Int32.TryParse(row[25].ToString(), out unitPrice);
                AddBillingCostRecord(OrderNumber, CustomerNumber, BillingCodes[12], unitPrice, volumes);
            }
        }

        private static void AddBillingCostRecord(string OrderNumber, string CustomerNumber, string BillingCodes, int unitPrice, int volumes)
        {
            if (unitPrice > 0)
            {
                unitPrice = volumes > 0 ? unitPrice / volumes : unitPrice;//Fix for unit Cost multiply by volumes 11-18-2017
                DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                detailBillingRecord.RecordType = "DTL";
                detailBillingRecord.OrderNumber = OrderNumber;
                detailBillingRecord.CustomerNumber = CustomerNumber;

                detailBillingRecord.ProductCode = BillingCodes;//productCode;
                detailBillingRecord.Quantity = volumes;
                detailBillingRecord.UnitPrice = unitPrice; //unitPrice;
                detailBillingRecord.JobQuantity = "000000";
                detailBillingRecord.Filler = "            ";

                logger.Info("Add Billing Cost: " + " Order Number:" + OrderNumber + " CustomerNumber:" + CustomerNumber + " ProductCode:" + detailBillingRecord.ProductCode +
                                " Quantity:" + detailBillingRecord.Quantity + " Unit Price:" + detailBillingRecord.UnitPrice);
                detailBillingRecordList.Add(detailBillingRecord);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="JobId"></param>
        /// <param name="CustomerNumber"></param>
        /// <param name="TotalJobQuantity"></param>
        private static void AddJobHeaderRecord(string JobId, string CustomerNumber, int TotalJobQuantity)
        {
            if (TotalJobQuantity > 0)
            {
                DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                detailBillingRecord.RecordType = "HDR";
                detailBillingRecord.OrderNumber = JobId.TrimEnd();
                detailBillingRecord.CustomerNumber = "";
                detailBillingRecord.ProductCode = "";
                detailBillingRecord.Quantity = TotalJobQuantity;
                detailBillingRecord.UnitPrice = 0;
                detailBillingRecord.JobQuantity = "000000";
                detailBillingRecord.Filler = "            ";

                detailBillingRecordList.Add(detailBillingRecord);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OrderNumber"></param>
        /// <param name="customerNumber"></param>
        /// <param name="TotalJobQuantity"></param>
        private static void AddOrderHeaderRecord(string OrderNumber, string customerNumber, int TotalJobQuantity)
        {
            if (TotalJobQuantity > 0)
            {
                DetailBillingRecord detailBillingRecord = new DetailBillingRecord();

                detailBillingRecord.RecordType = "DTL";
                detailBillingRecord.OrderNumber = OrderNumber.TrimEnd();
                detailBillingRecord.CustomerNumber = customerNumber.TrimEnd();
                detailBillingRecord.ProductCode = "VPQ";
                detailBillingRecord.Quantity = TotalJobQuantity;
                detailBillingRecord.UnitPrice = 0;
                detailBillingRecord.JobQuantity = "000000";
                detailBillingRecord.Filler = "            ";

                detailBillingRecordList.Add(detailBillingRecord);
            }
        }

        private static int CalculateTotalJobQuanity(bool billByDept, string Department, DataTable dataTable)
        {
            int totalQuantity = 0;
             

            foreach (DataRow row in dataTable.Rows)
            {
                int quantity = 0;
                int minutes = 1;
                string queryDepartment = "";

                string product = row[7].ToString();

                if (billByDept == true)
                {
                    queryDepartment = row[8].ToString().Trim();
                }

                //If bill by Department need to check that we are only counting the department for the current billing record
                if ((billByDept == false) || ((billByDept == true) && (queryDepartment.ToUpper() == Department.ToUpper())))
                {
                    int copies = Int32.Parse(row[5].ToString());

                    //Check for extra to apply minutes
                    if ((product == "EXTRA") && (row[6].ToString().Trim().Length > 0))
                        minutes = Int32.Parse(row[6].ToString()); 

                    quantity = Int32.Parse(row[3].ToString()) * copies * minutes;

                    //Only Add the total quantity for the CLASS product - Total Quantity is used for the HDR Record
                    if (product == "CLASS")
                        totalQuantity += quantity;
                }
            }
            return totalQuantity;
        }

        public static void GetJobHeaderRecords(string binder, string jobId)
        {
            DataTable dataTable = new DataTable();

            string sqlConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            logger.Info("GetJobHeaderRecords Sql Connection String: " + sqlConnectionString);

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    try
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "sp_AbleAsaJobHeaderExport";
                        sqlCommand.Parameters.Add("@Binder", SqlDbType.VarChar).Value = binder;
                        sqlCommand.Parameters.Add("@LotId", SqlDbType.VarChar).Value = jobId;

                        using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            sqlDataAdapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception in sp_AbleAsaJobHeaderExport: " + ex.Message);
                    }
                }
            }


            //Process each row in the data table
            foreach (DataRow row in dataTable.Rows)
            {
                JobHeaderRecord jobHeaderRecord = new JobHeaderRecord();

                jobHeaderRecord.RecordType = "HDR";
                jobHeaderRecord.JobNumber       = row[0].ToString();
                jobHeaderRecord.CustomerNumber  = row[1].ToString();
                jobHeaderRecord.ProductCode     = row[2].ToString();
                jobHeaderRecord.Quantity = Int32.Parse(row[3].ToString());
                jobHeaderRecord.UnitPrice = Int32.Parse(row[4].ToString());
                jobHeaderRecord.JobQuantity = "000000";
                jobHeaderRecord.Filler = "            ";

                jobHeaderRecordList.Add(jobHeaderRecord);
            }
        }

    }


}
