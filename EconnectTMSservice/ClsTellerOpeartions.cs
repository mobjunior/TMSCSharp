using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsTellerOpeartions
    {
        public void Run(string IncomingMessage, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            string strCardNumber = "";
            string strDeviceid = "";
            string strExpiryDate = "";
            string strAccountNumber = "";
            string strResponse = "";
            string strAgentID = "";
            string strAmount = "";
            double amount;
            string field24 = "";
            string strTrack2Data = "";
            string strAgencyCashManagement = "";
            string[] strCardInformation;
            string strField35 = "";
            string strPinClear = "";
            string strVerifyPin = "";
            string strNarration = "";
            string strAgentCode = "";
            string strAgentPassword = "";
            string strField60 = "";
            string TransID = "";

            ClsEbankingconnections Clogic = new ClsEbankingconnections();
            Dictionary<string, string> data = new Dictionary<string, string>();
            Dictionary<string, string> data2 = new Dictionary<string, string>();
            Dictionary<string, string> where = new Dictionary<string, string>();
            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            Int32 id = 0;
            string HeadTellerID="";

            try
            {
                strAgencyCashManagement = strReceivedData[1];
                switch (strAgencyCashManagement)
                {
                    case "ACCEPTCASH":
                        //710000#ACCEPTCASH#0000011300031089#2000#00007#
                        //this is just a confirmation from the teller that wat he/she has is the same amount was allocated.
                        strAmount= strReceivedData[3];
                        strAgentID = strReceivedData[4].Replace("Ù", "").Trim();
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                            TransID = opps.GetTellerAllocatedAmount(strAgentID, strAmount);
                            
                            id = Int32.Parse(TransID);
                            if (id < 0)
                            {
                                //send back a failure response.
                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller Cash Accept           " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += " Amount passed does not exist   " + "#";
                                strResponse += " Please key in Correct Amount   " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            else
                            {
                                //amount is okey proceedto do the rest
                               

                                string TellerGL = Clogic.RunStringReturnStringValue("SELECT account FROM VW_AGENTDETAILS WHERE AgentNo='" + strAgentID + "'");
                                string HeadTellerId = Clogic.RunStringReturnStringValue("select TellerId from tbTellerDenominations where id='" + id + "'");
                                string HeadTellerGL = Clogic.RunStringReturnStringValue("SELECT account FROM VW_AGENTDETAILS WHERE AgentNo='" + HeadTellerId + "'");
                                string TotalAmount = Clogic.RunStringReturnStringValue("select TotalAmount from tbTellerDenominations where id='" + id + "'");

                                string FIELD102 = HeadTellerGL;
                                string FIELD103 = TellerGL;
                                string narration = "HEADTELLER TO TELLER TRANSFER " + HeadTellerId + "-" + strAgentID;

                                opps.spInsertPOSTransaction(intid, strCardNumber, "400000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", narration, "", "", strAgentID, FIELD102, FIELD103, "");

                                Guid myguid = new Guid(MessageGUID);

                                string strRequest = opps.GenerateXMLtoeConnect("00001", "0200", "123454343546432890123456", "400000", TotalAmount, "532", "", "", "", "", strAgentID, FIELD102, FIELD103, "POS", strAgentID, ref myguid, narration, "", "", id.ToString(),"", "");

                                main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                                //wait for the response
                            }
                        }

                        break;
                    case "OPENTILL":
                        //710000#OPENTILL#0000011300031089#00007#
                        strAgentID = strReceivedData[3].Replace("Ù", "").Trim();
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                            data.Clear();
                            data["TillOpen"] = "1";
                            data["TillOpenOn"] = opps.GetWorkingdate();
                            where["TellerId"] = strAgentID;
                            string sql = Clogic.UpdateString("tbTellerTill", data, where);

                            strNarration = "OPENTILL" + strAgentID;

                            opps.spInsertPOSTransaction(intid, "", "710000", "", "", "", "", "", "", "", "", strDeviceid, "", "", "", "", strNarration, "", "", strAgentID, "", "", "");

                            if (Clogic.RunNonQuery(sql))
                            {
                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller Open Till          " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += " Till Opened Successful        " + "#";
                                strResponse += "    " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                //update response
                                opps.fn_updatetbPOSTransactions("00", "Till Opened Successful ", "", "", "", MessageGUID, strResponse, intid);
                                return;
                            }
                            else
                            {//failed
                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller Opening Till          " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += " Till Opening Failed            " + "#";
                                strResponse += "    " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                //update response
                                opps.fn_updatetbPOSTransactions("05", "Till Opening Failed  ", "", "", "", MessageGUID, strResponse, intid);
                                return;
                            }
                        }
                        break;
                    case "REQUESTCASH":
                        //request for cash from headteller
                         strAmount = strReceivedData[3];
                        strAgentID = strReceivedData[4].Replace("Ù", "").Trim();
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        amount = double.Parse(strAmount);

                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;

                            //get the teller branch
                            string TellerBranch = Clogic.RunStringReturnStringValue("SELECT Branch FROM tbTellerUsers WHERE TellerId='" + strAgentID + "'");
                            HeadTellerID = Clogic.RunStringReturnStringValue("SELECT TellerId FROM tbTellerUsers WHERE TellerType='006' and Branch='" + TellerBranch + "'");

                            data["TellerId"] = strAgentID;
                            data["BranchCode"] = TellerBranch;
                            data["CurrencyCode"] = "RWF";
                            data["Amount"] = amount.ToString();
                            data["WorkingDate"] = opps.GetWorkingdate();
                            data["RequestedOn"] = DateTime.Now.Date.ToString();
                            data["HeadTellerId"] = HeadTellerID;
                            data["Accepted"] = "0";

                            //insert
                            strNarration = "TEller Cash Request :" + strAgentID;

                           string sql = Clogic.InsertString("tbTellerCashRequest", data);

                           opps.spInsertPOSTransaction(intid, "", "710000", "", "", "", "", "", "", "", "", strDeviceid, "", "", "", "", strNarration, "", "", strAgentID, "", "", "");

                           if (Clogic.RunNonQuery(sql))
                           {
                               string PhoneNumber = Clogic.RunStringReturnStringValue("select PhoneNumber from tbTellerUsers where UserName='" + HeadTellerID + "'");
                               string username = Clogic.RunStringReturnStringValue("select UserName from tbTellerUsers where UserName='" + HeadTellerID + "'");
                               string Fullname = Clogic.RunStringReturnStringValue("select TellerName from tbTellerUsers where UserName='" + HeadTellerID + "'");

                               string strmessage = "";
                               strmessage = "Dear " + username + ", please process " + strAgentID + " cash request.";
                               //send sms
                               opps.SendSMS(PhoneNumber, strmessage, "POS", intid, "");
                               //send responseto pos
                               //send back a failure response.
                               strResponse = opps.strResponseHeader(strReceivedData[2]);
                               strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                               strResponse += "--------------------------------" + "#";
                               strResponse += "   Teller Cash Request          " + "#";
                               strResponse += "--------------------------------" + "#";
                               strResponse += " Cash Request Successful        " + "#";
                               // strResponse += "    " + "#";
                               strResponse += opps.strResponseFooter();
                               main.SendPOSResponse(strResponse, MessageGUID);

                               //update response
                               opps.fn_updatetbPOSTransactions("00", "Cash Request Successful  ", "", "", "", MessageGUID, strResponse, intid);
                               return;

                           }
                           else
                           {
                               strResponse = opps.strResponseHeader(strReceivedData[2]);
                               strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                               strResponse += "--------------------------------" + "#";
                               strResponse += "   Teller Cash Request          " + "#";
                               strResponse += "--------------------------------" + "#";
                               strResponse += " Cash Request Failed        " + "#";
                               // strResponse += "    " + "#";
                               strResponse += opps.strResponseFooter();
                               main.SendPOSResponse(strResponse, MessageGUID);

                               //update response
                               opps.fn_updatetbPOSTransactions("05", "Cash Request Failed ", "", "", "", MessageGUID, strResponse, intid);

                               return;
                           }


                        }

                        break;
                    case "CLOSETILL":

                        //710000#ACCEPTCASH#0000011300031089#00007#
                        strAgentID = strReceivedData[3].Replace("Ù", "").Trim();
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                            data.Clear();
                            data["TillOpen"] = "0";
                            where["TellerId"] = strAgentID;
                            string sql = Clogic.UpdateString("tbTellerTill", data, where);

                            strNarration = "Teller Close Till" + strAgentID;

                            opps.spInsertPOSTransaction(intid, "", "710000", "", "", "", "", "", "", "", "", strDeviceid, "", "", "", "", strNarration, "", "", strAgentID, "", "", "");

                            if (Clogic.RunNonQuery(sql))
                            {
                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller Closing Till          " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += " Till Closed Successful        " + "#";
                                strResponse += "    " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                //update response
                                opps.fn_updatetbPOSTransactions("00", "Till Closed Successful  ", "", "", "", MessageGUID, strResponse, intid);

                                return;
                            }
                            else
                            {//failed
                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller Closing Till          " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += " Till Closing Failed            " + "#";
                                strResponse += "    " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);

                                //update response
                                opps.fn_updatetbPOSTransactions("05", "Cash Request Failed ", "", "", "", MessageGUID, strResponse, intid);

                                return;
                            }
                        }
                        break;
                    case "TOHEADTELLER":
                        //teller transfer money to head teller
                         strAmount = strReceivedData[3];
                        strAgentID = strReceivedData[4].Replace("Ù", "").Trim();
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        amount = double.Parse(strAmount);

                        

                        

                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;

                            string TellerBranch = Clogic.RunStringReturnStringValue("SELECT Branch FROM tbTellerUsers WHERE TellerId='" + strAgentID + "'");
                            HeadTellerID = Clogic.RunStringReturnStringValue("SELECT TellerId FROM tbTellerUsers WHERE TellerType='006' and Branch='" + TellerBranch + "'");
                            string TellerGL = Clogic.RunStringReturnStringValue("SELECT account FROM VW_AGENTDETAILS WHERE AgentNo='" + strAgentID + "'");

                            string HeadTellerGL = Clogic.RunStringReturnStringValue("SELECT account FROM VW_AGENTDETAILS WHERE AgentNo='" + HeadTellerID + "'");
                            bool TillStatus = opps.GetTellerTillstatus(strAgentID);
                               // Clogic.RunStringReturnStringValue("select distinct b.id,a.WorkingDate,a.ToTeller,a.ReferenceNo,a.CurrencyCode,a.FiveThousand,a.TwoThousand,a.OneThousand,a.FiveHundred,a.OneHundred,a.Fifty,a.TotalAmount, b.* from tbTellerDenominations a inner join tbTellerTill b on a.ToTellerID=b.TellerId where b.Active=1 and a.ToTellerID ='" + strAgentID + "' and b.TillOpen=1 and a.WorkingDate ='" + opps.GetWorkingdate() + "'");

                            if (TillStatus == false)
                            {

                                strResponse = opps.strResponseHeader(strReceivedData[2]);
                                strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Teller To HeadTeller         " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "     Till Not Open              " + "#";
                                strResponse += "  Please, Open your Till        " + "#";
                                //strResponse += "    " + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                //update response
                                opps.fn_updatetbPOSTransactions("05", "Till Not Open", "", "", "", MessageGUID, strResponse, intid);
                                return;
                            }

                            string FIELD102 =  TellerGL ;
                            string FIELD103 = HeadTellerGL;
                            string narration = "TELLER TO HEADTELLER  TRANSFER " + HeadTellerID + "-" + strAgentID;

                            opps.spInsertPOSTransaction(intid, "", "710000", "", "", "", "", "", "", "", "", strDeviceid, "", "", "", "", strNarration, "", "", strAgentID, "", "", "");

                            string TellerGLAc = Clogic.RunStringReturnStringValue("SELECT account FROM VW_AGENTDETAILS WHERE AgentNo='" + strAgentID + "'");
                            //
                           string TxnsAmount = opps.fn_GetAccountBalance(TellerGLAc);
                            //string TxnsAmount = Clogic.RunStringReturnStringValue("select GLBalance  from tbGLBalance where GLCode='" + TellerGLAc + "'");

                            //send transaction to econect
                            Guid myguid = new Guid(MessageGUID);

                            string strRequest = opps.GenerateXMLtoeConnect("00001", "0200", "123454343546432890123456", "400000", amount.ToString(), "536", "", "", "", "", strAgentID, FIELD102, FIELD103, "POS", strAgentID, ref myguid, narration, "", "", "", "", "");

                            main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                            
                            if (TxnsAmount == "")
                            {
                                TxnsAmount = "0";
                            }
                            if (TxnsAmount != "")
                            {
                                double TotalTxnsAmount = double.Parse(TxnsAmount.ToString());

                                double defficit = (amount - TotalTxnsAmount);

                                data2["Excess"] = defficit.ToString();
                            }
                            data2["ProCode"] = "400000";
                            data2["ReferenceNo"] = "400000" + strAgentID;
                            data2["MsgType"] = "0200";
                           // data["TellerName"] = (string)HttpContext.Session["username"];
                            data2["TransactionDate"] = DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss");
                            data2["WorkingDate"] = opps.GetWorkingdate();
                            data2["TellerId"] = strAgentID;
                           // data2["BranchCode"] = collection["BranchCode"];
                            data2["CurrencyCode"] = "RWF";
                            data2["FiveThousand"] = "0";
                            data2["TwoThousand"] =  "0";
                            data2["OneThousand"] = "0";
                            data2["FiveHundred"] = "0";
                            data2["TwoHundred"] = "0";
                            data2["OneHundred"] = "0";
                            data2["FiftyCent"] = "0";
                            data2["Twenty"] = "0";
                            data2["Ten"] = "0";
                            data2["Five"] = "0";
                            data2["One"] = "0";
                            data2["TotalAmount"] = amount.ToString();
                            data2["ToTeller"] = "1";
                            data2["ToTellerID"] = HeadTellerID;
                            data2["TellerAccepted"] = "0";

                            //string sql = Clogic.InsertString("tbHeadTellerDenominations", data2);
                            //if (Clogic.RunNonQuery(sql))
                            //{
                            //     strResponse = opps.strResponseHeader(strReceivedData[2]);
                            //    strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                            //    strResponse += "---------------------------------" + "#";
                            //    strResponse += "   Teller To HeadTeller         " + "#";
                            //    strResponse += "---------------------------------" + "#";
                            //    strResponse += "Transfer to HeadTeller Successful" + "#";                                
                            //    //strResponse += "    " + "#";
                            //    strResponse += opps.strResponseFooter();
                            //    main.SendPOSResponse(strResponse, MessageGUID);

                            //    //update response
                            //    opps.fn_updatetbPOSTransactions("00", "Transfer to HeadTeller Successful", "", "", "", MessageGUID, strResponse, intid);
                            //    return;
                             
                            //}else
                            // {
                            //     //failed
                            //     strResponse = opps.strResponseHeader(strReceivedData[2]);
                            //    strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                            //    strResponse += "--------------------------------" + "#";
                            //    strResponse += "   Teller To HeadTeller         " + "#";
                            //    strResponse += "--------------------------------" + "#";
                            //    strResponse += "     Till Not Open              " + "#";
                            //    strResponse += " Transfer to HeadTeller Failed  " + "#";
                            //    strResponse += "    " + "#";
                            //    strResponse += opps.strResponseFooter();
                            //    main.SendPOSResponse(strResponse, MessageGUID);

                            //    //update response
                            //    opps.fn_updatetbPOSTransactions("00", "Transfer to HeadTeller Failed", "", "", "", MessageGUID, strResponse, intid);

                            //    return;
                            // }

                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "TellerOperations", "TellerOperations");
            }
        }
    }
}
