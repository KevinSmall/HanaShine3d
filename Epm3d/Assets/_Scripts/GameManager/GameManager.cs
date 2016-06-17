using UnityEngine;
using System.Collections;
using System;
using System.Text;
using SimpleJSON;

namespace Epm3d
{
   public struct WwwResponse
   {
      public string WwwText;
      public string WwwError;
   }

/// <summary>
/// Game state manager used to store persistent data, communicate with external server
/// There is no GUI here, just state.  Inside GameManager is all handling of security and 
/// comms with HANA and all JSON parsing.
/// Usage to get data:
/// - Other objects call GetEpmData_* to request some specific data, eg a PO list or a PO
/// - Once the data arrives the event On*DataChanged is raised 
/// - Other objects can then call GetEpmResponse_* to get the actual data as an object 
/// </summary>
   public class GameManager : MonoBehaviour
   {
      public event EventHandler<ChartEventArgs> OnChartDataChanged = delegate {};
      public event EventHandler<EventArgs> OnSinglePODataWithItemsChanged = delegate {};
      public event EventHandler<PoApprovedEventArgs> OnSinglePOApproved = delegate {};
      public event EventHandler<PoApprovedEventArgs> OnSinglePORejected = delegate {};
      public event EventHandler<EventArgs> OnMassListPOChanged = delegate {};

      public event EventHandler<EventArgs> OnConnNetCheckReturned = delegate {};
      public event EventHandler<EventArgs> OnServerUpReturned = delegate {};
      public event EventHandler<EventArgs> OnLoginCredReturned = delegate {};
      public event EventHandler<VerDetEventArgs> OnVerDetReturned = delegate {};

      private enum WwwDataType
      {
         EpmChartGroup,
         EpmSinglePOwithItems,
         EpmApprovePO,
         EpmRejectPO,
         EpmMassListPO,
         ConnNetCheck,
         ConnServerUp,
         ConnLoginCred,
         ConnVerDet_60,
         ConnVerDet_70,
         ConnVerDet_72,
      }
   
      private static GameManager _instance;
   
      /// <summary>
      /// Flag true if game manager has already been created
      /// </summary>
      private bool _isCreated = false;
      private string _host;
      private string _instanceHana;
      private string _username;
      private string _password;
      private bool _isOffline;
      private string _epmDataChart = "empty";
      private string _epmDataSinglePOwithItems = "empty";
      private string _epmDataMassListPO = "empty";
      private string _epmDataApprovePORequested = "";
      private EpmDataTypeChart _epmDataChartTypeRequested = EpmDataTypeChart.PartnerCity;
      private string _epmDataApprovedPOResponse = "empty";
      private string _epmDataRejectPORequested = "";
      private string _epmDataRejectedPOResponse = "empty";

      private WwwResponse _connDataNetCheckResponse;
      private WwwResponse _connDataServerUpResponse;
      private WwwResponse _connDataLoginCredResponse;
      private WwwResponse _connDataVerDet60Response;
      private WwwResponse _connDataVerDet70Response;
      private WwwResponse _connDataVerDet72Response;

      private VersionTranslator _verTranslator;

      /// <summary>
      /// Maximum number of POs requested in the mass PO list
      /// The actual number returned might be less, because not all might be eligible for approval/rejection
      /// </summary>
      private int _maxPos = 0;

      /// <summary>
      /// Maximum number of lines to request per PO
      /// </summary>
      private int _maxLinesPerPo = 0;

      /// <summary>
      /// Creates an instance of gamestate as a gameobject if an instance does not exist
      /// you'll see the gamestate instance appear in the hierarchy.
      /// </summary>
      public static GameManager Instance
      {
         get
         {
            if (_instance == null)
            {
               // create a gamestate instance as a GameObject if one does not exist (this prevents you creating multiple
               // instances which you don't want), and AddComponent this script component (gamestate) to it
               _instance = new GameObject("gamestate").AddComponent<GameManager>();
            }
            return _instance;
         }
      }

      void Awake()
      {
   
      }

      void OnApplicationPause()
      {
         // TODO?
      }

      /// <summary>
      /// Calls HANA server and asynchronously gets data, raising appropriate event On*Changed when done.
      /// Results can later be retreived using GetEpmResponse_Chart.
      /// Handles fieldname case correctly (HANA SP6 is eg PurchaseOrderId vs HANA SP7 PURCHASEORDERID)
      /// </summary>
      public void GetEpmData_Chart(EpmDataTypeChart chartType, string chartCurrency)
      {
         string url = _verTranslator.GetUrl_Chart(chartType, chartCurrency);
         _epmDataChartTypeRequested = chartType;
         // Asynch call will eventually populate _epmDataChart
         GetWwwData(url, WwwDataType.EpmChartGroup);
      }
   
      public void GetEpmData_SinglePOwithItems(string poDoc, int maxItemsPerPO)
      {
         _maxLinesPerPo = maxItemsPerPO;
         string url = _verTranslator.GetUrl_SinglePOwithItems(poDoc, maxItemsPerPO);
         // Asynch call will eventually populate _epmDataSinglePOwithItems
         GetWwwData(url, WwwDataType.EpmSinglePOwithItems);
      }

      /// <summary>
      /// Calls HANA server and asynchronously gets data, raising appropriate event On*Changed when done.
      /// Results can later be retreived using GetEpmResponse_MassListPO.
      /// Handles fieldname case correctly (HANA SP6 is eg PurchaseOrderId vs HANA SP7 PURCHASEORDERID)
      /// </summary>r
      public void GetEpmData_MassListPO(int maxPos)
      {
         _maxPos = maxPos;
         string url = _verTranslator.GetUrl_MassListPO(maxPos);
         // Asynch call will eventually populate _epmDataMassListPO
         GetWwwData(url, WwwDataType.EpmMassListPO);
      }

      /// <summary>
      /// Calls HANA server and asynchronously gets data, raising appropriate event On*Changed when done.
      /// Results can later be retreived using GetEpmResponse_ApprovedPO.
      /// Handles fieldname case correctly (HANA SP6 is eg PurchaseOrderId vs HANA SP7 PURCHASEORDERID)
      /// </summary>
      public void GetEpmData_ApprovePO(string poDoc)
      {
         print("PO approval requested for: " + poDoc);
         string url = _verTranslator.GetUrl_ApprovePO(poDoc);
         // Asynch call will eventually populate _epmDataApprovedPOResponse
         _epmDataApprovePORequested = poDoc;
         GetWwwData(url, WwwDataType.EpmApprovePO);
      }

      public void GetEpmData_RejectPO(string poDoc)
      {
         print("PO rejection requested for: " + poDoc);
         string url = _verTranslator.GetUrl_RejectPO(poDoc);
         // Asynch call will eventually populate _epmDataRejectedPOResponse
         _epmDataRejectPORequested = poDoc;
         GetWwwData(url, WwwDataType.EpmRejectPO);
      }

      public void GetConnData_NetCheck()
      {
         //print("Net check requested);
         string url = @"http://www.google.com";
         // Asynch call will eventually populate _connDataNetCheckResponse
         GetWwwData(url, WwwDataType.ConnNetCheck);
      }

      public void GetConnData_ServerUp(string host, string instance)
      {
         string url = @"http://" + host + @":80" + instance + @"/";
         // Asynch call will eventually populate _connDataServerUpResponse
         GetWwwData(url, WwwDataType.ConnServerUp);
      }

      public void GetConnData_LoginCred()
      {
         string url = @"http://" + _host + @":80" + _instanceHana + @"/sap/hana/xs/ide/";
         // Asynch call will eventually populate _connDataLoginCredResponse
         GetWwwData(url, WwwDataType.ConnLoginCred);
      }

      public void GetConnData_VerDet(HanaVersion hanaVersion, string host, string instance)
      {
         string url = VersionTranslator.GetUrl_ToCheckForAVersion(hanaVersion, host, instance);

         switch (hanaVersion)
         {
            case HanaVersion.v1_00_60:
               GetWwwData(url, WwwDataType.ConnVerDet_60);
               break;

            case HanaVersion.v1_00_70:
               GetWwwData(url, WwwDataType.ConnVerDet_70);
               break;

            case HanaVersion.v1_00_72:
               GetWwwData(url, WwwDataType.ConnVerDet_72);
               break;

            default:
               break;
         }
      }

      private void GetWwwData(string url, WwwDataType wwwDataType)
      {
         // offline handling, fakes population of _epmData* values and raises events
         if (_isOffline)
         {
            // and raise events
            switch (wwwDataType)
            {
               case WwwDataType.EpmChartGroup:
                  ChartEventArgs cea = null;
                  if (_epmDataChartTypeRequested == EpmDataTypeChart.ProductCategory)
                  {
                     // Product Category
                     _epmDataChart = @"{""entries"":[{""name"":""apples"",""value"":1776298.12},{""name"":""pears"",""value"":8486375.52},{""name"":""strawbs"",""value"":1200005.32},{""name"":""bananas"",""value"":9522913.72},{""name"":""grapes"",""value"":1888444.52}]}";
                     cea = new ChartEventArgs(){ChartType = EpmDataTypeChart.ProductCategory};
                  }
                  else
                  {
                     // Partner City
                     _epmDataChart = @"{""entries"":[{""name"":""Rom"",""value"":9522913.12},{""name"":""Sapporo"",""value"":8486375.52},{""name"":""MÂnchen"",""value"":1888444.32},{""name"":""Tokyo"",""value"":1776298.72},{""name"":""Madrid"",""value"":1200005.52}]}";
                     cea = new ChartEventArgs(){ChartType = EpmDataTypeChart.PartnerCity};
                  }
                  OnChartDataChanged(null, cea);
                  break;
              
               case WwwDataType.EpmSinglePOwithItems:
                  float f = UnityEngine.Random.Range(-2f, 2f);
                  if (f > 0)
                  {
                     _epmDataSinglePOwithItems = @"{d: {results: [{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000010')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000010"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-2000"",ProductTypeCode: ""PR"",ProductCategory: ""Electronics"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""3"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000020')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000020"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000030')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000030"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1103"",ProductTypeCode: ""PR"",ProductCategory: ""Software"",ProductName: ""Smart Multimedia"",ProductDesc: ""Complete package. 1 User. different Multimedia applications. playing music. watching dvds. only with this Smart package"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000040')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000040"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-2000"",ProductTypeCode: ""PR"",ProductCategory: ""Electronics"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000050')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000050"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000060')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000060"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1103"",ProductTypeCode: ""PR"",ProductCategory: ""Software"",ProductName: ""Smart Multimedia"",ProductDesc: ""Complete package. 1 User. different Multimedia applications. playing music. watching dvds. only with this Smart package"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000070')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000070"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-2000"",ProductTypeCode: ""PR"",ProductCategory: ""Electronics"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: " +
                        @"{uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000080')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000080"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""}]}}";
                  }
                  else
                  {
                     _epmDataSinglePOwithItems = @"{d: {results: [{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000003',PurchaseOrderItem='0000000010')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000003"",PurchaseOrderItem: ""0000000010"",PartnerId: ""0100000006"",CompanyName: ""SAP"",GrossAmount: ""31666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""3"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000020')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000020"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000030')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000030"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1103"",ProductTypeCode: ""PR"",ProductCategory: ""Software"",ProductName: ""Smart Multimedia"",ProductDesc: ""Complete package. 1 User. different Multimedia applications. playing music. watching dvds. only with this Smart package"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000040')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000040"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-2000"",ProductTypeCode: ""PR"",ProductCategory: ""Electronics"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000050')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000050"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000060')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000060"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1103"",ProductTypeCode: ""PR"",ProductCategory: ""Software"",ProductName: ""Smart Multimedia"",ProductDesc: ""Complete package. 1 User. different Multimedia applications. playing music. watching dvds. only with this Smart package"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""},{__metadata: {uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000070')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000070"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-2000"",ProductTypeCode: ""PR"",ProductCategory: ""Electronics"",ProductName: ""7 Widescreen Portable DVD Player w MP3"",ProductDesc: ""7 LCD Screen. storage battery holds up to 6 hours!"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""1"",QuantityUnit: ""EA""},{__metadata: " +
                        @"{uri: ""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000002',PurchaseOrderItem='0000000080')"",type: ""sap.hana.democontent.epm.PO_WORKLISTType""},PurchaseOrderId: ""0300000002"",PurchaseOrderItem: ""0000000080"",PartnerId: ""0100000005"",CompanyName: ""TECUM"",GrossAmount: ""11666.69"",Currency: ""EUR"",LifecycleDesc: ""Closed"",ApprovalDesc: ""Approved"",ConfirmationDesc: ""Sent"",OrderingDesc: ""Initial"",ProductId: ""HT-1010"",ProductTypeCode: ""PR"",ProductCategory: ""Notebooks"",ProductName: ""Notebook Professional 15"",ProductDesc: ""Notebook Professional 15 with 2.3GHz - 15 XGA - 2048MB DDR2 SDRAM - 40GB Hard Disc - DVD-Writer (DVD-R/+R/-RW/-RAM)"",PartnerCity: ""Muenster"",PartnerPostalCode: ""48155"",Quantity: ""2"",QuantityUnit: ""EA""}]}}";
                  }
                  OnSinglePODataWithItemsChanged(null, EventArgs.Empty);
                  break;
             
               case WwwDataType.EpmMassListPO:
                  _epmDataMassListPO = @"{""d"":{""results"":[{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000060')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000060""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000070')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000070""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000080')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000080""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000090')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000090""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000000',PurchaseOrderItem='0000000100')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000000"",""PurchaseOrderItem"":""0000000100""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent." + 
                     @"epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000060')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000060""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000070')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000070""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000080')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000080""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000090')"",""type"":""sap.hana.democontent." +
                     @"epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000090""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000001',PurchaseOrderItem='0000000100')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000001"",""PurchaseOrderItem"":""0000000100""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000060')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000060""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000010',PurchaseOrderItem='0000000070')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000010"",""PurchaseOrderItem"":""0000000070""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000016',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000016"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000016',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000016"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000016',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent." +
                     @"epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000016"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000060')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000060""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000070')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000070""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000080')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000080""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000090')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000090""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000026',PurchaseOrderItem='0000000100')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000026"",""PurchaseOrderItem"":""0000000100""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000028',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000028"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000028',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000028"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000028',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000028"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000028',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000028"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000028',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000028"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000031',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000031"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000031',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000031"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000031',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent." +
                     @"epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000031"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000050""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000060')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000060""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000038',PurchaseOrderItem='0000000070')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000038"",""PurchaseOrderItem"":""0000000070""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000044',PurchaseOrderItem='0000000010')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000044"",""PurchaseOrderItem"":""0000000010""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000044',PurchaseOrderItem='0000000020')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000044"",""PurchaseOrderItem"":""0000000020""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000044',PurchaseOrderItem='0000000030')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000044"",""PurchaseOrderItem"":""0000000030""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000044',PurchaseOrderItem='0000000040')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000044"",""PurchaseOrderItem"":""0000000040""},{""__metadata"": {""uri"":""http://uvo1lhvt5s58vittd8f.vm.cld.sr:8000/sap/hana/democontent/epm/services/poWorklist.xsodata/PO_WORKLIST(PurchaseOrderId='0300000044',PurchaseOrderItem='0000000050')"",""type"":""sap.hana.democontent.epm.PO_WORKLISTType""},""PurchaseOrderId"":""0300000044"",""PurchaseOrderItem"":""0000000050""}],""__count"":""2354""}}";
                  OnMassListPOChanged(null, EventArgs.Empty);
                  break;

               case WwwDataType.EpmApprovePO:
                  _epmDataApprovedPOResponse = "Success";
                  PoApprovedEventArgs poea = new PoApprovedEventArgs(){PurchaseOrderId = _epmDataApprovePORequested};
                  OnSinglePOApproved(null, poea);
                  break;

               case WwwDataType.EpmRejectPO:
                  _epmDataRejectedPOResponse = "Success";
                  PoApprovedEventArgs poear = new PoApprovedEventArgs(){PurchaseOrderId = _epmDataRejectPORequested};
                  OnSinglePORejected(null, poear);
                  break;

               case WwwDataType.ConnNetCheck:
                  _connDataNetCheckResponse.WwwText = "some google text";
                  _connDataNetCheckResponse.WwwError = null;
                  OnConnNetCheckReturned(this, null);
                  break;

               case WwwDataType.ConnServerUp:
                  _connDataServerUpResponse.WwwText = "some server text";
                  _connDataServerUpResponse.WwwError = null;
                  OnServerUpReturned(this, null);
                  break;

               case WwwDataType.ConnLoginCred:
                  _connDataLoginCredResponse.WwwText = "some login cred text";
                  _connDataLoginCredResponse.WwwError = null;
                  OnLoginCredReturned(this, null);
                  break;

               case WwwDataType.ConnVerDet_60:
                  _connDataVerDet60Response.WwwText = "ver det 60 ok";
                  _connDataVerDet60Response.WwwError = null;
                  VerDetEventArgs vdea60 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_60};
                  OnVerDetReturned(this, vdea60);
                  break;

               case WwwDataType.ConnVerDet_70:
                  _connDataVerDet70Response.WwwText = "ver det 70 failed";
                  _connDataVerDet70Response.WwwError = "ver det 70 failed";
                  VerDetEventArgs vdea70 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_70};
                  OnVerDetReturned(this, vdea70);
                  break;

               case WwwDataType.ConnVerDet_72:
                  _connDataVerDet72Response.WwwText = "ver det 72 failed";
                  _connDataVerDet72Response.WwwError = "ver det 72 failed";
                  VerDetEventArgs vdea72 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_72};
                  OnVerDetReturned(this, vdea72);
                  break;

               default:
                  break;
            }
            return;
         }

         // normal online use
         try
         {
            WWW www = null;
            System.Collections.Generic.Dictionary<string, string> d = null;

            if (wwwDataType == WwwDataType.ConnNetCheck || wwwDataType == WwwDataType.ConnServerUp)
            {
               //---------------------------------------------------------
               // Without login
               //---------------------------------------------------------
               print("WWW ------------ calling now: " + url);
               www = new WWW(url, null, d); 
            }
            else
            {
               //---------------------------------------------------------
               // With login 
               //---------------------------------------------------------
               // prep headers
               //Hashtable headers = new Hashtable();
               //headers["Accept-Language"] = "en-US,en;q=0.8"; // prevents a 500 error saying "Multiple resources found. Inconsistency between data model and service description found"
               //string userAndPasswordCombo = _username + ":" + _password;
               //byte[] bytesToEncode = Encoding.UTF8.GetBytes(userAndPasswordCombo);
               //string encodedText = Convert.ToBase64String(bytesToEncode);
               //headers["Authorization"] = "Basic " + encodedText;
               //print("WWW ------------ calling now: " + url);
               //www = new WWW(url, null, headers);
               d = new System.Collections.Generic.Dictionary<string, string>();
               d.Add("Accept-Language", "en-US,en;q=0.8"); // prevents a 500 error saying "Multiple resources found. Inconsistency between data model and service description found"
               string userAndPasswordCombo = _username + ":" + _password;
               byte[] bytesToEncode = Encoding.UTF8.GetBytes(userAndPasswordCombo);
               string encodedText = Convert.ToBase64String(bytesToEncode);
               d.Add("Authorization", "Basic " + encodedText);
               print("WWW ------------ calling now: " + url);
               www = new WWW(url, null, d);
            }
            StartCoroutine(WaitForRequest(www, wwwDataType));
         }
         catch (Exception ex)
         {
            print("An error occurred starting url: " + ex.Message);
         }
      }

      string TruncateWwwAsRequired(string fullText, WwwDataType wwwDataType)
      {
         string s = "";
         switch (wwwDataType)
         {
            case WwwDataType.ConnNetCheck:
               // the google response is quite long
               s = fullText.Substring(0,20) + " (string truncated)";
               break;

            case WwwDataType.ConnServerUp:
               // pinging the web ide is long
               s = fullText.Substring(0,20) + " (string truncated)";
               break;

            case WwwDataType.ConnLoginCred:
               // this is pro a hana login page
               s = fullText.Substring(0,20) + " (string truncated)";
               break;
            
            // version det are all quite small responses
            case WwwDataType.ConnVerDet_60:
               s = fullText;
               break;
            
            case WwwDataType.ConnVerDet_70:
               s = fullText;
               break;
            
            case WwwDataType.ConnVerDet_72:
               s = fullText;
               break;
            
            default:
               // no truncations
               s = fullText;
               break;
         }
         return s;
      }

      /// <summary>
      /// Waits for request, then stores raw string result in _epmData* and raises event
      /// No parsing of results happens here, the results are parsed on demand when the
      /// EpmChartData GetEpmResponse* methods are called.
      /// </summary>
      IEnumerator WaitForRequest(WWW www, WwwDataType wwwDataType)
      {
         yield return www;
         print("WWW ------------ returned");
         string wwwResult = "";
         bool isWwwAdditionalError = false;
         string wwwAdditionalError = "";
         bool isWwwOk = false;

         //-------------------------------------------------------------------------
         // Insert our own errors
         //-------------------------------------------------------------------------
         // check for failed login, it is a www ok request, but body contains a login page
         // the login page is acceptable for the server ping check
         if (wwwDataType != WwwDataType.ConnServerUp && www.text.Contains(@"<title>HANA Login</title>"))
         {
            isWwwAdditionalError = true;
            wwwAdditionalError = "EPM Login Failed - User or Pwd wrong";
         }

         // Error check
         if (www.error == null && !isWwwAdditionalError)
         {
            // all is ok
            string s = TruncateWwwAsRequired(www.text, wwwDataType);
            print("WWW Ok: " + s);
            wwwResult = www.text;
            isWwwOk = true;
         }
         else if (!isWwwAdditionalError)
         {
            // standard error (eg field name is wrong case)
            print("WWW Error: " + www.error);
            wwwResult = www.error;
            isWwwOk = false;
         }
         else
         {
            // own additional error (eg login failed) 
            print("WWW Additional Error: " + wwwAdditionalError);
            wwwResult = wwwAdditionalError;
            isWwwOk = false;
         }

         //-------------------------------------------------------------------------
         // use dataType to decide where to put data
         //-------------------------------------------------------------------------
         switch (wwwDataType)
         {
            case WwwDataType.EpmChartGroup:
               _epmDataChart = wwwResult;
               if (isWwwOk)
               {
                  ChartEventArgs cea = new ChartEventArgs(){ChartType = _epmDataChartTypeRequested};
                  OnChartDataChanged(null, cea);
               }
               break;
         
            case WwwDataType.EpmSinglePOwithItems:
               _epmDataSinglePOwithItems = wwwResult;
               if (isWwwOk)
               {
                  OnSinglePODataWithItemsChanged(null, EventArgs.Empty);
               }
               break;

            case WwwDataType.EpmMassListPO:
               _epmDataMassListPO = wwwResult;
               if (isWwwOk)
               {
                  OnMassListPOChanged(null, EventArgs.Empty);
               }
               break;

            case WwwDataType.EpmApprovePO:
               _epmDataApprovedPOResponse = wwwResult;
               if (isWwwOk && wwwResult == "Success")
               {
                  PoApprovedEventArgs poea = new PoApprovedEventArgs(){PurchaseOrderId = _epmDataApprovePORequested};
                  OnSinglePOApproved(null, poea);
               }
               break;
         
            case WwwDataType.EpmRejectPO:
               _epmDataRejectedPOResponse = wwwResult;
               if (isWwwOk && wwwResult == "Success")
               {
                  PoApprovedEventArgs poear = new PoApprovedEventArgs(){PurchaseOrderId = _epmDataRejectPORequested};
                  OnSinglePORejected(null, poear);
               }
               break;
            
            case WwwDataType.ConnNetCheck:
               _connDataNetCheckResponse.WwwText = wwwResult;
               _connDataNetCheckResponse.WwwError = www.error;
               OnConnNetCheckReturned(this, null);
               break;

            case WwwDataType.ConnServerUp:
               _connDataServerUpResponse.WwwText = wwwResult;
               _connDataServerUpResponse.WwwError = www.error;
               OnServerUpReturned(this, null);
               break;

            case WwwDataType.ConnLoginCred:
               _connDataLoginCredResponse.WwwText = wwwResult;
               _connDataLoginCredResponse.WwwError = www.error;
               if (isWwwAdditionalError)
               {
                  _connDataLoginCredResponse.WwwError = wwwResult;
               }
               OnLoginCredReturned(this, null);
               break;

            case WwwDataType.ConnVerDet_60:
               _connDataVerDet60Response.WwwText = wwwResult;
               _connDataVerDet60Response.WwwError = www.error;
               VerDetEventArgs vdea60 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_60};
               OnVerDetReturned(this, vdea60);
               break;

            case WwwDataType.ConnVerDet_70:
               _connDataVerDet70Response.WwwText = wwwResult;
               _connDataVerDet70Response.WwwError = www.error;
               VerDetEventArgs vdea70 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_70};
               OnVerDetReturned(this, vdea70);
               break;

            case WwwDataType.ConnVerDet_72:
               _connDataVerDet72Response.WwwText = wwwResult;
               _connDataVerDet72Response.WwwError = www.error;
               VerDetEventArgs vdea72 = new VerDetEventArgs(){HanaVer = HanaVersion.v1_00_72};
               OnVerDetReturned(this, vdea72);
               break;

            default:
               break;
         }
      }

      // Sets the instance to null when the application quits
      public void OnApplicationQuit()
      {
         _instance = null;
      }
   
      /// <summary>
      /// Starts new game state. sceneName is an actual scene to start with, not a login screen
      /// </summary>
      public void StartNewGameState(HanaVersion hanaVersion,
                                    string host, string instance,
                                    string username, string password,
                                    bool isOffline, string sceneName)
      {
         if (_isCreated)
            return;

         Debug.Log("Starting game... version: " + hanaVersion.ToString() + 
                   " host: " + host + " instance " + instance + 
                   " user: " + username + " offline: " + isOffline /*+ " " + _passwordToEdit*/, this);

         _verTranslator = new VersionTranslator(hanaVersion, host, instance);
         _isCreated = true;
         _username = username;
         _password = password;
         _isOffline = isOffline;

         // Load scene (an actual scene, not a login screen)
         if (!string.IsNullOrEmpty(sceneName))
         {
            Cursor.visible = false;
            Application.LoadLevel(sceneName);
         }
      }

      public void SetOffineMode(bool isOffline)
      {
         _isOffline = isOffline;
      }

      /// <summary>
      /// Set some fields so can login with www calls to check login id, and determine version
      /// </summary>
      public void StartGamePartial(string host, string instance, string username, string password)
      {
         _host = host;
         _instanceHana = instance;
         _username = username;
         _password = password;
      }
     
      public EpmChartData GetEpmResponse_Chart()
      {
         if (string.IsNullOrEmpty(_epmDataChart) || _epmDataChart == "empty")
         {
            return null;
         }
       else
         {
            EpmChartData epmChart = new EpmChartData(false);
            epmChart.ChartTitle = _epmDataChartTypeRequested.ToString();
            
            var N = JSON.Parse(_epmDataChart);
            // if server not up, then _epmDataChart will contain error string, not parsable JSON
            if (N == null)
            {
               print("GameManager.GetEpmResponse_Chart failed to parse: " + _epmDataChart);
               // only log above once:
               _epmDataChart = null;
               return null;
            }

            int entries = N["entries"].Count;
            for (int i = 0; i < entries; i++)
            {
               string name = N["entries"][i]["name"];
               string value = N["entries"][i]["value"];
               //print("entry: " + i + " " + name + " " + value);
               epmChart.Records.Add(new EpmChartRecord()
               {
               BarName = name,
               BarValue = float.Parse(value),
               BarValueLabel = ""
               });
            }
            return epmChart;
         }
      }

      public EpmPoDataWithItems GetEpmResponse_SinglePOWithItems()
      {
         if (string.IsNullOrEmpty(_epmDataSinglePOwithItems) || _epmDataSinglePOwithItems == "empty")
         {
            return null;
         }
         else
         {
            var N = JSON.Parse(_epmDataSinglePOwithItems);          
            
            // if server not up, then _epmDataMassListPO will contain error string, not parsable JSON
            if (N == null)
            {
               print("GameManager.GetEpmResponse_SinglePOWithItems failed to parse: " + _epmDataSinglePOwithItems);
               // only log above once:
               _epmDataSinglePOwithItems = null;
               return null;
            }
            
            // Build PO item list
            EpmPoDataWithItems epmPoDataWithItems = new EpmPoDataWithItems();

            // Nasty hack for offline PO generation
            string poIdOffline = "";
            if (_isOffline)
            {
               int po = (int)UnityEngine.Random.Range(1999f,9000f);
               poIdOffline = "030000" + po; // 10 chars long
            }
    
            for (int i = 0; i < _maxLinesPerPo; i++) 
            {
               string poId = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.PurchaseOrderId)].Value;
               string poItem = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.PurchaseOrderItem)].Value;

               if (String.IsNullOrEmpty(poId))
               {
                  // We didnt get all the POs we asked for, leave early
                  break;
               }
               else
               {
                  // This is a valid po line, can store it
                  // Nasty hack for offline PO generation
                  if (_isOffline)
                  {
                     poId = poIdOffline;
                  }

                  string currValue = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.GrossAmount)].Value; 
                  string qty = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.Quantity)].Value;
                  epmPoDataWithItems.PoItems.Add(new EpmPoData()
                  { 
                     PurchaseOrderId = poId,
                     PurchaseOrderItem = poItem,
                     PartnerId = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.PartnerId)].Value,
                     CompanyName = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.CompanyName)].Value,
                     GrossAmount = float.Parse(currValue),
                     Currency = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.Currency)].Value,
                     ProductId = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.ProductId)].Value,
                     ProductCategory = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.ProductCategory)].Value,
                     ProductName = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.ProductName)].Value,
                     Quantity = (int)(float.Parse(qty)),
                     QuantityUnit =  N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.QuantityUnit)].Value
                  }
                  );
               }
            }   

            // Now add header level attributes
            epmPoDataWithItems.PurchaseOrderId = epmPoDataWithItems.PoItems[0].PurchaseOrderId;
            epmPoDataWithItems.CompanyName = epmPoDataWithItems.PoItems[0].CompanyName;
            epmPoDataWithItems.PartnerId = epmPoDataWithItems.PoItems[0].PartnerId;
            epmPoDataWithItems.ProductId = epmPoDataWithItems.PoItems[0].ProductId;
            epmPoDataWithItems.ProductCategory = epmPoDataWithItems.PoItems[0].ProductCategory;
            epmPoDataWithItems.ProductName = epmPoDataWithItems.PoItems[0].ProductName;
            epmPoDataWithItems.GrossAmount = epmPoDataWithItems.PoItems[0].GrossAmount;
            epmPoDataWithItems.Currency = epmPoDataWithItems.PoItems[0].Currency;

            return epmPoDataWithItems;
         }
      }

      public EpmPoDataMass GetEpmResponse_MassListPO()
      {
         if (string.IsNullOrEmpty(_epmDataMassListPO) || _epmDataMassListPO == "empty")
         {
            return null;
         }
         else
         {
            var N = JSON.Parse(_epmDataMassListPO);          

            // if server not up, then _epmDataMassListPO will contain error string, not parsable JSON
            if (N == null)
            {
               print("GameManager.GetEpmResponse_MassListPO failed to parse: " + _epmDataMassListPO);
               // only log above once:
               _epmDataMassListPO = null;
               return null;
            }
            
            // Build PO list
            EpmPoDataMass epmPoDataMass = new EpmPoDataMass();

            for (int i = 0; i < _maxPos; i++) 
            {
               string poId = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.PurchaseOrderId)].Value;
               string poItem = N["d"]["results"][i][_verTranslator.GetPoFieldName(EpmPoFieldName.PurchaseOrderItem)].Value;
               if (String.IsNullOrEmpty(poId))
               {
                  // We didnt get all the POs we asked for, leave early
                  break;
               }
               else
               {
                  epmPoDataMass.PoList.Add(new PoKey(){ PurchaseOrderId = poId, PurchaseOrderItem = poItem });
               }
            }

            return epmPoDataMass;
         }
      }

      public string GetEpmResponse_ApprovedPO()
      {
         return _epmDataApprovedPOResponse;
      }

      public string GetEpmResponse_RejectedPO()
      {
         return _epmDataRejectedPOResponse;
      }

      public WwwResponse GetConnResponse_NetCheck()
      {
         return _connDataNetCheckResponse;
      }
      
      public WwwResponse GetConnResponse_ServerUp()
      {
         return _connDataServerUpResponse;
      }

      public WwwResponse GetConnResponse_LoginCred()
      {
         return _connDataLoginCredResponse;
      }

      public WwwResponse GetConnResponse_VerDet(HanaVersion hanaVersion)
      {
         WwwResponse wwwResponse = new WwwResponse();
         switch (hanaVersion)
         {
            case HanaVersion.v1_00_60:
               wwwResponse = _connDataVerDet60Response;
               break;
               
            case HanaVersion.v1_00_70:
               wwwResponse = _connDataVerDet70Response;
               break;
               
            case HanaVersion.v1_00_72:
               wwwResponse = _connDataVerDet72Response;
               break;
               
            default:
               break;
         }
         return wwwResponse;
      }
   }
}