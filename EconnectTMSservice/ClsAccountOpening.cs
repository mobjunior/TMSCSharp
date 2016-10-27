using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EconnectTMSservice.AccountOpeningRef;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace EconnectTMSservice
{
    class ClsAccountOpening
    {
        public string strIDNo = "";
        public string strPhoneNumber = "";
        
        IncomingTransactionClient cl = new IncomingTransactionClient();

        public void LookUp(string IncomingMessage, string intid, string MessageGUID)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            
            string strDeviceid = "";
            string strResponse = "";
            string strAgentID = "";
            string strAgencyCashManagement = "";
            string strSessionID = ""; //seession ID for NID lookup and Account Opening. i used id no.
            ClsEbankingconnections Clogic = new ClsEbankingconnections();

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            try
            {
                strAgencyCashManagement = strReceivedData[1];
                switch (strAgencyCashManagement)
                {
                    case "AGENCY":
                        strDeviceid = strReceivedData[2];
                        strAgentID = strReceivedData[5].Replace("Ù", "").Trim();

                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        strSessionID = strReceivedData[3];

                        if (strAgentID.Trim().Length == 5)
                        {
                            //if its a Teller block it coz it will be illegal
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "Illegal Teller Transaction#";
                            strResponse += opps.strResponseFooter(strDeviceid);

                            main.SendPOSResponse(strResponse, MessageGUID);
                            //Console.WriteLine(strResponse);
                            return;
                        }
                        else
                        {
                            strAgentID = "A" + strAgentID;
                        }

                        // invoke the NID lookup service VIA wsdl: http://41.186.47.25:8081/NIDRwanda_Test/Request/IncomingTransaction?wsdl
                        strIDNo = strReceivedData[3];
                        strPhoneNumber = "250"+ strReceivedData[4].Substring(1,9);

                        string lookupRes=cl.NIDRequest(strIDNo,strPhoneNumber,strSessionID,"POS");
                        //sample response =  <TranResponse>{session=1239, sex=m, channel=pos, phonenumber=250788731151, issuenumber=2, placeofbirth=rusizi, mothernames=mukabagenyi, civilstatus=s, authenticatedocumentresult=null, dateofissue=24/02/2016, soap:envelope=null, authenticatedocumentresponse=null, applicationnumber=03367905, fathernames=rubagumya, soap:body=null, cell=kagugu, dateofbirth=01/01/1984, status=00, forename=badesire, village=kadobogo, sector=kinyinya, surnames=rubagumya, documenttype=1, villageid=0102100305, documentnumber=1 1984 8 0014850 1 59, placeofissue=kinyinya / gasabo, province=umujyi wa kigali, district=gasabo}</TranResponse>

                        string lookupres1 = lookupRes.Replace("{", string.Empty);
                        string lookupres2 = lookupres1.Replace("}", string.Empty);
                        string[] resArray = lookupres2.Split(',');
                        if (resArray.Length < 12)
                        {
                            main.SendPOSResponse("01#Lookup Failed#Lookup Failed#", MessageGUID);
                            //Console.WriteLine(strResponse);
                        }
                        else
                        {
                            string[] fornamedata = resArray[18].Split('=');
                            string[] surnamesdata = resArray[21].Split('=');
                            string[] statusData = resArray[17].Split('=');
                            string forename = fornamedata[1];
                            string surnames = surnamesdata[1];
                            string status = statusData[1];
                            main.SendPOSResponse(status +"#" + forename + "#" + surnames + "#", MessageGUID);
                        }

                        break;
                }// end of switch




            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "AccountsOpening", "NIDLookUp");
            }
        }

        public void OpenAccount(string IncomingMessage, string intid, string MessageGUID)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            string strDeviceid = "";
            string strResponse = "";
            string strAgentID = "";
            string strAgencyCashManagement = "";
            string strFirstname = "";
            string strSecondname = "";
            string strPhonenumber = "";
            string strSessionID = ""; //seession ID for NID lookup and Account Opening. Here i used the idno.

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            try
            {
                strAgencyCashManagement = strReceivedData[1];
                switch (strAgencyCashManagement)
                {
                    case "AGENCY":
                        strDeviceid = strReceivedData[2];
                        strAgentID = strReceivedData[7].Replace("Ù", "").Trim();

                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        strSessionID = strReceivedData[3];
                        strFirstname = strReceivedData[5].Replace("FName:","").Trim();
                        strSecondname = strReceivedData[6].Replace("SName:","").Trim();
                        strPhonenumber = strReceivedData[4];

                        if (strAgentID.Trim().Length == 5)
                        {
                            //if its a Teller block it coz it will be illegal
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "Illegal Teller Transaction#";
                            strResponse += opps.strResponseFooter(strDeviceid);

                            main.SendPOSResponse(strResponse, MessageGUID);
                            return;
                        }
                        else
                        {
                            strAgentID = "A" + strAgentID;
                        }

                        // invoke the open account service VIA wsdl: http://41.186.47.25:8081/NIDRwanda_Test/Request/IncomingTransaction?wsdl

                        string responseCode="";
                        string responseMsg="";
                        try
                        {
                            string openAcres = cl.OpenAccount(strSessionID);
                            JObject t = JsonConvert.DeserializeObject<JObject>(openAcres);
                            responseCode = t.GetValue("responsecode").ToString();
                            responseMsg = t.GetValue("responsemsg").ToString();
                        }
                        catch (Exception ex)
                        {
                            EconnectTMSservice.ClsMain.LogMessage("Error", ex.Message);
                            main.SendPOSResponse("01#Account Opening Failedjson#", MessageGUID);
                            return;
                        }
                        
                        if (responseCode == "00")
                        {
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "ACCOUNT OPENING SUCCESSFUL#";
                            strResponse += "Name: " + strFirstname + " " + strSecondname + "#";
                            strResponse += "PhoneNumber: " + strPhonenumber + "#";
                            strResponse += "ACCOUNT: " + responseMsg + "#";
                            strResponse += opps.strResponseFooter();
                            main.SendPOSResponse(strResponse, MessageGUID);
                            
                        }
                        else if(responseCode =="02")
                        {
                            main.SendPOSResponse("02#"+responseMsg+"#", MessageGUID);
                        }
                        else
                        {
                            main.SendPOSResponse("01#Account Opening Failed.#", MessageGUID);
                        }

                        break;
                }// end of switch

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "AccountsOpening", "AccountOpening");
            }
        }
    }
    }

