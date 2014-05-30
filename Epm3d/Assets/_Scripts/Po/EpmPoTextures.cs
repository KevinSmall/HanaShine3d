using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using Epm3d;

namespace Epm3d
{
   /// <summary>
   /// Epm po textures.
   /// Holds all possible PO textures as properties. These get assigned in inspector and compiled
   /// into build.  Needed because SP6 doesnt have images saved with the data in HANA.
   /// </summary>
   [System.Serializable]
   public class EpmPoTextures
   {
      public Texture2D HT_1000;
      public Texture2D HT_1001;
      public Texture2D HT_1002;
      public Texture2D HT_1003;
      public Texture2D HT_1007;
      public Texture2D HT_1010;
      public Texture2D HT_1011;
      public Texture2D HT_1020;
      public Texture2D HT_1021;
      public Texture2D HT_1022;
      public Texture2D HT_1023;
      public Texture2D HT_1030;
      public Texture2D HT_1031;
      public Texture2D HT_1032;
      public Texture2D HT_1035;
      public Texture2D HT_1036;
      public Texture2D HT_1037;
      public Texture2D HT_1040;
      public Texture2D HT_1041;
      public Texture2D HT_1042;
      public Texture2D HT_1050;
      public Texture2D HT_1051;
      public Texture2D HT_1052;
      public Texture2D HT_1055;
      public Texture2D HT_1056;
      public Texture2D HT_1060;
      public Texture2D HT_1061;
      public Texture2D HT_1062;
      public Texture2D HT_1063;
      public Texture2D HT_1064;
      public Texture2D HT_1065;
      public Texture2D HT_1066;
      public Texture2D HT_1067;
      public Texture2D HT_1068;
      public Texture2D HT_1069;
      public Texture2D HT_1070;
      public Texture2D HT_1071;
      public Texture2D HT_1072;
      public Texture2D HT_1073;
      public Texture2D HT_1080;
      public Texture2D HT_1081;
      public Texture2D HT_1082;
      public Texture2D HT_1083;
      public Texture2D HT_1085;
      public Texture2D HT_1090;
      public Texture2D HT_1091;
      public Texture2D HT_1092;
      public Texture2D HT_1095;
      public Texture2D HT_1096;
      public Texture2D HT_1097;
      public Texture2D HT_1100;
      public Texture2D HT_1101;
      public Texture2D HT_1102;
      public Texture2D HT_1103;
      public Texture2D HT_1104;
      public Texture2D HT_1105;
      public Texture2D HT_1106;
      public Texture2D HT_1107;
      public Texture2D HT_1110;
      public Texture2D HT_1111;
      public Texture2D HT_1112;
      public Texture2D HT_1113;
      public Texture2D HT_1114;
      public Texture2D HT_1115;
      public Texture2D HT_1116;
      public Texture2D HT_1117;
      public Texture2D HT_1118;
      public Texture2D HT_1119;
      public Texture2D HT_1120;
      public Texture2D HT_1137;
      public Texture2D HT_1138;
      public Texture2D HT_1210;
      public Texture2D HT_1500;
      public Texture2D HT_1501;
      public Texture2D HT_1502;
      public Texture2D HT_1600;
      public Texture2D HT_1601;
      public Texture2D HT_1602;
      public Texture2D HT_1603;
      public Texture2D HT_2000;
      public Texture2D HT_2001;
      public Texture2D HT_2002;
      public Texture2D HT_2020;
      public Texture2D HT_2025;
      public Texture2D HT_2026;
      public Texture2D HT_2027;
      public Texture2D HT_2500;
      public Texture2D HT_2501;
      public Texture2D HT_2502;
      public Texture2D HT_6100;
      public Texture2D HT_6102;
      public Texture2D HT_6110;
      public Texture2D HT_6111;
      public Texture2D HT_6120;
      public Texture2D HT_6121;
      public Texture2D HT_6122;
      public Texture2D HT_6123;
      public Texture2D HT_6130;
      public Texture2D HT_6131;
      public Texture2D HT_7000;
      public Texture2D HT_7010;
      public Texture2D HT_7020;
      public Texture2D HT_7030;
      public Texture2D HT_8000;
      public Texture2D HT_8001;
      public Texture2D HT_8002;
      public Texture2D HT_8003;
      public Texture2D HT_9991;
      public Texture2D HT_9992;
      public Texture2D HT_9993;
      public Texture2D HT_9994;
      public Texture2D HT_9995;
      public Texture2D HT_9996;
      public Texture2D HT_9997;
      public Texture2D HT_9998;
      public Texture2D HT_9999;
      // etc

      /// <summary>
      /// Gets the po texture for product identifier.
      /// convert eg "HT-1000" to the field name .HT_1000 and returns that texture
      /// </summary>
      /// <returns>The po texture for product identifier.</returns>
      /// <param name="productId">Product identifier.</param>
      public Texture2D GetPoTextureForProductId(string productId)
      {  
         // convert eg "HT-1000" to property name "HT_1000"
         string propertyName = productId.Replace("-", "_");
         propertyName = propertyName.ToUpperInvariant();

         //PropertyInfo propertyInfo = this.GetType().GetProperty(propertyName);
         FieldInfo propertyInfo = this.GetType().GetField(propertyName);
         if (propertyInfo == null)
         {
            return null;
         }
         else
         {
            return (Texture2D)propertyInfo.GetValue(this);
            //return (Texture2D)propertyInfo.GetValue(null, null);
            //return (Texture2D)propertyInfo.GetValue(this, some sort of conversion here?);
         }
      }
   }
}