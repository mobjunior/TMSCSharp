using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.VisualBasic;
using System.Threading;
using System.Xml;
using System.Linq;
using System.Collections;
using System.IO;
using System.Timers;
 
namespace EconnectTMSservice
{
    public static class ResponseCodes_ISO87
    {

        public const string SUCCESS = "00";
        public const string CR_ACCOUNT_MISSING = "12";
        public const string INSUFFICIENT_FUNDS = "51";
        public const string DR_ACCOUNT_MISSING = "53";
        public const string DONOT_HONOR = "05";
        public const string NO_MATCHING_RECORD = "58";
        public const string EXPIRED_CODE = "59";
        public const string WRONG_AMOUNT = "60";
        public const string LIMIT_EXCEEDED = "61";
        public const string HOST_DOWN = "91";
        public const string FORMAT_ERROR = "30";

    }

    public enum Operation
    {
        Cash_withdrawal = 010000,
        Cash_Deposit = 210000,
        Cheque_Deposit = 240000,
        Balance = 310000,
        Agent_Float = 320000,
        Mini_Statement = 380000,
        Full_statement = 390000,
        Funds_Transfer = 400000,
        Cash_Request = 401000,
        Deposit_Request = 402000,
        Accept_Cash = 403000,
        Confirm_Deposit = 404000,
        Request_Excess_Cash = 405000,
        Shortage_Cash = 406000,
        Topup = 420000,
        Billpayments= 500000,
        Bill_Presentment = 510000,
        Loan_Repayment = 530000,
        Merchant = 540000,
        Cardless_Origination = 620000,
        Cardless_Fulfilment = 630000,
        Reprint = 999999,
        EOD_Report = 999990, 
        Password_Change = 999980,
        Login= 000000,
        Link_Account=700000,
        Teller_Opeartions=710000,
        NIDLookup=569980,
        Account_Opening= 569990,
        ReveralsRequest = 720000

    }
    public static class sharedvars
    {
        //Queues variables
        public static System.Collections.Queue InMessagesQueue = new System.Collections.Queue();
        public static System.Collections.Queue OutMessagesQueue = new System.Collections.Queue();
        public static System.Collections.Queue InMessagesQueue_FromCBS = new System.Collections.Queue();
        public static System.Collections.Queue queuetoeconnect = new System.Collections.Queue();

        //General
        public static Guid ListenerMainGuid;// = new Guid();

        //
        public static string appPath_IncExe = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string appPath = "";
        //
       public static string strField41  = ConfigurationManager.AppSettings["Card_Acceptor_Terminal_ID"];
       public static string strField42 = ConfigurationManager.AppSettings["Card_Acceptor_ID_Code"];
       public static string strField43  = ConfigurationManager.AppSettings["Card_Acceptor_Name_Location"];

         public static string strKeysForEncyption = ConfigurationSettings.AppSettings["pinverificationencyptionkeys"];
         public static string strCorrenetCardlessKey = ConfigurationManager.AppSettings["CorrenetCardlessKey"];
         public static string eConnectConnectionString = ConfigurationSettings.AppSettings["eConnectConnectionString"];
         public static string eBankConnectionString = ConfigurationManager.AppSettings["eBankConnectionString"];
         public static string Currencycode = ConfigurationManager.AppSettings["Currencycode"];
 
    }
    class ClsMain
    {
        private static Winsock_Orcas.WinsockCollection _wsks; // tms listener
        private static Winsock_Orcas.Winsock wsksockPIN;//pin verification
        private static Winsock_Orcas.Winsock wsksockEconnect; //connect to econnect
        private static Winsock_Orcas.Winsock wsktoReversal; //auto reversal to econnect
        //app config
        public static string TMSIP = ConfigurationManager.AppSettings["LocalIP"];
        public static string TMSport = ConfigurationManager.AppSettings["TMSport"];
        public static string eConnectBankingBPIPAddress = ConfigurationManager.AppSettings["eConnectBankingBPIPAddress"];
        public static string eConnectBankingBPPortNumber = ConfigurationManager.AppSettings["eConnectBankingBPPortNumber"];
        public static string eConnectBankingRVPortNumber = ConfigurationManager.AppSettings["eConnectBankingRVPortNumber"];
        public static string PINIP = ConfigurationManager.AppSettings["PINIP"];
        public static string PINport = ConfigurationManager.AppSettings["PINport"];
        public static string debugmode = ConfigurationManager.AppSettings["debugmode"];
        //
        static readonly object ErrorFilelocker = new object();
        static readonly object EnqueuelockerIn = new object();

        //timers
        private static System.Timers.Timer TimerMonitorConnections;

     //threads
        Thread Thread_main;
        Thread Thread_MainResponse;

        //data structures
        public static Dictionary<string, string> PINResults = new Dictionary<string, string>();
        public static Dictionary<string, string> messagesfrompos = new Dictionary<string, string>();
        
        public void Start()
        {
            try
            {

                sharedvars.appPath = sharedvars.appPath_IncExe.Substring(0, sharedvars.appPath_IncExe.LastIndexOf("\\"));

                //Tms listener
                _wsks = new Winsock_Orcas.WinsockCollection(true);
                // _wsks = new Winsock_Orcas.Winsock();
                _wsks.DataArrival += _wsks_DataArrival;
                _wsks.ConnectionRequest += _wsks_ConnectionRequest;
                _wsks.ErrorReceived += _wsks_SocketErrorReceived;
                _wsks.Connected += _wsks_Connected;
                _wsks.StateChanged += _wsks_StatusChange;

                // To econnect we the client
                wsksockEconnect = new Winsock_Orcas.Winsock();
                wsksockEconnect.LegacySupport = true;
                wsksockEconnect.DataArrival += wsksockEconnect_DataArrival;
                wsksockEconnect.ConnectionRequest += wsksockEconnect_ConnectionRequest;

                // To pin port  we the client
                wsksockPIN = new Winsock_Orcas.Winsock();
                wsksockPIN.LegacySupport = true;
                wsksockPIN.DataArrival += wsksockPIN_DataArrival;
                wsksockPIN.ConnectionRequest += wsksockPIN_ConnectionRequest;

                // to econect reversals

                wsktoReversal = new Winsock_Orcas.Winsock();
                wsktoReversal.LegacySupport = true;
                wsktoReversal.DataArrival += wsktoReversal_DataArrival;
                wsktoReversal.ConnectionRequest += wsktoReversal_ConnectionRequest;
                // start tms listener
                Connect_wsks();
                //econnect client
                Connect_wsksockEconnect();
                //pin client
                Connect_wsksockPIN();
                //auto reversa;

                Connect_wsktoReversal();

                //Start threads
                Thread_main = new Thread(new ThreadStart(ThreadMainProcessRun));
                Thread_main.Start();

                Thread_MainResponse = new Thread(new ThreadStart(ThreadtoEconnect));
                Thread_MainResponse.Start();


                //monitor conections
                TimerMonitorConnections = new System.Timers.Timer(2000); // Set up the timer for 3 seconds
                TimerMonitorConnections.Elapsed += new ElapsedEventHandler(_TimerMonitorThreads_Elapsed);
                TimerMonitorConnections.Enabled = true; // Enable it
                TimerMonitorConnections.Start(); 
            }
            catch (Exception ex)
            {

            }
        }
        private void _TimerMonitorThreads_Elapsed(object sender, ElapsedEventArgs e)
        {
            Connect_wsksockEconnect();
            //pin client
            Connect_wsksockPIN();
            //auto reversa;

            Connect_wsktoReversal();
        }
        public void Stop()
        {
            try
            {
                //abort threads
                if (Thread_MainResponse != null)
                {
                    Thread_MainResponse.Abort();                    
                }

                if (Thread_main != null)
                {
                    Thread_main.Abort();
                }

                try
                {
                    _wsks[sharedvars.ListenerMainGuid].Close();
                }
                catch (Exception e)
                {

                }
                wsksockPIN.Close();
                wsksockEconnect.Close();
                wsktoReversal.Close();



            }
            catch (Exception ex)
            {

            }
        }
        //thread to econnect processor
        private void ThreadtoEconnect()
        {
             Boolean Exitthread  = false;
            string message  = "";
            string[] Messagedata;           
            string MessageGUID = "";
            string Field37 = "";
            string[] strRecievedData;
            ClsSharedFunctions opp = new ClsSharedFunctions();
            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                    if (sharedvars.queuetoeconnect.Count > 0)
                    {
                        message =(string)sharedvars.queuetoeconnect.Dequeue();
                        strRecievedData = message.Split('|');
                        //strRecieveddata=DataStream + | + MessageGUID + | + strDeviceid + | field37
                        Guid myguid = new Guid(strRecievedData[1]);
                        Field37 = strRecievedData[3];



                        Messagedata = strRecievedData[0].Split('#');

                        MessageGUID = myguid.ToString();
                        Operation pcode = (Operation)Enum.Parse(typeof(Operation), Messagedata[0], true);
                        switch (pcode)
                        {
                            case Operation.Login:
                                ClsLogin login = new ClsLogin();
                                //USE A THREAD POOL MANAGED FOR YOU BY .NET to avoid eating all the resources
                                //the thread pool has a default max Threads on same app.. typically 50 etc 
                                //(string[] strReceivedData, string intid,string MessageGUID
                                login.Run(strRecievedData[0], Field37, MessageGUID);
                                break;
                            case Operation.Cash_withdrawal:
                                ClsCashwithdrawal withdrawal = new ClsCashwithdrawal();
                                withdrawal.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                             case Operation.Cash_Deposit:
                                 ClsCashDeposit clsdeposit = new ClsCashDeposit();
                                 clsdeposit.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                 break;
                            case Operation.Cheque_Deposit:
                                ClsCashDeposit deposit = new ClsCashDeposit();
                                deposit.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Balance:
                                ClsBalanceEnquiry bal = new ClsBalanceEnquiry();
                                bal.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Agent_Float:
                                ClsAgentFloat agentfoat = new ClsAgentFloat();
                                agentfoat.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Mini_Statement:
                                ClsMinistatement mini = new ClsMinistatement();
                                mini.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Funds_Transfer:
                                ClsFundsTransfer ft = new ClsFundsTransfer();
                                ft.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Cash_Request:
                                ClsCashRequest CashRe = new ClsCashRequest();
                                CashRe.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Deposit_Request:
                                ClsRequestFordeposit Redeposit = new ClsRequestFordeposit();
                                Redeposit.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Accept_Cash:
                                ClsAcceptCash accCash = new ClsAcceptCash();
                                accCash.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Confirm_Deposit:
                                ClsConfirmDeposit confdep = new ClsConfirmDeposit();
                                confdep.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Request_Excess_Cash:
                                ClsRequestexcesscash reexecesscash = new ClsRequestexcesscash();
                                reexecesscash.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Shortage_Cash:
                                ClsShortageCash shortcash = new ClsShortageCash();
                                shortcash.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Topup:
                                ClsMobileTopup topup = new ClsMobileTopup();
                                topup.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Loan_Repayment:
                                ClsLoanRepaymentEbank loan = new ClsLoanRepaymentEbank();
                                loan.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Billpayments:
                                ClsBillspayments bills = new ClsBillspayments();
                                bills.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Cardless_Origination:
                                ClsCardlessOrigination cardorig = new ClsCardlessOrigination();
                                cardorig.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Reprint:
                                ClsReprint reprint = new ClsReprint();
                                reprint.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.EOD_Report:
                                ClsAgentTellerEODTransactions eod = new ClsAgentTellerEODTransactions();
                                eod.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Password_Change:
                                ClsChangePassword pwdchage = new ClsChangePassword();
                                pwdchage.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Cardless_Fulfilment:
                                ClsCardlessFulfilment fulfil = new ClsCardlessFulfilment();
                                fulfil.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                            case Operation.Merchant:
                                ClsMerchant merchant = new ClsMerchant();
                                merchant.Run(strRecievedData[0], Field37, MessageGUID, Field37, true);
                                break;
                               
                            default:
                                string strResponse = "";

                                strResponse = "Transaction Code Not Defined#--------------------------------#";
                                strResponse += opp.strResponseFooter();
                                SendPOSResponse(strResponse, MessageGUID);
                                break;
                        }               
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        
        //Thread process Main
        private void ThreadMainProcessRun()
        {
            string message = "";
            string[] strReceivedData;
            string IncomingMessage = "";
            string MessageGUID = "";
            string[] Messagedata ;
            string intid ;

            ClsSharedFunctions opp = new ClsSharedFunctions();

            try
            {
                while (true)
                {
                    if (sharedvars.InMessagesQueue.Count > 0)
                    {
                        message = (string)sharedvars.InMessagesQueue.Dequeue();

                        strReceivedData = message.Split('|');

                        IncomingMessage = strReceivedData[0];
                        MessageGUID = strReceivedData[1];

                        Messagedata = IncomingMessage.Split('#');
                        //generate unique transaction number

                       intid = opp.GetRRNFRomregistry();


                         if (intid == "-1" )
                         {
                             SendPOSResponse("Error In transaction", MessageGUID);
                             continue;
                         }
                        //Account opening object used by pcodes
                         ClsAccountOpening acopening = new ClsAccountOpening();

                        Operation pcode = (Operation)Enum.Parse(typeof(Operation), Messagedata[0], true);
                         switch (pcode)
                         {
                             case Operation.Login:
                                 ClsLogin login = new ClsLogin();
                                 //USE A THREAD POOL MANAGED FOR YOU BY .NET to avoid eating all the resources
                                 //the thread pool has a default max Threads on same app.. typically 50 etc 
                                 //(string[] strReceivedData, string intid,string MessageGUID
                                 login.Run(IncomingMessage, intid, MessageGUID);
                                 break;
                             case Operation.Cash_withdrawal:
                                 ClsCashwithdrawal withdrawal = new ClsCashwithdrawal();
                                 withdrawal.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Cheque_Deposit:
                                 ClsCashDeposit deposit = new ClsCashDeposit();
                                 deposit.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Balance:
                                 ClsBalanceEnquiry bal = new ClsBalanceEnquiry();
                                 bal.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Agent_Float:
                                 ClsAgentFloat agentfoat = new ClsAgentFloat();
                                 agentfoat.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Mini_Statement:
                                 ClsMinistatement mini = new ClsMinistatement();
                                 mini.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Funds_Transfer:
                                 ClsFundsTransfer ft = new ClsFundsTransfer();
                                 ft.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Cash_Request:
                                 ClsCashRequest CashRe = new ClsCashRequest();
                                 CashRe.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Deposit_Request:
                                 ClsRequestFordeposit Redeposit = new ClsRequestFordeposit();
                                 Redeposit.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Accept_Cash:
                                 ClsAcceptCash accCash = new ClsAcceptCash();
                                 accCash.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Confirm_Deposit:
                                 ClsConfirmDeposit confdep = new ClsConfirmDeposit();
                                 confdep.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Loan_Repayment :
                                 ClsLoanRepaymentEbank loan = new ClsLoanRepaymentEbank();
                                 loan.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Request_Excess_Cash:
                                 ClsRequestexcesscash reexecesscash = new ClsRequestexcesscash();
                                 reexecesscash.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Shortage_Cash:
                                 ClsShortageCash shortcash = new ClsShortageCash();
                                 shortcash.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Topup:
                                 ClsMobileTopup topup = new ClsMobileTopup();
                                 topup.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Billpayments:
                                 ClsBillspayments bills = new ClsBillspayments();
                                 bills.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Cardless_Origination:
                                 ClsCardlessOrigination cardorig = new ClsCardlessOrigination();
                                 cardorig.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Reprint:
                                 ClsReprint reprint = new ClsReprint();
                                 reprint.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.EOD_Report:
                                 ClsAgentTellerEODTransactions eod = new ClsAgentTellerEODTransactions();
                                 eod.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Password_Change:
                                 ClsChangePassword pwdchage = new ClsChangePassword();
                                 pwdchage.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Cash_Deposit:
                                 ClsCashDeposit clsdeposit = new ClsCashDeposit();
                                 clsdeposit.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Cardless_Fulfilment:
                                 ClsCardlessFulfilment fulfil = new ClsCardlessFulfilment();
                                 fulfil.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Link_Account:
                                 ClsAccountlinking acclin = new ClsAccountlinking();
                                 acclin.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Teller_Opeartions:
                                 ClsTellerOpeartions telope = new ClsTellerOpeartions();
                                 telope.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.ReveralsRequest:
                                 ClsReversalRequests reversals = new ClsReversalRequests();
                                 reversals.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             case Operation.Merchant:
                                 ClsMerchant merchant = new ClsMerchant();
                                 merchant.Run(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID, intid.PadLeft(12, '0'), false);
                                 break;
                             //For account opening. First we do an NID check then if ok submit for registration
                             case Operation.NIDLookup:
                                 //ClsAccountOpening lookup = new ClsAccountOpening();
                                 acopening.LookUp(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID);
                                 break;
                             //Submit customer details for account opening
                             case Operation.Account_Opening:
                                // ClsAccountOpening acopening = new ClsAccountOpening();
                                 acopening.OpenAccount(IncomingMessage, intid.PadLeft(12, '0'), MessageGUID);
                                 break;
                             default :
                                 string strResponse="";

                                strResponse = "Transaction Code Not Defined#";
                                strResponse += "--------------------------------#";
                                strResponse += opp.strResponseFooter();
                                SendPOSResponse(strResponse, MessageGUID);
                                 break;
                         }
                    }
                }
            
            }
            catch (Exception ex)
            {
            LogErrorMessage_Ver1(ex, "ThreadMainProcessRun", "ThreadMainProcessRun");
            LogMessage("Error",ex.Message + Strings.Format(DateTime.Now, "dd-mm-yyy HH:mm:ss"));
            
            string strResponse  = "";

            strResponse = "Error on Transaction#";
            strResponse +="--------------------------------#";
            strResponse += opp.strResponseFooter();
         
            SendPOSResponse(strResponse, MessageGUID);
            LogMessage("MessagesToPOS", strResponse + "-" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss"));
            }
        }
        public void Tillnotoperesponse(string TellerID, string messageGUID, string strDeviceid, string intid)
        {
            string strResponse = "";
            ClsSharedFunctions opp = new ClsSharedFunctions();
            Guid guid = new Guid(messageGUID);

            strResponse = opp.strResponseHeader(strDeviceid);           
            strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
            strResponse += "--------------------------------" + "#";
            strResponse += "   Teller  Till                 " + "#";
            strResponse += "--------------------------------" + "#";
            strResponse += " Please Open Till               " + "#";
            strResponse += "#--------------------------------#";
            strResponse += opp.strResponseFooter();
            SendPOSResponse(strResponse, messageGUID);
        }
        public void SendPOSResponse(string outmessage,string messageGUID)
        {
            Guid guid = new Guid(messageGUID);
            try
            {
                _wsks[guid].LegacySupport = true;
                _wsks[guid].Send(outmessage);
                _wsks[guid].Dispose();
                LogMessage("MessagesToPOS", outmessage);

            }catch(Exception ex)
            {

            }
       
        }


        public void SendToEconnect(string Requestxml, string strField37, string messageGUID, string strDeviceid, string strPAN)
        {
            ClsEbankingconnections Clogic= new ClsEbankingconnections();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            

            string strResponse="";

            try
            {
                 if(wsksockEconnect.State == Winsock_Orcas.WinsockStates.Connected )
                 {
                                wsksockEconnect.LegacySupport = true;
                                wsksockEconnect.Send(Requestxml);
                                if (debugmode == "1")
                                {
                                    LogMessage("MessagesToeConnect", Requestxml + "-" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss"));
                                }
                                else
                                {
                                    //mask the pan
                                    //here let mask the pan
                                   /* string maskedpan = strPAN.Substring(6, 8);
                                    string strnewpan = strPAN;
                                    strnewpan = strnewpan.Replace(maskedpan, "xxxxxxxx");
                                    //replace in the string
                                    Requestxml = Requestxml.Replace(strPAN, strnewpan);*/
                                    LogMessage("MessagesToeConnect", Requestxml + "-" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss"));
                                }
                                string strInMsg  = "update tbincomingPosTransactions set request_time='" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff") + "',request_to_econnect='" + Requestxml + "' where field_0='" + strField37 + "'";
                                Clogic.RunNonQuery(strInMsg);
                 }
                  else
                 {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Connection Error#";
                                strResponse += opps.strResponseFooter();
                               

                                SendPOSResponse(strResponse, messageGUID);
                 }
            }
            catch (Exception ex)
            {

            }
        }
        private void EnqueueIncomingMessages(string messagetoqueue)
        {
            lock (EnqueuelockerIn)
            {
                if (string.IsNullOrEmpty(messagetoqueue) != true)
                {
                    sharedvars.InMessagesQueue.Enqueue(messagetoqueue);
                }
            }
        }
        private void _wsks_DataArrival(object sender, Winsock_Orcas.WinsockDataArrivalEventArgs e)
        {
            try
            {
                Guid gid = _wsks.findGID((Winsock_Orcas.Winsock)sender);
                if (gid.ToString() == "00000000-0000-0000-0000-000000000000")
                    return;

                //string msg = (string)_wsks[gid].Get();
                string DataRxd = _wsks[gid].Get<string>();
                string[] logmessage;
                logmessage = DataRxd.Split('#');
                //if not on debug mode only log the first two items not expose customer pin numbers
                if (debugmode == "1")
                {
                    LogMessage("From_POS.log", DataRxd);
                }
                else
                {
                   //LogMessage("From_POS_AllMsg.log", logmessage[0] + " " + DataRxd);
                    LogMessage("From_POS.log", logmessage[0] + " " + logmessage[1]);
                }
                

                if (String.IsNullOrEmpty(DataRxd) == false)
                {
                    EnqueueIncomingMessages(DataRxd + "|" + gid.ToString());
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void _wsks_StatusChange(object sender, Winsock_Orcas.WinsockStateChangedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void _wsks_SocketErrorReceived(object sender, Winsock_Orcas.WinsockErrorReceivedEventArgs e)
        {
            string error = "";
            try
            {
                //Guid gid = _wsks.findGID((Winsock_Orcas.Winsock)sender);
                //if (gid.ToString() == "00000000-0000-0000-0000-000000000000")
                //    return;
                //_wsks[gid].Dispose();

                error = e.Message;

               // LogMessage("SocketErrorLOG", error + " " + "_wsks_SocketErrorReceived");

            }
            catch (Exception ex)
            {
                //LogErrorMessage_Ver1(ex, "_wsks_SocketErrorReceived");
            }
        }

        private void _wsks_ConnectionRequest(object sender, Winsock_Orcas.WinsockConnectionRequestEventArgs e)
        {
            try
            {


                Winsock_Orcas.Winsock sck = new Winsock_Orcas.Winsock();
                Guid myguid = _wsks.Add(sck);
                _wsks[myguid].Close();
                _wsks[myguid].LegacySupport = true;
                _wsks[myguid].Accept(e.Client);

                string rst = e.ClientIP;  
                ClsSharedFunctions opps = new ClsSharedFunctions();
                //let verify the ip addresses that come to only allocated ips

                //if (opps.fn_find_client(rst) == false)
                //{
                //    LogMessage("eAgency", "Not Authotized from " + rst);
                //    SendPOSResponse("Not Athorized", myguid.ToString());
                //    _wsks[myguid].Close();
                //}
            }
            catch (Exception ex)
            {
                //LogErrorMessage_Ver1(ex, "_wsks_MainCBS_SocketErrorReceived");
            }
        }

        private void _wsks_Connected(object sender, Winsock_Orcas.WinsockConnectedEventArgs e)
        {
            

        }

        //connect to econect
        private void wsksockEconnect_DataArrival(object sender, Winsock_Orcas.WinsockDataArrivalEventArgs e)
        {
            try
            {
                string DataRxd = wsksockEconnect.Get<string>();

                if (debugmode == "1")
                {
                    LogMessage("FromEconnectABC.log", DataRxd);
                }
                List<string> lstmsgs = new List<string>();
                lstmsgs = separateXMLMessage(DataRxd);
                //process messages
                for (int i = 0; i <= lstmsgs.Count - 1; i++)
                {
                    ProcessEconnectResponses(lstmsgs[i]);

                }


            }
            catch (Exception ex)
            {
                LogErrorMessage_Ver1(ex, "wsksockEconnect_DataArrival", "wsksockEconnect_DataArrival");
            }
        }
        private void wsksockEconnect_ConnectionRequest(object sender, Winsock_Orcas.WinsockConnectionRequestEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }
        private void wsksockPIN_DataArrival(object sender, Winsock_Orcas.WinsockDataArrivalEventArgs e)
        {
            try
            {
                string DataRxd = wsksockPIN.Get<string>();

                if (debugmode == "1")
                {
                    LogMessage("PINResults.log", DataRxd);
                }
               //pin response
                List<string> lstmsgs = new List<string>();
                lstmsgs=separateMessage_XML(DataRxd);

               for (int i = 0; i <= lstmsgs.Count - 1; i++)
               {
                    ProcessPINResponse(lstmsgs[i]);
               }

              
            }
            catch (Exception ex)
            {

            }
        }
        //
        private void wsktoReversal_DataArrival(object sender, Winsock_Orcas.WinsockDataArrivalEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }
        //Tillnot open response

       
        private void ProcessPINResponse(string msg)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            Dictionary<string, string> data2 = new Dictionary<string, string>();
            string strResponse = "";
            //Guid myguid = default(Guid);
            string MessageGUID = "";
            string strDeviceid = "";
            string TransactionID = "";
            string DataStream = "";
            ClsSharedFunctions opps = new ClsSharedFunctions();
            string strfield52="";

            data = opps.ExtractXMLFields(msg);
            if (debugmode == "1")
            {
                LogMessage("PINResultsFrom", msg);
            }
            else
            {

                //below while logg the pan masked
                data2 = data;
                string strPAN = data2["field2"];
                if (data2.ContainsKey("field52"))
                {
                    strfield52 = data2["field52"];
                }
                 
                //here let mask the pan
                string maskedpan = strPAN.Substring(6, 8);
                string strnewpan = strPAN;
                strnewpan = strnewpan.Replace(maskedpan, "xxxxxxxx");
                //replace in the string
                data2["field2"] = strnewpan;
                data2["field52"] = "***********";
                string xmlstring = opps.CreateXMLMessage(data2);
                xmlstring = xmlstring.Replace(strPAN, strnewpan);
                LogMessage("PINResultsFrom", xmlstring);
            }

            //check if 630000 route to econnect
            if (data.ContainsKey("field3"))
            {
                if (data["field3"] == "630000")
                {
                    //
                    LogMessage("CardelessFromCorrenet", msg);
                    if (data.ContainsKey("field39"))
                    {
                        //send the transaction to econnect
                        if (data.ContainsKey("field56"))
                        {
                            MessageGUID = data["field56"];
                        }

                        // pick the  GUID
                        if (data.ContainsKey("field56"))
                        {
                            MessageGUID = data["field56"];
                        }
                        if (data.ContainsKey("field41"))
                        {
                            strDeviceid = data["field41"];
                        }
                        if (data.ContainsKey("field11"))
                            TransactionID = data["field11"];

                        //if (winsockEconect.State == Winsock_Orcas.WinsockStates.Connected)
                        //{
                        //    wsCoreBankingConnection.LegacySupport = true;
                        //    wsCoreBankingConnection.Send(msg);
                        //   // Debug.Print(msg);
                        //    LogMessage("MessagesToeConnect", msg + "-" + Strings.Format(Now, "dd-MMM-yyyy HH:mm:ss"));
                        //    string strInMsg = "update tbincomingPosTransactions set request_time='" + Strings.Format(Now, "dd-MMM-yyyy HH:mm:ss.fff") + "',request_to_econnect='" + msg + "' where field_0='" + TransactionID + "'";
                        //    Clogic.RunNonQuery(strInMsg);
                        //}
                        //else
                        //{
                        //    strResponse = strResponseHeader(strDeviceid);
                        //    strResponse += "--------------------------------" + "#";
                        //    strResponse += "Connection Failure#";
                        //    strResponse += strResponseFooter("");

                        //    //_wsks.Item(myguid).LegacySupport = True
                        //    //Debug.Print(strResponse)
                        //    //_wsks.Item(myguid).Send(strResponse)
                        //    //_wsks.Item(myguid).Dispose()

                        //    SendPOSResponse(strResponse, myguid);
                        //}
                    }
                    //  PIN
                }
                else
                {

                    if (data.ContainsKey("field39"))
                    {
                        if (messagesfrompos.ContainsKey(data["field37"]))
                        {
                            //here check if the pin is correct or not 
                            //if correct send the transaction to econnect
                            //otherwise send to pos invalid pin     
                            //intid, strReceivedData + "|" + MessageGUID.ToString() + "|" + strDeviceid
                            string[] Strmsg = messagesfrompos[data["field37"]].Split('|');
                            if (Strmsg.Length > 0)
                            {
                                strDeviceid = Strmsg[2];
                                DataStream = Strmsg[0];
                                MessageGUID = Strmsg[1];
                            }


                            string fields39 = data["field39"];
                            if (fields39 == "00")
                            {
                                EconnectTMSservice.sharedvars.queuetoeconnect.Enqueue(messagesfrompos[data["field37"]] + "|" + data["field37"]);
                                messagesfrompos.Remove(messagesfrompos[data["field37"]]);
                            }
                            else
                            {
                                switch (fields39)
                                {
                                    case "55":
                                        strResponse =opps.strResponseHeader(strDeviceid);
                                        strResponse += "--------------------------------" + "#";                                        
                                        strResponse += opps.GetFailedPinCodeDescriptions(fields39) + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += opps.strResponseFooter("");
                                        opps.SPInsertFailedCardPINS(data["field2"], data["field39"], data["field37"]);
                                        SendPOSResponse(strResponse, MessageGUID);

                                        break;
                                   
                                    default:
                                        //log in the db the response from cr2 to track thecard issues
                                        opps.SPInsertFailedCardPINS(data["field2"], data["field39"], data["field37"]);
                                        strResponse = opps.strResponseHeader(strDeviceid);
                                        strResponse += "--------------------------------" + "#";                                       
                                        strResponse += opps.GetFailedPinCodeDescriptions(fields39) + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += opps.strResponseFooter("");

                                        SendPOSResponse(strResponse, MessageGUID);
                                        break;
                                }

                            }
                        }
                        else
                        {
                            LogMessage("PINRESULTFROMCR2_MISMATCH", "Reference number mismatch" + msg);
                        }
                    }
                    else
                    {
                        LogMessage("PINRESULTFROMCR2_MISMATCH", "NOFIELD39" + msg);
                        //PINResults(data("field37")) = data("field39")
                    }
                }
                //
            }
            else
            {

            }
        }

        private List<string> separateMessage_XML(string inmessage)
        {

            try
            {

                List<string> lstmsgs = new List<string>();
                int msglen = inmessage.Length;
                int start = inmessage.IndexOf("<?");

                string cutmsg = inmessage;
                //  string[] MessageX = inmessage.Split('^');
                //  string MessageGUID = "";

                while (msglen > 0)
                {
                    string tempmsg = "";

                    start = cutmsg.IndexOf("<?");
                    tempmsg = cutmsg.Substring(start, cutmsg.IndexOf("</message>") + 10);
                    //  msgs[k] = tempmsg;

                    if (tempmsg.Length < 680)
                    {
                        tempmsg = tempmsg;
                    }

                    lstmsgs.Add(tempmsg);
                    // k += 1;

                    cutmsg = cutmsg.Substring(tempmsg.Length, (cutmsg.Length - tempmsg.Length));
                    if (cutmsg.IndexOf("<?") > -1)
                    {
                        if (cutmsg.IndexOf("</message>") > -1)
                        {
                            cutmsg = cutmsg.Substring(cutmsg.IndexOf("<?"), (cutmsg.Length - cutmsg.IndexOf("<?")));
                            msglen = cutmsg.Length;
                        }
                        else
                        {
                            msglen = 0;
                        }

                    }
                    else
                    {
                        msglen = 0;
                    }


                }

                return lstmsgs;


            }
            catch (Exception ex)
            {
                LogErrorMessage_Ver1(ex, "separateMessage_XML");
                return null;
            }

        }
        private void wsksockPIN_ConnectionRequest(object sender, Winsock_Orcas.WinsockConnectionRequestEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void wsktoReversal_ConnectionRequest(object sender, Winsock_Orcas.WinsockConnectionRequestEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }
        
        private void Connect_wsks()
        {
            bool connectionOK = false;

            try
            {

                if ((sharedvars.ListenerMainGuid != null) && (sharedvars.ListenerMainGuid.ToString() != "00000000-0000-0000-0000-000000000000"))
                {

                    if (_wsks[sharedvars.ListenerMainGuid].State == Winsock_Orcas.WinsockStates.Connected)
                    {
                        connectionOK = true;
                    }
                    else if (_wsks[sharedvars.ListenerMainGuid].State == Winsock_Orcas.WinsockStates.Listening)
                    {
                        connectionOK = true;
                    }
                    else
                    {

                        connectionOK = false;


                    }

                }
                              

                if (sharedvars.ListenerMainGuid != null && sharedvars.ListenerMainGuid.ToString() == "00000000-0000-0000-0000-000000000000" || connectionOK != true)
                {
                    Winsock_Orcas.Winsock k = new Winsock_Orcas.Winsock();
                    sharedvars.ListenerMainGuid = _wsks.Add(k);                    
                    _wsks[sharedvars.ListenerMainGuid].LegacySupport = true;
                    _wsks[sharedvars.ListenerMainGuid].Listen(TMSIP, int.Parse(TMSport));
                  

                }


            }
            catch (Exception ex)
            {

              //  LogErrorMessage_Ver1(ex, "Connect_wsks");

            }


            //================================================================


        }

        private void Connect_wsksockPIN()
        {
            try
            {
                if (wsksockPIN.State != Winsock_Orcas.WinsockStates.Connected)
                {

                    wsksockPIN.Connect(PINIP, int.Parse(PINport));

                }
            }
            catch (Exception ex)
            {

            }
        }
        private void Connect_wsksockEconnect()
        {
            bool connectionOK = false;

            try
            {
                if (wsksockEconnect.State != Winsock_Orcas.WinsockStates.Connected)
                {

                    wsksockEconnect.Connect(eConnectBankingBPIPAddress, int.Parse(eConnectBankingBPPortNumber));

                }


            }
            catch (Exception ex)
            {

                LogErrorMessage_Ver1(ex, "wsksockAgencyBanking");
            }





        }


        private void Connect_wsktoReversal()
        {
            bool connectionOK = false;

            try
            {
                if (wsktoReversal.State != Winsock_Orcas.WinsockStates.Connected)
                {

                    wsktoReversal.Connect(eConnectBankingBPIPAddress, int.Parse(eConnectBankingRVPortNumber));

                }


            }
            catch (Exception ex)
            {

                LogErrorMessage_Ver1(ex, "wsktoReversal");
            }





        }
         public string PIN_Verify(string strProcessingCode,string strClearPIN,string strPAN ,string strExpiryDate, string strTerminalSerial,string strField35, string strField37 )
        {
            string response ="";
             string strField39 = "99" ;          
            string strField52  = "";
            string strField32  = "";
            try
            {
                ClsSharedFunctions opps= new ClsSharedFunctions();

                string strField41 = ConfigurationManager.AppSettings["Card_Acceptor_Terminal_ID"];
                string strField42 = ConfigurationManager.AppSettings["Card_Acceptor_ID_Code"];
                string strField43 = ConfigurationManager.AppSettings["Card_Acceptor_Name_Location"];

                 string  strField7  = Strings.Format(DateAndTime.Now, "MMddHHmmss");
                string  strField11  = strField37 ;

                strField11 = strField11.Substring(strField11.Length - 6, 6);

                string strField13  = Strings.Format(DateAndTime.Now, "MMdd");
                string strField15 = Strings.Format(DateAndTime.Now, "MMdd");
                string strField65  = "";
                string strField66  = "";

                // strKeysForEncyption = "E7F1E3B557A980F027AB4E0F4C77E5E5"
                SED3.cDesImplement des= new SED3.cDesImplement();

                // Dim des As New SED3.cDesImplement

                strField52 = des.getAnsiPinBlock(strPAN,sharedvars.strKeysForEncyption, strClearPIN);

                 Dictionary<string, string> data = new Dictionary<string, string>();

                data.Clear();
                data.Add("field0", "0200");
                data.Add("field3", "310000");
                data.Add("field2", strPAN);
                data.Add("field4", "0");
                data.Add("field7", strField7);
                data.Add("field11", strField11);

                data.Add("field13", strField13);
                data.Add("field14", strExpiryDate);
                data.Add("field15", strField15);
                data.Add("field35", strField35);
                data.Add("field32", strField32);
                data.Add("field37", strField37);
                data.Add("field41", strField41);
                data.Add("field42", strField42);

                data.Add("field43", strField43);
                data.Add("field52", strField52);
                //send to the pin port
                string requestXML  = opps.CreateXMLMessage(data);

            if(wsksockPIN.State == Winsock_Orcas.WinsockStates.Connected)
            {
                PINResults.Remove(strField37);
                PINResults.Add(strField37, "");
                wsksockPIN.Send(requestXML);
                if (debugmode == "1")
                {
                    LogMessage("PINMessageSent", requestXML);
                }
                else
                {
                    //here let mask the pan
                    string maskedpan = strPAN.Substring(6, 8);
                    string strnewpan = strPAN;
                    strnewpan = strnewpan.Replace(maskedpan, "xxxxxxxx");
                    //replace in the string
                    requestXML = requestXML.Replace(strPAN, strnewpan);
                    string pin = strField52;
                    //mask the pin too
                    requestXML = requestXML.Replace(strField52, "*****");

                    LogMessage("PINMessageSent", requestXML);
                }
            }
            else
            {
                PINResults.Remove(strField37);
                if (debugmode == "1")
                {
                    LogMessage("PINMessageNotSent", requestXML);
                }
                else
                {
                    //here let mask the pan
                    string maskedpan = strPAN.Substring(6, 8);
                    string strnewpan = strPAN;
                    strnewpan = strnewpan.Replace(maskedpan, "xxxxxxxx");
                    //replace in the string
                    requestXML = requestXML.Replace(strPAN, strnewpan);

                    LogMessage("PINMessageSent", requestXML);
                }
                return "91";
            }               
              
         

                return response;


            }catch(Exception ex)
            {
                return "";
            }
        }

         private List<string> separateXMLMessage(string inmessage)
         {

             try
             {

                 List<string> lstmsgs = new List<string>();
                 int msglen = inmessage.Length;
                 int start = inmessage.IndexOf("<?");

                 string cutmsg = inmessage;
                 //  string[] MessageX = inmessage.Split('^');
                 //  string MessageGUID = "";

                 while (msglen > 0)
                 {
                     string tempmsg = "";

                     start = cutmsg.IndexOf("<?");
                     tempmsg = cutmsg.Substring(start, cutmsg.IndexOf("</message>") + 10);
                     //  msgs[k] = tempmsg;

                     if (tempmsg.Length < 680)
                     {
                         tempmsg = tempmsg;
                     }

                     lstmsgs.Add(tempmsg);
                     // k += 1;

                     cutmsg = cutmsg.Substring(tempmsg.Length, (cutmsg.Length - tempmsg.Length));
                     if (cutmsg.IndexOf("<?") > -1)
                     {
                         if (cutmsg.IndexOf("</message>") > -1)
                         {
                             cutmsg = cutmsg.Substring(cutmsg.IndexOf("<?"), (cutmsg.Length - cutmsg.IndexOf("<?")));
                             msglen = cutmsg.Length;
                         }
                         else
                         {
                             msglen = 0;
                         }

                     }
                     else
                     {
                         msglen = 0;
                     }


                 }

                 return lstmsgs;


             }
             catch (Exception ex)
             {
                 LogErrorMessage_Ver1(ex, "separateMessage_XML");
                 return null;
             }

         }
        public static void LogErrorMessage_Ver1(Exception ex, string SourceModule, string AnyExtraDeatails = "")
        {
            string filename = "econnectError.log";
            string err = "";
            //  string specificerrror = "";
            try
            {

                lock (ErrorFilelocker)
                {
                    string path = sharedvars.appPath + "\\Logs\\" + DateTime.Today.ToString("dd MMM yyyy").ToString();
                    if (System.IO.Directory.Exists(path) != true)
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }

                    err = ex.Message + "\r\n" + ex.StackTrace + "\r\n" + AnyExtraDeatails + SourceModule + "\r\n" + DateTime.Now.ToString() + "\r\n";

                    System.IO.File.AppendAllText(path + "\\" + filename, err);

                }



            }
            catch (Exception x)
            {

            }
        }

        public static void LogMessage(string filename, string message)
        {

            try
            {

                lock (ErrorFilelocker)
                {
                    //string err = message +  "\r\n" + DateTime.Now.ToString();
                    string path = sharedvars.appPath + "\\Logs\\" + DateTime.Today.ToString("dd MMM yyyy").ToString();
                    if (System.IO.Directory.Exists(path) != true)
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }

                    System.IO.File.AppendAllText(path + "\\" + filename, DateTime.Now.ToString() + "\r\n" + message + "\r\n");



                }



            }
            catch (Exception ex)
            {
                // LogErrorMessage_Ver1(ex, "LogMessage", message);
            }
        }
        public Boolean SendToEconnectReversals(string strField11, string strField37) 
        {
        Boolean success = false;
        string xmltoFlex  = "";
        ClsEbankingconnections Clogic = new ClsEbankingconnections();
        try
        {
            if (wsktoReversal.State == Winsock_Orcas.WinsockStates.Connected)
            {
                //'get the transaction from tbIncomingPosTransactions
                string strSQL = "select request_to_econnect from tbIncomingPosTransactions where field_0='" + strField37 + "'";
                SqlDataReader rsReader   = Clogic.RunQueryReturnDataReader(strSQL);
                if(rsReader.HasRows)
                {
                    while(rsReader.Read())
                    {
                        xmltoFlex = rsReader["request_to_econnect"].ToString();
                    }
                    wsktoReversal.LegacySupport = true;
                    wsktoReversal.Send(xmltoFlex);
                    
                }
                
            }
            return true;
            }catch(Exception ex)
         {
            LogErrorMessage_Ver1(ex,"SendToEconnectReversals","SendToEconnectReversals");
            return false;
            }
       

        }

        private void ProcessEconnectResponses(string msg)
        {
            //Dim msg As String = ""
            string strResponse = "";
            string strAvailableBalance = "";
            string strActualBalance = "";
            string strReasonDescription = "";
            string strMessage = "";
            string strMessage1 = "";
            string cardlesssmsmsg = "";
            string strPhoneNumberToAlert = "";
            string strCustomerFirstName = "";
            string strProcessingCode = "";
            string intid = "";
            string strAccountNumber = "";
            string strAccountNumber2 = "";
            string strTransactionReferenceNo = "";
            string strField2 = "";
            string strField54 = "";
            string strField127 = "";
            string strField41 = "";
            string strField4 = "";
            string strField80 = "";
            string strField100 = "";
            string strField101 = "";
            string strField48 = "";
            string strField11 = "";
            string strField65 = "";
            string strField66 = "";
            string strField67 = "";
            string strField68 = "";
            string strField24 = "";
            string agentTeller = "";
            string strField58 = "";
            string strField37 = "";
            string strMiniAccount = "";
            string strfield6 = "";
            string strfield71 = "";
            string strfield74 = "";
            string strfield126 = "";
            string str514narration = "";
            Guid myguid = default(Guid);
            string strField39 = "";
            ClsSharedFunctions opps = new ClsSharedFunctions();
            ClsEbankingconnections Clogic = new ClsEbankingconnections();

            try
            {
                string xmlString = msg;


               
                XmlTextReader MyReader = new XmlTextReader(new StringReader(xmlString));
                MyReader.WhitespaceHandling = WhitespaceHandling.None;
                while (MyReader.Read())
                {
                    switch (MyReader.NodeType)
                    {
                        case System.Xml.XmlNodeType.Element:
                            if ((MyReader.HasAttributes))
                            {
                                string name = MyReader.Name.ToString();
                                string value = MyReader.Value.ToString();
                                string idtag = "";
                                string idvalue = "";
                                if ((MyReader.AttributeCount > 1))
                                {
                                    idtag = MyReader.GetAttribute(0);
                                    idvalue = MyReader.GetAttribute(1);

                                    switch (idtag)
                                    {
                                        case "2":
                                            strField2 = idvalue;
                                            break;
                                        case "3":
                                            strProcessingCode = idvalue;
                                            break;
                                        case "4":
                                            strField4 = idvalue;
                                            break;
                                        case "11":
                                            intid = idvalue;
                                            break;
                                        case "24":
                                            strField24 = idvalue;
                                            break;
                                        case "37":
                                            strField37 = idvalue;
                                            break;
                                        case "39":
                                            strField39 = idvalue;
                                            break;
                                        case "41":
                                            strField41 = idvalue;
                                            break;
                                        case "48":
                                            strField48 = idvalue;
                                            break;
                                        case "58":
                                            strField58 = idvalue;
                                            break;
                                        case "65":
                                            strField65 = idvalue;
                                            break;
                                        case "66":
                                            strField66 = idvalue;
                                            break;
                                        case "67":
                                            strField67 = idvalue;
                                            break;
                                        case "68":
                                            strField68 = idvalue;
                                            break;
                                        case "54":
                                            strField54 = idvalue;
                                            break;
                                        case "80":
                                            strField80 = idvalue;
                                            break;
                                        case "100":
                                            strField100 = idvalue;
                                            break;
                                        case "101":
                                            strField101 = idvalue;
                                            break;
                                        case "127":
                                            strField127 = idvalue;
                                            break;
                                        case "102":
                                            strAccountNumber = idvalue;
                                            break;                                      
                                        case "103":
                                            strAccountNumber2 = idvalue;
                                                break;
                                        case "123":
                                            str514narration =idvalue;
                                            break;
                                        case "129":
                                            strMiniAccount = idvalue;
                                            break;
                                        case "56":
                                            myguid = new Guid(idvalue);
                                            break;
                                        case "6":
                                            strfield6 = idvalue;
                                            break;
                                        case "126":
                                            strfield126 = idvalue;
                                            break;
                                        case "71":
                                            strfield71 = idvalue;
                                            break;
                                        case "74":
                                            strfield74 = idvalue;
                                            break;
                                    }

                                }

                                // Data.Add(name + idtag, idvalue)
                            }

                            break;
                    }

                }
                //julius lets extract t
                // log the message masking the pan
                if (debugmode == "0")
                {

                    //string strPAN = strField2;
                    ////here let mask the pan
                    //string maskedpan = strPAN.Substring(6, 8);
                    //string strnewpan = strPAN;
                    //strnewpan = strnewpan.Replace(maskedpan, "xxxxxxxx");
                    //msg= msg.Replace(strField2,strnewpan);
                    LogMessage("FromEconnectABC", msg);
                }
                //uncoment the below when goin live coz it is tied to a particular POS
                //strResponse = strResponseHeader(strField41)
                //JULIUS 
                if (!string.IsNullOrEmpty(strField101))
                {
                    strResponse = opps.strResponseagenetTeller(strField101);
                    if (strField101.StartsWith("A"))
                    {
                        agentTeller = "Agent " + strField101;
                    }
                    else
                    {
                        agentTeller = "Teller " + strField101;
                    }
                }
                else
                {
                    strResponse = opps.strResponseHeader(strField41);
                    // agentTeller = "Agent " & strField41
                    agentTeller = "Agent " + strField101;
                }

                //if the -10 then initiatea reversal

                if (strField39 == "-10")
                {
                    // SEND A REVERSAL TO ECONNECT
                    string strInTransactions = "update tbIncomingPosTransactions set field_39='" + strField39 + "', field_48='" + strField48 + "', field_80='" + strField80 + "', field_54='" + strField54 + "',field_127='" + strField127 + "', " + "field_56='" + myguid.ToString() + "',response_from_econnect='" + msg + "', pos_receipt='" + strResponse + "',response_time='" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff") + "'  where field_0='" + strField37 + "'";

                    bool blninupdate = Clogic.RunNonQuery(strInTransactions);
                    //here send a reversal to econnect for the transaction
                    SendToEconnectReversals(strField11, strField37);
                    strResponse += "Transaction Failed#--------------------------------#";
                    strResponse += opps.strResponseFooter();
                    SendPOSResponse(strResponse, myguid.ToString());
                    
                    // continue
                }
                else
                {
                    // BreakXML The Message And Get the Fields
                    //check if strProcessingCode is empty
                    if (string.IsNullOrEmpty(strProcessingCode))
                    {
                        strResponse = "Transaction Failed#--------------------------------#";
                        strResponse += opps.strResponseFooter();
                        SendPOSResponse(strResponse, myguid.ToString());
                        //_wsks.Item(myguid).LegacySupport = True
                        //Debug.Print(strResponse)
                        //_wsks.Item(myguid).Send(strResponse)

                        //_wsks.Item(myguid).Dispose()
                    }
                    else
                    {
                        switch (strProcessingCode)
                        {

                            case "010000":
                                // Cash Withdrawal
                                strResponse += "Auth ID:         " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")

                                switch (strField39)
                                {
                                    case "00":
                                        string[] strBal = strField54.Split('|');
                                        strAvailableBalance = strBal[0];
                                        strActualBalance = strBal[1];
                                        //check for field24 means supervisor deposits agent cash on request so the receipt changes
                                        if (strField24 != "514")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Cash Withdrawal        " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Withdrawal Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                            strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a cash withdrawal transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                        }
                                        else
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Deposit Confirmation        " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += str514narration;
                                            str514narration = "";
                                            strMessage = "Dear Customer, you have done a cash pickup of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";

                                            //update the tbmibrequest to mark the ticker closed
                                            bool succ = false;

                                            succ = opps.UpdateCashpickupCallnumber(strField66);
                                        }

                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "         Cash Withdrawal        " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";

                                        break;
                                }

                                // strMessage = "Dear Customer, you have done a cash withdrawal transaction of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & ". If you have any queries about this transaction, please call 0722980980"


                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "210000":
                                // Cash Deposit

                                strResponse += "Auth ID:            " + strField37 + "#";
                               
                                // strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        //check for field24 for the following scenario'
                                        //1.  cash collection 502
                                        //2.  Cash pickup 505
                                        //3   cash acceptance

                                        if (strField24 == "502")
                                        {
                                           // Dictionary<string, string> data = new Dictionary<string, string>();
                                            //data = opps.ExtractXMLFields(msg);

                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "          Cash Collection          " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Collection Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Account: " + strAccountNumber2 + "#"; 
                                            strResponse += "Name: " + opps.GetAccountName(strAccountNumber2) +"#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            //strResponse += "Avail.      " & Format(Val(strAvailableBalance), "##,###.00") & "#"
                                            //strResponse += "Actual.     " & Format(Val(strActualBalance), "##,###.00") & "#"
                                            strMessage = "Dear Customer, you have done a cash deposit transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                        }
                                        else if (strField24 == "533" || strField24 == "534")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         School Fees            " + "#";
                                            strResponse += "--------------------------------" + "#";                                            
                                            string schoolname = opps.GetSchoolname(strAccountNumber2);
                                            strResponse += "School Name  :" + schoolname + "#";
                                            strResponse += "Student No.  :" + strfield71 + "#";
                                            strResponse += "NARRATION :" + strfield74 + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "School Fees Successful" + "#";
                                           // strResponse += "--------------------------------" + "#";
                                        }
                                        else if (strField24 == "505")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "          Cash Pickup          " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Pickup Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a cash deposit transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                            //strResponse += "Avail.      " & Format(Val(strAvailableBalance), "##,###.00") & "#"
                                            //strResponse += "Actual.     " & Format(Val(strActualBalance), "##,###.00") & "#"
                                        }
                                        else if (strField24 == "509")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "          Cash Deposit          " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Deposit Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Account: " + strAccountNumber2 + "#"; 
                                            strResponse += "Name: " + opps.GetAccountName(strAccountNumber2) + "#"; 
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                           // strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                           // strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a cash deposit transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                        }
                                        else
                                        {
                                            //Dictionary<string, string> data = new Dictionary<string, string>();
                                            //data = opps.ExtractXMLFields(msg);
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "          Cash Deposit          " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Deposit Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Account: " + strAccountNumber2 + "#"; 
                                            strResponse += "Name: " + opps.GetAccountName(strAccountNumber2) + "#"; 
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            //strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                            //strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a cash deposit transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                        }

                                        break;
                                    default:
                                        //strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "          Cash Deposit          " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";

                                        break;
                                }

                                //strMessage = "Dear Customer, you have done a cash deposit transaction of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & ". If you have any queries about this transaction, please call 0722980980"


                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            case "530000"://loan repayments

                                strResponse += "Auth ID:            " + strField37 + "#";

                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Loan Repayment         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Loan Repayment Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                            strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a Loan Repayment transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";
                                  
                                        break;
                                    default:

                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "          Loan Repayment        " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }
                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;

                                //--------merchant------
                            case "540000"://Merchant purchases

                                strResponse += "Auth ID:            " + strField37 + "#";

                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "     Merchant Purchase         " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Merchant Purchase  Successful" + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                        strMessage = "Dear Customer, you have done a Purchase worth RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";

                                        break;
                                    default:

                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "       Merchant Purchase       " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }
                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                                //--------end merchant------


                            case "240000":
                                //Cheque Deposit...

                                strResponse += "Auth ID:          " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")

                                switch (strField39)
                                {
                                    case "00":

                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "         Cheque Deposit         " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Cheque Deposit Successful" + "#";
                                        strResponse += "--------------------------------" + "#";
                                        break;
                                    default:
                                        //strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "         Cheque Deposit         " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }


                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "310000":
                                //Balance

                                strResponse += "Auth ID:          " + strField37 + "#";
                                // strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                //moved the logic of fetching the error codes to a SP which is more effecient
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }



                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "    Balance Enquiry Details     " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;
                                    default:
                                        //strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "    Balance Enquiry Details     " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;

                                }

                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "320000":
                                // Agent balances
                                strResponse += "Auth ID:         " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")

                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "     Agent Balance Enquiry      " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Float Balance:   " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        strResponse += "Withdrawal Float:" + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "    Agent Balance Enquiry     " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;
                                }

                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "380000":
                                // Mini Statement

                                strResponse += "Auth ID:          " + strField37 + "#";
                                string[] strStatementData = strField127.Split('|');
                                string strStatementPrinting = "";

                                //29-Aug-12~217938560~INTERNAL TRF VIA FIDELITY VIRTUAL~0.2~DR~0|29-Aug-12~217938554~INTERNAL TRF VIA FIDELITY VIRTUAL~7~DR~0|29-Aug-12~217938524~INTERNAL TRF VIA FIDELITY VIRTUAL~4~DR~0|29-Aug-12~217938410~INTERNAL TRF VIA FIDELITY VIRTUAL~0.2~DR~0|
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        try
                                        {
                                            int x = 0;
                                            for (x = 0; x <= strStatementData.Length - 1; x++)
                                            {
                                                string[] strInternalStatement = strStatementData[x].Split('~');
                                                if (!string.IsNullOrEmpty(strInternalStatement[0].Trim()))
                                                {
                                                    strStatementPrinting = strStatementPrinting + strInternalStatement[0] + " " + strInternalStatement[3] + " " + strInternalStatement[4] + "#";
                                                }

                                            }

                                        }
                                        catch (Exception ex)
                                        {

                                        }

                                        strResponse += "--------------------------------" + "#";
                                        strResponse += " Ministatement Enquiry Details  " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += strStatementPrinting + "#";
                                        strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += " Ministatement Enquiry Details  " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        strResponse += opps.strResponseFooter();

                                        break;
                                }
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "390000":
                                // Full Statement Request??
                                strResponse = "Full Statement Request#";
                                strResponse += opps.strResponseFooter();

                                intid = (double.Parse(intid) + 1).ToString();
                                Clogic.RunNonQuery("update tbSequenceValues set NextMSGID='" + intid + "'");
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "400000":
                                // Funds Transfer??
                                strResponse += "Auth ID:          " + strField37 + "#";
                                // strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        //julius 02/01/214
                                        //accounting linking let separate the print out on the pos
                                        //field 24 being 531
                                        if (strField24 == "531")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Account Activation     " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += " Account Activation Successful  " + "#";
                                            strResponse += "--------------------------------" + "#";                                           
                                        
                                        }
                                        else if (strField24 == "535")
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         School Fees            " + "#";
                                            strResponse += "--------------------------------" + "#";                                           
                                            string schoolname = opps.GetSchoolname(strAccountNumber2);
                                            strResponse += "School Name  :" + schoolname + "#";
                                            strResponse += "Student No.  :" + strfield71 + "#";
                                            strResponse += "NARRATION :" + strfield74 + "#";
                                            
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "School Fees Successful" + "#";
                                           // strResponse += "--------------------------------" + "#";
                                           // strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                           // strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a funds transfer of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + " to " + strAccountNumber2 + ". If you have any queries about this transaction, please call 0722980980";
                                            strMessage1 = "Dear Customer, you have received funds transfer of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " from " + strAccountNumber2 + ". If you have any queries about this transaction, please call 0722980980";
                                    
                                        }
                                        else if (strField24 == "532")
                                        {
                                            //teller Accept cash
                                            Dictionary<string, string> data = new Dictionary<string, string>();
                                            Dictionary<string, string> where = new Dictionary<string, string>();

                                            data.Clear();
                                            data["TellerAccepted"] = "1";
                                            // data["reworked"] = "0";
                                            data["TellerAcceptedOn"] = DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss");
                                            // data["approvedby"] = (string)HttpContext.Session["username"];
                                            data["ReferenceNo"] = strField58;//minireference

                                            where["Id"] = strfield126;
                                            //update tbTellerdenomination
                                            string sql = Clogic.UpdateString("tbTellerDenominations", data, where);
                                            if (Clogic.RunNonQuery(sql))
                                            { }

                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "     Teller Cash Accept         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Cash Accept Aproved Successful " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";

                                        }
                                        else if (strField24 == "536")
                                        {
                                            
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "     Teller To HeadTeller         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Teller To HeadTeller Successful " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                        }
                                        else
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Funds Transfer         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Funds Transfer Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                            strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            strMessage = "Dear Customer, you have done a funds transfer of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + " to " + strAccountNumber2 + ". If you have any queries about this transaction, please call 0722980980";
                                            strMessage1 = "Dear Customer, you have received funds transfer of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " from " + strAccountNumber2 + ". If you have any queries about this transaction, please call 0722980980";
                                        }
                                        break;
                                    //here will put a case for Accountlinking using a field24 to be provided by kiche then print accordingly

                                    default:
                                        //strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "          Funds Transfer        " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }
                                //strMessage = "Dear Customer, you have done a funds transfer of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & " to " & strAccountNumber2 & ". If you have any queries about this transaction, please call 0722980980"

                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());
                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "420000":
                                // Mobile Topup??

                               

                                strResponse += "Auth ID:          " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        //check for walking customers, display the amount paid instead of account balances
                                        if (strField24 != "525" || strField24 != "511")
                                        {
                                            if (double.Parse(strField4) <= 1000)
                                            {
                                                strResponse  += "";
                                                break;
                                            }
                                            else
                                            {
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "         Mobile Topup           " + "#";
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "Mobile Topup Successful" + "#";
                                                strResponse += "Mobile Topup by " + strField65 + "#";
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                                strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            }
                                        }
                                        else
                                        {
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "         Mobile Topup           " + "#";
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "Mobile Topup Successful" + "#";
                                                strResponse += "Mobile Topup by " + strField65 + "#";
                                                strResponse += "--------------------------------" + "#";
                                                strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                                // strResponse += "Actual.     " & Format(Val(strActualBalance), "##,###.00") & "#"
                   
                                        }
                                        strMessage = "Dear Customer, you have done a Mobile Topup of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". If you have any queries about this transaction, please call 0722980980";

                                        if (double.Parse(strField4) <= 1000)
                                            strResponse = "";
                                            strResponse += "Mobile Topup Successful" + "#";
                                            strResponse += "--------------------------------" + "#";

                                        break;

                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "          Mobile Topup          " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Mobile Topup by " + strField65 + "#";
                                        //strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }

                                //strMessage = "Dear Customer, you have done a Mobile Topup of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & ". If you have any queries about this transaction, please call 0722980980"



                                strResponse += opps.strResponseFooter();

                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            case "500000":
                                //Bills Payments
                                strResponse += "Auth ID:            " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        //For ecg bills they want customer infor displayed on the receipt
                                        // julius ECG forward the transaction to ecg for crediting
                                        // then print only after response from  ECG
                                        //cash pwoer respnses
                                        

                                        if (strField100 == "ELEC" & strField24 == "508" || strField24 == "526")
                                        {
                                            //introduce a delay here to get response from pivot access
                                            int milliseconds = 15000;
                                            Thread.Sleep(milliseconds);
                                            string strToken = opps.Get_CashpowerResponse(strField37);
                                            string consname = opps.Get_Cashpoweruser(strField37);
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Bills Payments         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Bills Payments Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Name: " + consname + "#";
                                            strResponse += "Meter NO. " + strField65 + "#";
                                            strResponse += "Token     " + strToken + "#";
                                            strResponse += "Amount Paid .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                            strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                            // cate for ecg walking customers
                                        }
                                        else if (strField100 == "ELEC" & strField24 == "512")
                                        {
                                            //introduce a delay here to get response from pivot access
                                            int milliseconds = 15000;
                                            Thread.Sleep(milliseconds);
                                            string strToken = opps.Get_CashpowerResponse(strField37);
                                            string consname = opps.Get_Cashpoweruser(strField37);
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Bills Payments         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Bills Payments Successful" + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Name: " + consname + "#";
                                            strResponse += "Meter NO. " + strField65 + "#";
                                            strResponse += "Token     " + strToken + "#";    
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount Paid .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";

                                            //check for walking customers, display the amount paid instead of account balances
                                        }
                                        //else if (strField24 != "526")
                                        //{
                                        //    strResponse += "--------------------------------" + "#";
                                        //    strResponse += "         Bills Payments         " + "#";
                                        //    strResponse += "--------------------------------" + "#";
                                        //    strResponse += "Bills Payments Successful" + "#";
                                        //    strResponse += "--------------------------------" + "#";
                                        //    strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                        //    strResponse += "--------------------------------" + "#";
                                        //    strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        //    strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";
                                        //}
                                        else
                                        {
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "         Bills Payments         " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Bills Payments Successful       " + "#";
                                            strResponse += "--------------------------------" + "#";
                                            strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                            // strResponse += "Actual.     " & Format(Val(strActualBalance), "##,###.00") & "#"
                                        }
                                        strMessage = "Dear Customer, your " + strField100 + "Payments of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + ". to Bill Acct. No " + strField66 + " has been received. If you have any queries about this transaction, please call 0722980980";
                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "         Bills Payments         " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }

                                //•	Dear First Name of Customer, your ECG payment to Bill Acct No. xxxxxxxxxxxxx has been received. Transaction reference: xxxxxxxxxxxxxxx

                                //strMessage = "Dear Customer, your " & strField100 & "Payments of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & ". to Bill Acct. No " & strField66 & " has been received. If you have any queries about this transaction, please call 0722980980"


                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)


                            case "620000":
                                //Cardless Origination
                                strResponse += "Auth ID:           " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":

                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }



                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "      Cardless Origination      " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Cardless Transfer Successful    " + "#";
                                        strResponse += "--------------------------------" + "#";

                                        if (string.IsNullOrEmpty(strField80))
                                        {
                                        }
                                        else
                                        {
                                            strResponse += "    Your 3 digit code is " + strField80.Substring(0, 3) + "#";
                                        }

                                        strResponse += "Please send it to the Receipient" + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Avail.      " + Strings.Format(Conversion.Val(strAvailableBalance), "##,###.00") + "#";
                                        strResponse += "Actual.     " + Strings.Format(Conversion.Val(strActualBalance), "##,###.00") + "#";

                                        strMessage = "Dear Customer, you have done a Card-less transaction of RWF " + Strings.Format(Conversion.Val(strField4), "##,###.00") + " at " + agentTeller + " to " + strAccountNumber2 + ". If you have any queries about this transaction, please call 0722980980";
                                        cardlesssmsmsg = "Dear Customer, Your 3 digit code is " + strField80.Substring(0, 3) + ", kindly send this to the recipient";
                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "      Cardless Origination      " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }

                                //strMessage = "Dear Customer, you have done a Card-less transaction of RWF " & Format(Val(strField4), "##,###.00") & " at Agent " & strField41 & " to " & strAccountNumber2 & ". If you have any queries about this transaction, please call 0722980980"



                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)


                            case "630000":
                                //Cardless Fullfillment
                                strResponse += "Auth ID:            " + strField37 + "#";
                                //strReasonDescription = Blogic.RunStringReturnStringValue("select error_description from tbposerrorcodes where error_code='" & strField39 & "'")
                                switch (strField39)
                                {
                                    case "00":
                                        string[] strBal = strField54.Split('|');
                                        try
                                        {
                                            strAvailableBalance = strBal[0];

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        try
                                        {
                                            strActualBalance = strBal[1];

                                        }
                                        catch (Exception ex)
                                        {
                                        }



                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "      Cardless Fulfillment      " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Cardless Fulfillment Successful " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "Amount .      " + Strings.Format(Conversion.Val(strField4), "##,###.00") + "#";
                                        strResponse += "--------------------------------" + "#";

                                        break;
                                    default:
                                        // strReasonDescription = GetPOSErrorResponsecodes(strField39)
                                        strReasonDescription = opps.GetResponseCode(strField39);
                                        strResponse += "--------------------------------" + "#";
                                        strResponse += "      Cardless Fulfillment      " + "#";
                                        strResponse += "--------------------------------" + "#";
                                        // strResponse += strReasonDescription & "#"
                                        strResponse += strField48 + "#";
                                        break;
                                }


                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;
                            //_wsks.Item(myguid).LegacySupport = True
                            //Debug.Print(strResponse)
                            //_wsks.Item(myguid).Send(strResponse)

                            default:
                                strResponse = "Transaction Code Not Defined#--------------------------------#";
                                strResponse += opps.strResponseFooter();
                                SendPOSResponse(strResponse, myguid.ToString());

                                break;                          
                        }

                        //_wsks.Item(myguid).Dispose()

                        // Update The the DB With Field39 and Field

                        string strInTransactions = "update tbIncomingPosTransactions set field_39='" + strField39 + "', field_48='" + strField48 + "', field_80='" + strField80 + "', field_54='" + strField54 + "',field_127='" + strField127 + "', " + "field_56='" + myguid.ToString() + "',response_from_econnect='" + msg + "', pos_receipt='" + strResponse + "',response_time='" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff") + "'  where field_0='" + strField37 + "'";

                        bool blninupdate = Clogic.RunNonQuery(strInTransactions);

                        if (strProcessingCode == "210000")
                        {
                            strAccountNumber = strAccountNumber2;
                        }
                        if (strField39 == "00" & !string.IsNullOrEmpty(strMessage) & strAccountNumber.Length == 13)
                        {
                            strPhoneNumberToAlert = Clogic.RunStringReturnStringValue("select MobileNumber1 from tbcustomers where customerno='" + strAccountNumber.Substring(4, 7) + "'");
                            strCustomerFirstName = Clogic.RunStringReturnStringValue("select FullName from tbcustomers where customerno='" + strAccountNumber.Substring(4, 7) + "'");
                            strMessage = strMessage.Replace("Customer", strCustomerFirstName);
                            string strInsertMsg = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " + " values ('" + strPhoneNumberToAlert + "','" + strTransactionReferenceNo + "','" + strAccountNumber + "','" + strMessage + "','POS',0,0)";
                            bool blninmsg = Clogic.RunNonQuery(strInsertMsg);


                            if (strProcessingCode == "400000")
                            {
                                strPhoneNumberToAlert = Clogic.RunStringReturnStringValue("select MobileNumber1 from tbcustomers where customerno='" + strAccountNumber2.Substring(4, 7) + "'");
                                string strCustomerRecipient = Clogic.RunStringReturnStringValue("select FullName from tbcustomers where customerno='" + strAccountNumber2.Substring(4, 7) + "'");
                                strMessage1 = strMessage1.Replace("Customer", strCustomerRecipient);
                                strMessage1 = strMessage1.Replace(strAccountNumber2, strCustomerFirstName);
                                string strInsertMsg2 = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " + " values ('" + strPhoneNumberToAlert + "','" + strTransactionReferenceNo + "','" + strAccountNumber2 + "','" + strMessage1 + "','POS',0,0)";
                                bool blninmsg2 = Clogic.RunNonQuery(strInsertMsg2);
                            }

                            if (strProcessingCode == "620000")
                            {
                                strPhoneNumberToAlert = Clogic.RunStringReturnStringValue("select MobileNumber1 from tbcustomers where customerno='" + strAccountNumber.Substring(4, 7) + "'");
                                string strCustomerRecipient = Clogic.RunStringReturnStringValue("select FullName from tbcustomers where customerno='" + strAccountNumber.Substring(4, 7) + "'");
                                cardlesssmsmsg = cardlesssmsmsg.Replace("Customer", strCustomerRecipient);
                                cardlesssmsmsg = cardlesssmsmsg.Replace(strAccountNumber, strCustomerFirstName);
                                string strInsertMsg2 = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " + " values ('" + strPhoneNumberToAlert + "','" + strTransactionReferenceNo + "','" + strAccountNumber + "','" + cardlesssmsmsg + "','POS',0,0)";
                                bool blninmsg2 = Clogic.RunNonQuery(strInsertMsg2);
                            }
                          
                        }
                        else if (strField39 == "00" & !string.IsNullOrEmpty(strMessage) & strAccountNumber.Length < 13)
                        {
                            // this will cater for funds transfer to flex and mini customer should recieve an sms
                            string sqlst = "select MobileNumber1 from tbcustomers where customerno='" + strMiniAccount.Substring(4, 7) + "'";

                            strPhoneNumberToAlert = Clogic.RunStringReturnStringValue(sqlst);

                            string sqlname = "select FirstName from tbcustomers where customerno='" + strMiniAccount.Substring(4, 7) + "'";
                            strCustomerFirstName = Clogic.RunStringReturnStringValue(sqlname);

                            strMessage = strMessage.Replace("Customer", strCustomerFirstName);
                            string strInsertMsg = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " + " values ('" + strPhoneNumberToAlert + "','" + strTransactionReferenceNo + "','" + strMiniAccount + "','" + strMessage + "','POS',0,0)";
                            bool blninmsg = Clogic.RunNonQuery(strInsertMsg);


                            if (strProcessingCode == "400000")
                            {
                            }


                        }
                    }

                }
                // if the response is not -10

            }
            catch (Exception ex)
            {
               LogErrorMessage_Ver1 (ex, "ProcessEconnectResponses-" + msg);
            }

        }

    }
}
