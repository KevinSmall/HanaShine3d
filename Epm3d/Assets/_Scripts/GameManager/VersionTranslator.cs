using System;
namespace Epm3d
{
   public enum HanaVersion
   {
      /// <summary>
      /// TitleCase fields
      /// </summary>
      v1_00_60,

      /// <summary>
      /// UPPERCASE fields
      /// </summary>
      v1_00_70,

      /// <summary>
      /// TitleCase fields for epmNext
      /// </summary>
      v1_00_72,

      /// <summary>
      /// UPPERCASE fields again
      /// </summary>
      v1_00_80,

      Unknown
   }

   public class VerDetEventArgs : EventArgs
   {
      public HanaVersion HanaVer;
   }

   public class VersionTranslator
   {
      private HanaVersion _hanaVersion;

      private const string _epmPackagePath_60 = @"/sap/hana/democontent/epm/services/";
      private const string _epmPackagePath_70 = @"/sap/hana/democontent/epm/services/";
      private const string _epmPackagePath_72 = @"/sap/hana/democontent/epmNext/services/";
      private const string _epmPackagePath_80 = @"/sap/hana/democontent/epm/services/";

      //@"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PartnerCity&currency=USD&filterterms=";
      private string _urlEnd_EpmChart = @"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby={0}&currency={1}&filterterms=";
      
      private string _urlEnd_EpmGetOnePOwithItems = @"poWorklist.xsodata/PO_WORKLIST?$top={0}&$orderby={1},{2}%20asc&$filter={1}%20eq%20%27{3}%27&$format=json";
      
      //@"poWorklistUpdate.xsjs?cmd=approval&PurchaseOrderId=0300000010&Action=Accept";
      private string _urlEpm_AcceptOnePO = @"poWorklistUpdate.xsjs?cmd=approval&{0}={1}&Action=Accept";
      
      // only POs eligible for approval/rejection that aren't already approved rejected
      private string _urlEnd_EpmGetMassPO = @"poWorklist.xsodata/PO_WORKLIST?$skip=0&$top={0}&$orderby={1},{2}%20asc&$select={1},{2}&$inlinecount=allpages&$filter={3}%20ne%20%27Closed%27%20and%20{3}%20ne%20%27Cancelled%27%20and%20{4}%20eq%20%27Initial%27%20and%20{5}%20ne%20%27Delivered%27%20and%20{6}%20ne%20%27Approved%27%20and%20{6}%20ne%20%27Rejected%27&$format=json";
      
      private string _urlEpm_RejectOnePO = @"poWorklistUpdate.xsjs?cmd=approval&{0}={1}&Action=Reject";
      
      private string _urlEpmBase; // root of service eg "http://<server>:8000/sap/hana/democontent/epm/services/"

      public VersionTranslator(HanaVersion hanaVersion, string host, string instance)
      {
         _hanaVersion = hanaVersion;
         switch (hanaVersion)
         {
            case HanaVersion.v1_00_60:
               _urlEpmBase = @"http://" + host + ":80" + instance + _epmPackagePath_60;
               break;

            case HanaVersion.v1_00_70:
               _urlEpmBase = @"http://" + host + ":80" + instance + _epmPackagePath_70;
               break;

            case HanaVersion.v1_00_72:
               _urlEpmBase = @"http://" + host + ":80" + instance + _epmPackagePath_72;
               break;

            case HanaVersion.v1_00_80:
               _urlEpmBase = @"http://" + host + ":80" + instance + _epmPackagePath_80;
               break;

            default:
               break;
         }
      }

      public string GetUrl_Chart(EpmDataTypeChart chartType, string chartCurrency)
      {
         // build url with params for chart group by and currency
         string chartTypeAdjustedForHanaSP = chartType.ToString();
         if (_hanaVersion == HanaVersion.v1_00_70)
         {
            chartTypeAdjustedForHanaSP = chartTypeAdjustedForHanaSP.ToUpperInvariant();
         }
         string url = _urlEpmBase + string.Format(_urlEnd_EpmChart, chartTypeAdjustedForHanaSP, chartCurrency);
         return url;
      }

      public string GetUrl_SinglePOwithItems(string poDoc, int maxItemsPerPO)
      {
         //@"poWorklist.xsodata/PO_WORKLIST?$top={0}&$orderby={1},{2}%20asc&$filter={1}%20eq%20%27{3}%27&$format=json";
         // {0} = max items on po
         // {1} = PURCHASEORDERID
         // {2} = PURCHASEORDERITEM
         // {3} = 0300000002
         string url = _urlEpmBase + string.Format(_urlEnd_EpmGetOnePOwithItems,
                                                  maxItemsPerPO,
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderId),
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderItem),
                                                  poDoc);
         return url;
      }

      public string GetUrl_MassListPO(int maxPos)
      {
         //@"poWorklist.xsodata/PO_WORKLIST?$skip=0&$top={0}&$orderby=PurchaseOrderId,PurchaseOrderItem%20asc&$select=PurchaseOrderId,PurchaseOrderItem&$inlinecount=allpages&$filter=LifecycleDesc%20ne%20%27Closed%27%20and%20LifecycleDesc%20ne%20%27Cancelled%27%20and%20ConfirmationDesc%20eq%20%27Initial%27%20and%20OrderingDesc%20ne%20%27Delivered%27&$format=json";
         // {0} = count
         // {1} = po
         // {2} = poitem
         // {3} = lifecycledesc
         // {4} = confirmationdesc
         // {5} = orderingdesc
         // {6} = approvaldesc
         string url = _urlEpmBase + string.Format(_urlEnd_EpmGetMassPO,
                                                  maxPos.ToString(),
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderId),
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderItem),
                                                  GetPoFieldName(EpmPoFieldName.LifecycleDesc),
                                                  GetPoFieldName(EpmPoFieldName.ConfirmationDesc),
                                                  GetPoFieldName(EpmPoFieldName.OrderingDesc),
                                                  GetPoFieldName(EpmPoFieldName.ApprovalDesc)
                                                  );
         return url;
      }

      public string GetUrl_ApprovePO(string poDoc)
      {
         //_urlEpm_AcceptOnePO = @"poWorklistUpdate.xsjs?cmd=approval&PurchaseOrderId=0300000010&Action=Accept";
         //_urlEpm_AcceptOnePO = @"poWorklistUpdate.xsjs?cmd=approval&{0}={1}&Action=Accept";
         string url = _urlEpmBase + string.Format(_urlEpm_AcceptOnePO, 
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderId), poDoc);
         
         return url;
      }

      public string GetUrl_RejectPO(string poDoc)
      {
         string url = _urlEpmBase + string.Format(_urlEpm_RejectOnePO, 
                                                  GetPoFieldName(EpmPoFieldName.PurchaseOrderId), poDoc);
         return url;
      }

      /// <summary>
      /// Gets the url to check for a hana version.
      /// Static so it can called before actual version is known
      /// </summary>
      public static string GetUrl_ToCheckForAVersion(HanaVersion hanaVersion, string host, string instance)
      {
         string url = "";
         switch (hanaVersion)
         {
            case HanaVersion.v1_00_60:
               // 60
               // URL PO chart data (internal)
               // http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PartnerCity&currency=USD&filterterms=
               url = @"http://" + host + ":80" + instance + _epmPackagePath_60 + @"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PartnerCity&currency=USD&filterterms=";

               break;
               
            case HanaVersion.v1_00_70:
               // 70
               // URL PO chart data (internal)
               // http://54.86.47.170:8002/sap/hana/democontent/epm/services/poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PARTNERCITY&currency=USD&filterterms=
               url = @"http://" + host + ":80" + instance + _epmPackagePath_70 + @"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PARTNERCITY&currency=USD&filterterms=";
               
               break;
               
            case HanaVersion.v1_00_72:
               // 72
               // URL PO chart data (internal)
               // http://54.178.200.169:8000/sap/hana/democontent/epmNext/services/poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PartnerCity&currency=USD&filterterms= [note fields TitleCase]
               url = @"http://" + host + ":80" + instance + _epmPackagePath_72 + @"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PartnerCity&currency=USD&filterterms=";
               
               break;

            case HanaVersion.v1_00_80:
               // 80
               // URL PO chart data (internal)
               // http://54.178.200.169:8000/sap/hana/democontent/epm/services/poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PARTNERCITY&currency=USD&filterterms= [note fields back to UPPERCASE]
               url = @"http://" + host + ":80" + instance + _epmPackagePath_80 + @"poWorklistQuery.xsjs?cmd=getTotalOrders&groupby=PARTNERCITY&currency=USD&filterterms=";

               break;

            default:
               break;
         }

         // @"http://" + host + @":80" + instance + @"/sap/hana/xs/ide/";
         // _urlEpmBase + string.Format(_urlEpm_RejectOnePO, 
         // GetPoFieldName(EpmPoFieldName.PurchaseOrderId), poDoc);
         return url;
      }


      /// <summary>
      /// Gets the PO field name, taking into account SP6 SP7 etc differences.
      /// </summary>
      public string GetPoFieldName(EpmPoFieldName epmPoFieldName)
      {
         string poFieldName = "";
         
         if (_hanaVersion == HanaVersion.v1_00_70)
         {
            // SP7.0 is all upper case fields, with some exceptions
            switch (epmPoFieldName)
            {
               case EpmPoFieldName.ProductTypeCode:
                  poFieldName = "TYPECODE";
                  break;
               case EpmPoFieldName.ProductCategory:
                  poFieldName = "CATEGORY";
                  break;
               case EpmPoFieldName.PartnerCity:
                  poFieldName = "CITY";
                  break;
               case EpmPoFieldName.PartnerPostalCode:
                  poFieldName = "POSTALCODE";
                  break;
               default:
                  poFieldName = epmPoFieldName.ToString().ToUpperInvariant();
                  break;
            }
         }
         else
         {
            // SP6 has mixed upper lower case fields, the enum was built to match this
            poFieldName = epmPoFieldName.ToString();
         }
         
         return poFieldName;
      }
   }
}

