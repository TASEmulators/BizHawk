using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{
		uint ExecuteArm()
		{
			_currentCondVal = CONDITION(instruction);
			if (_currentCondVal == 0xF) return ExecuteArm_Unconditional();
			bool pass = _ConditionPassed() || disassemble;
			if (pass)
			{
				uint op1 = (instruction & 0x0E000000) >> 25;
				uint op = (instruction & 0x10) >> 4;
				switch (op1)
				{
					case _.b000:
					case _.b001:
						return ExecuteArm_DataProcessing();
					case _.b010:
						return ExecuteArm_LoadStore();
					case _.b011:
						if (op == 0) return ExecuteArm_LoadStore();
						else return ExecuteArm_Media();
					case _.b100:
					case _.b101:
						return ExecuteArm_BranchAndTransfer();
					case _.b110:
					case _.b111:
						return ExecuteArm_SVCAndCP();
					default:
						throw new InvalidOperationException();
				}
			}
			else
				return 1;
		}


		uint ExecuteArm_LoadStore()
		{
			//A5.3
			uint A = _.BIT25(instruction);
			uint op1 = (instruction >> 20) & 0x1F;
			uint B = _.BIT4(instruction);
			uint Rn = Reg16(16);	

			//ArmLoadStore
			switch((A<<0)|(op1<<1)|(B<<6)|(Rn<<7)) {
			//A==0 && op1 == #xx0x0 && op1 != #0x010
			case 0: case 16: case 32: case 36: case 48: case 52: case 64: case 80: case 96: case 100: case 112: case 116: case 128: case 144: case 160: case 164: case 176: case 180: case 192: case 208: case 224: case 228: case 240: case 244: case 256: case 272: case 288: case 292: case 304: case 308: case 320: case 336: case 352: case 356: case 368: case 372: case 384: case 400: case 416: case 420: case 432: case 436: case 448: case 464: case 480: case 484: case 496: case 500: case 512: case 528: case 544: case 548: case 560: case 564: case 576: case 592: case 608: case 612: case 624: case 628: case 640: case 656: case 672: case 676: case 688: case 692: case 704: case 720: case 736: case 740: case 752: case 756: case 768: case 784: case 800: case 804: case 816: case 820: case 832: case 848: case 864: case 868: case 880: case 884: case 896: case 912: case 928: case 932: case 944: case 948: case 960: case 976: case 992: case 996: case 1008: case 1012: case 1024: case 1040: case 1056: case 1060: case 1072: case 1076: case 1088: case 1104: case 1120: case 1124: case 1136: case 1140: case 1152: case 1168: case 1184: case 1188: case 1200: case 1204: case 1216: case 1232: case 1248: case 1252: case 1264: case 1268: case 1280: case 1296: case 1312: case 1316: case 1328: case 1332: case 1344: case 1360: case 1376: case 1380: case 1392: case 1396: case 1408: case 1424: case 1440: case 1444: case 1456: case 1460: case 1472: case 1488: case 1504: case 1508: case 1520: case 1524: case 1536: case 1552: case 1568: case 1572: case 1584: case 1588: case 1600: case 1616: case 1632: case 1636: case 1648: case 1652: case 1664: case 1680: case 1696: case 1700: case 1712: case 1716: case 1728: case 1744: case 1760: case 1764: case 1776: case 1780: case 1792: case 1808: case 1824: case 1828: case 1840: case 1844: case 1856: case 1872: case 1888: case 1892: case 1904: case 1908: case 1920: case 1936: case 1952: case 1956: case 1968: case 1972: case 1984: case 2000: case 2016: case 2020: case 2032: case 2036: 
				Execute_STR_immediate_A1();
				break;
			//A==1 && op1 == #xx0x0 && op1 != #0x010 && B==0
			case 1: case 17: case 33: case 37: case 49: case 53: case 129: case 145: case 161: case 165: case 177: case 181: case 257: case 273: case 289: case 293: case 305: case 309: case 385: case 401: case 417: case 421: case 433: case 437: case 513: case 529: case 545: case 549: case 561: case 565: case 641: case 657: case 673: case 677: case 689: case 693: case 769: case 785: case 801: case 805: case 817: case 821: case 897: case 913: case 929: case 933: case 945: case 949: case 1025: case 1041: case 1057: case 1061: case 1073: case 1077: case 1153: case 1169: case 1185: case 1189: case 1201: case 1205: case 1281: case 1297: case 1313: case 1317: case 1329: case 1333: case 1409: case 1425: case 1441: case 1445: case 1457: case 1461: case 1537: case 1553: case 1569: case 1573: case 1585: case 1589: case 1665: case 1681: case 1697: case 1701: case 1713: case 1717: case 1793: case 1809: case 1825: case 1829: case 1841: case 1845: case 1921: case 1937: case 1953: case 1957: case 1969: case 1973: 
				//STR (register) on page A8-386;
				break;
			//A==0 && op1 == #0x010
			//A==1 && op1 == #0x010 && B==0
			case 4: case 5: case 20: case 21: case 68: case 84: case 132: case 133: case 148: case 149: case 196: case 212: case 260: case 261: case 276: case 277: case 324: case 340: case 388: case 389: case 404: case 405: case 452: case 468: case 516: case 517: case 532: case 533: case 580: case 596: case 644: case 645: case 660: case 661: case 708: case 724: case 772: case 773: case 788: case 789: case 836: case 852: case 900: case 901: case 916: case 917: case 964: case 980: case 1028: case 1029: case 1044: case 1045: case 1092: case 1108: case 1156: case 1157: case 1172: case 1173: case 1220: case 1236: case 1284: case 1285: case 1300: case 1301: case 1348: case 1364: case 1412: case 1413: case 1428: case 1429: case 1476: case 1492: case 1540: case 1541: case 1556: case 1557: case 1604: case 1620: case 1668: case 1669: case 1684: case 1685: case 1732: case 1748: case 1796: case 1797: case 1812: case 1813: case 1860: case 1876: case 1924: case 1925: case 1940: case 1941: case 1988: case 2004: 
				//STRT on page A8-416;
				break;
			//A==0 && op1 == #xx0x1 && op1 != #0x011 && Rn != #1111
			case 2: case 18: case 34: case 38: case 50: case 54: case 66: case 82: case 98: case 102: case 114: case 118: case 130: case 146: case 162: case 166: case 178: case 182: case 194: case 210: case 226: case 230: case 242: case 246: case 258: case 274: case 290: case 294: case 306: case 310: case 322: case 338: case 354: case 358: case 370: case 374: case 386: case 402: case 418: case 422: case 434: case 438: case 450: case 466: case 482: case 486: case 498: case 502: case 514: case 530: case 546: case 550: case 562: case 566: case 578: case 594: case 610: case 614: case 626: case 630: case 642: case 658: case 674: case 678: case 690: case 694: case 706: case 722: case 738: case 742: case 754: case 758: case 770: case 786: case 802: case 806: case 818: case 822: case 834: case 850: case 866: case 870: case 882: case 886: case 898: case 914: case 930: case 934: case 946: case 950: case 962: case 978: case 994: case 998: case 1010: case 1014: case 1026: case 1042: case 1058: case 1062: case 1074: case 1078: case 1090: case 1106: case 1122: case 1126: case 1138: case 1142: case 1154: case 1170: case 1186: case 1190: case 1202: case 1206: case 1218: case 1234: case 1250: case 1254: case 1266: case 1270: case 1282: case 1298: case 1314: case 1318: case 1330: case 1334: case 1346: case 1362: case 1378: case 1382: case 1394: case 1398: case 1410: case 1426: case 1442: case 1446: case 1458: case 1462: case 1474: case 1490: case 1506: case 1510: case 1522: case 1526: case 1538: case 1554: case 1570: case 1574: case 1586: case 1590: case 1602: case 1618: case 1634: case 1638: case 1650: case 1654: case 1666: case 1682: case 1698: case 1702: case 1714: case 1718: case 1730: case 1746: case 1762: case 1766: case 1778: case 1782: case 1794: case 1810: case 1826: case 1830: case 1842: case 1846: case 1858: case 1874: case 1890: case 1894: case 1906: case 1910: 
				Execute_LDR_immediate_arm_A1();
				break;
			//A==0 && op1 == #xx0x1 && op1 != #0x011 && Rn == #1111
			case 1922: case 1938: case 1954: case 1958: case 1970: case 1974: case 1986: case 2002: case 2018: case 2022: case 2034: case 2038: 
				Execute_LDR_literal_A1();
				break;
			//A==1 && op1 == #xx0x1 && A != #0x011 && B==0
			case 3: case 19: case 35: case 39: case 51: case 55: case 131: case 147: case 163: case 167: case 179: case 183: case 259: case 275: case 291: case 295: case 307: case 311: case 387: case 403: case 419: case 423: case 435: case 439: case 515: case 531: case 547: case 551: case 563: case 567: case 643: case 659: case 675: case 679: case 691: case 695: case 771: case 787: case 803: case 807: case 819: case 823: case 899: case 915: case 931: case 935: case 947: case 951: case 1027: case 1043: case 1059: case 1063: case 1075: case 1079: case 1155: case 1171: case 1187: case 1191: case 1203: case 1207: case 1283: case 1299: case 1315: case 1319: case 1331: case 1335: case 1411: case 1427: case 1443: case 1447: case 1459: case 1463: case 1539: case 1555: case 1571: case 1575: case 1587: case 1591: case 1667: case 1683: case 1699: case 1703: case 1715: case 1719: case 1795: case 1811: case 1827: case 1831: case 1843: case 1847: case 1923: case 1939: case 1955: case 1959: case 1971: case 1975: 
				//LDR (register) on page A8-124;
				break;
			//A==0 && op1 == #0x011
			//A==1 && op1 == #0x011 && B==0
			case 6: case 7: case 22: case 23: case 70: case 86: case 134: case 135: case 150: case 151: case 198: case 214: case 262: case 263: case 278: case 279: case 326: case 342: case 390: case 391: case 406: case 407: case 454: case 470: case 518: case 519: case 534: case 535: case 582: case 598: case 646: case 647: case 662: case 663: case 710: case 726: case 774: case 775: case 790: case 791: case 838: case 854: case 902: case 903: case 918: case 919: case 966: case 982: case 1030: case 1031: case 1046: case 1047: case 1094: case 1110: case 1158: case 1159: case 1174: case 1175: case 1222: case 1238: case 1286: case 1287: case 1302: case 1303: case 1350: case 1366: case 1414: case 1415: case 1430: case 1431: case 1478: case 1494: case 1542: case 1543: case 1558: case 1559: case 1606: case 1622: case 1670: case 1671: case 1686: case 1687: case 1734: case 1750: case 1798: case 1799: case 1814: case 1815: case 1862: case 1878: case 1926: case 1927: case 1942: case 1943: case 1990: case 2006: 
				//LDRT on page A8-176;
				break;
			//A==0 && op1 == #xx1x0 && op1 != #0x110
			case 8: case 24: case 40: case 44: case 56: case 60: case 72: case 88: case 104: case 108: case 120: case 124: case 136: case 152: case 168: case 172: case 184: case 188: case 200: case 216: case 232: case 236: case 248: case 252: case 264: case 280: case 296: case 300: case 312: case 316: case 328: case 344: case 360: case 364: case 376: case 380: case 392: case 408: case 424: case 428: case 440: case 444: case 456: case 472: case 488: case 492: case 504: case 508: case 520: case 536: case 552: case 556: case 568: case 572: case 584: case 600: case 616: case 620: case 632: case 636: case 648: case 664: case 680: case 684: case 696: case 700: case 712: case 728: case 744: case 748: case 760: case 764: case 776: case 792: case 808: case 812: case 824: case 828: case 840: case 856: case 872: case 876: case 888: case 892: case 904: case 920: case 936: case 940: case 952: case 956: case 968: case 984: case 1000: case 1004: case 1016: case 1020: case 1032: case 1048: case 1064: case 1068: case 1080: case 1084: case 1096: case 1112: case 1128: case 1132: case 1144: case 1148: case 1160: case 1176: case 1192: case 1196: case 1208: case 1212: case 1224: case 1240: case 1256: case 1260: case 1272: case 1276: case 1288: case 1304: case 1320: case 1324: case 1336: case 1340: case 1352: case 1368: case 1384: case 1388: case 1400: case 1404: case 1416: case 1432: case 1448: case 1452: case 1464: case 1468: case 1480: case 1496: case 1512: case 1516: case 1528: case 1532: case 1544: case 1560: case 1576: case 1580: case 1592: case 1596: case 1608: case 1624: case 1640: case 1644: case 1656: case 1660: case 1672: case 1688: case 1704: case 1708: case 1720: case 1724: case 1736: case 1752: case 1768: case 1772: case 1784: case 1788: case 1800: case 1816: case 1832: case 1836: case 1848: case 1852: case 1864: case 1880: case 1896: case 1900: case 1912: case 1916: case 1928: case 1944: case 1960: case 1964: case 1976: case 1980: case 1992: case 2008: case 2024: case 2028: case 2040: case 2044: 
				//STRB (immediate, ARM) on page A8-390;
				break;
			//A==1 && op1 == #xx1x0 && op1 != #0x110 && B==0
			case 9: case 25: case 41: case 45: case 57: case 61: case 137: case 153: case 169: case 173: case 185: case 189: case 265: case 281: case 297: case 301: case 313: case 317: case 393: case 409: case 425: case 429: case 441: case 445: case 521: case 537: case 553: case 557: case 569: case 573: case 649: case 665: case 681: case 685: case 697: case 701: case 777: case 793: case 809: case 813: case 825: case 829: case 905: case 921: case 937: case 941: case 953: case 957: case 1033: case 1049: case 1065: case 1069: case 1081: case 1085: case 1161: case 1177: case 1193: case 1197: case 1209: case 1213: case 1289: case 1305: case 1321: case 1325: case 1337: case 1341: case 1417: case 1433: case 1449: case 1453: case 1465: case 1469: case 1545: case 1561: case 1577: case 1581: case 1593: case 1597: case 1673: case 1689: case 1705: case 1709: case 1721: case 1725: case 1801: case 1817: case 1833: case 1837: case 1849: case 1853: case 1929: case 1945: case 1961: case 1965: case 1977: case 1981: 
				//STRB (register) on page A8-392;
				break;
			//A==0 && op1 == #0x110
			//A==1 && op1 == #0x110 && B==0
			case 12: case 13: case 28: case 29: case 76: case 92: case 140: case 141: case 156: case 157: case 204: case 220: case 268: case 269: case 284: case 285: case 332: case 348: case 396: case 397: case 412: case 413: case 460: case 476: case 524: case 525: case 540: case 541: case 588: case 604: case 652: case 653: case 668: case 669: case 716: case 732: case 780: case 781: case 796: case 797: case 844: case 860: case 908: case 909: case 924: case 925: case 972: case 988: case 1036: case 1037: case 1052: case 1053: case 1100: case 1116: case 1164: case 1165: case 1180: case 1181: case 1228: case 1244: case 1292: case 1293: case 1308: case 1309: case 1356: case 1372: case 1420: case 1421: case 1436: case 1437: case 1484: case 1500: case 1548: case 1549: case 1564: case 1565: case 1612: case 1628: case 1676: case 1677: case 1692: case 1693: case 1740: case 1756: case 1804: case 1805: case 1820: case 1821: case 1868: case 1884: case 1932: case 1933: case 1948: case 1949: case 1996: case 2012: 
				//STRBT on page A8-394;
				break;
			//A==0 && op1 == #xx1x1 && op1 != #0x111 && Rn != #1111
			case 10: case 26: case 42: case 46: case 58: case 62: case 74: case 90: case 106: case 110: case 122: case 126: case 138: case 154: case 170: case 174: case 186: case 190: case 202: case 218: case 234: case 238: case 250: case 254: case 266: case 282: case 298: case 302: case 314: case 318: case 330: case 346: case 362: case 366: case 378: case 382: case 394: case 410: case 426: case 430: case 442: case 446: case 458: case 474: case 490: case 494: case 506: case 510: case 522: case 538: case 554: case 558: case 570: case 574: case 586: case 602: case 618: case 622: case 634: case 638: case 650: case 666: case 682: case 686: case 698: case 702: case 714: case 730: case 746: case 750: case 762: case 766: case 778: case 794: case 810: case 814: case 826: case 830: case 842: case 858: case 874: case 878: case 890: case 894: case 906: case 922: case 938: case 942: case 954: case 958: case 970: case 986: case 1002: case 1006: case 1018: case 1022: case 1034: case 1050: case 1066: case 1070: case 1082: case 1086: case 1098: case 1114: case 1130: case 1134: case 1146: case 1150: case 1162: case 1178: case 1194: case 1198: case 1210: case 1214: case 1226: case 1242: case 1258: case 1262: case 1274: case 1278: case 1290: case 1306: case 1322: case 1326: case 1338: case 1342: case 1354: case 1370: case 1386: case 1390: case 1402: case 1406: case 1418: case 1434: case 1450: case 1454: case 1466: case 1470: case 1482: case 1498: case 1514: case 1518: case 1530: case 1534: case 1546: case 1562: case 1578: case 1582: case 1594: case 1598: case 1610: case 1626: case 1642: case 1646: case 1658: case 1662: case 1674: case 1690: case 1706: case 1710: case 1722: case 1726: case 1738: case 1754: case 1770: case 1774: case 1786: case 1790: case 1802: case 1818: case 1834: case 1838: case 1850: case 1854: case 1866: case 1882: case 1898: case 1902: case 1914: case 1918: 
				//LDRB (immediate, ARM) on page A8-128;
				break;
			//A==0 && op1 == #xx1x1 && op1 != #0x111 && Rn == #1111
			case 1930: case 1946: case 1962: case 1966: case 1978: case 1982: case 1994: case 2010: case 2026: case 2030: case 2042: case 2046: 
				//LDRB (literal) on page A8-130;
				break;
			//A==1 && op1 == #xx1x1 && op1 != #0x111 && B==0
			case 11: case 27: case 43: case 47: case 59: case 63: case 139: case 155: case 171: case 175: case 187: case 191: case 267: case 283: case 299: case 303: case 315: case 319: case 395: case 411: case 427: case 431: case 443: case 447: case 523: case 539: case 555: case 559: case 571: case 575: case 651: case 667: case 683: case 687: case 699: case 703: case 779: case 795: case 811: case 815: case 827: case 831: case 907: case 923: case 939: case 943: case 955: case 959: case 1035: case 1051: case 1067: case 1071: case 1083: case 1087: case 1163: case 1179: case 1195: case 1199: case 1211: case 1215: case 1291: case 1307: case 1323: case 1327: case 1339: case 1343: case 1419: case 1435: case 1451: case 1455: case 1467: case 1471: case 1547: case 1563: case 1579: case 1583: case 1595: case 1599: case 1675: case 1691: case 1707: case 1711: case 1723: case 1727: case 1803: case 1819: case 1835: case 1839: case 1851: case 1855: case 1931: case 1947: case 1963: case 1967: case 1979: case 1983: 
				//LDRB (register) on page A8-132;
				break;
			//A==0 && op1 == #0x111
			//A==1 && op1 == #0x111 && B==0
			case 14: case 15: case 30: case 31: case 78: case 94: case 142: case 143: case 158: case 159: case 206: case 222: case 270: case 271: case 286: case 287: case 334: case 350: case 398: case 399: case 414: case 415: case 462: case 478: case 526: case 527: case 542: case 543: case 590: case 606: case 654: case 655: case 670: case 671: case 718: case 734: case 782: case 783: case 798: case 799: case 846: case 862: case 910: case 911: case 926: case 927: case 974: case 990: case 1038: case 1039: case 1054: case 1055: case 1102: case 1118: case 1166: case 1167: case 1182: case 1183: case 1230: case 1246: case 1294: case 1295: case 1310: case 1311: case 1358: case 1374: case 1422: case 1423: case 1438: case 1439: case 1486: case 1502: case 1550: case 1551: case 1566: case 1567: case 1614: case 1630: case 1678: case 1679: case 1694: case 1695: case 1742: case 1758: case 1806: case 1807: case 1822: case 1823: case 1870: case 1886: case 1934: case 1935: case 1950: case 1951: case 1998: case 2014: 
				//LDRBT on page A8-134;
				break;
			default: throw new InvalidOperationException("unhandled case for switch ArmLoadStore");
			}


			return cycles;
		}

		uint Execute_LDR_immediate_arm_A1()
		{
			//A8.6.58 LDR (immediate, ARM)
			Bit P = _.BIT24(instruction);
			Bit U = _.BIT23(instruction);
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			if (n == _.b1111) throw new NotImplementedException("see LDR (literal");
			if (P == 0 && W == 1) throw new NotImplementedException("see LDRT");
			if (n == _.b1101 && P == 0 && U == 1 && W == 0 && imm12 == _.b000000000100)
				return Execute_POP_A2();
			uint imm32 = _ZeroExtend_32(imm12);
			bool index = (P == 1);
			bool add = (U == 1);
			bool wback = (P == 0) || (W == 1);
			if (wback && n == t) unpredictable = true;

			return ExecuteCore_LDR_immediate_arm(Encoding.A1, t, imm32, n, index, add, wback);
		}

		uint Execute_POP_A2()
		{
			//A8.6.122 POP
			uint t = Reg16(12);
			uint regs = (uint)(1 << (int)t);
			if (t == 13) unpredictable = true;
			const bool UnalignedAllowed = true;
			return ExecuteCore_POP(Encoding.A2, regs, UnalignedAllowed);
		}

		uint Execute_STR_immediate_A1()
		{
			//A8.6.194
			Bit P = _.BIT24(instruction);
			Bit U = _.BIT23(instruction);
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm32 = instruction & 0xFFF;
			if (P == 0 && W == 1) throw new NotImplementedException("see STRT");
			if (n == _.b1101 && P == 1 && U == 0 && W == 1 && imm32 == _.b000000000100)
				return Execute_PUSH_A2();
			bool index = (P == 1);
			bool add = (U == 1);
			bool wback = (P == 0) || (W == 1);
			if (wback && n == 16 || n == t) unpredictable = true;

			return ExecuteCore_STR_immediate_arm(Encoding.A1, P, U, W, n, t, imm32, index, add, wback);
		}

		uint Execute_PUSH_A2()
		{
			//A8.6.123
			uint t = Reg8(12);
			bool UnalignedAllowed = true;
			uint registers = (uint)(1 << (int)t);
			if (t == 13) _FlagUnpredictable();
			return ExecuteCore_PUSH(Encoding.A2, registers, UnalignedAllowed);
		}

		uint Execute_LDR_literal_A1()
		{
			//A8.6.59
			//A8-122
			uint t = Reg16(12);
			uint imm32 = instruction & 0xFFF;
			uint U = _.BIT23(instruction);
			bool add = (U == 1);

			return ExecuteCore_LDR_literal(Encoding.A1, t, imm32, add);
		}


		uint ExecuteArm_DataProcessing()
		{
			uint op = _.BIT25(instruction);
			uint op1 = (instruction >> 20) & 0x1F;
			uint op2 = (instruction >> 4) & 0xF;
			switch (op)
			{
				case 0:
					if (!CHK(op1, _.b11001, _.b10000))
						if (CHK(op2, _.b0001, _.b0000)) return ExecuteArm_DataProcessing_Register();
						else if (CHK(op2, _.b1001, _.b0001)) return Execute_Unhandled("data-processing (register-shifted register) on page A5-7");
					if (CHK(op1, _.b11001, _.b10000))
						if (CHK(op2, _.b1000, _.b0000)) return ExecuteArm_DataProcessing_Misc();
						else if (CHK(op2, _.b1001, _.b1000)) return Execute_Unhandled("halfword multiply and multiply-accumulate on page A5-13");
					if (CHK(op1, _.b10000, _.b00000) && op2 == _.b1001) return Execute_Unhandled("multiply and multiply-accumulate on page A5-12");
					if (CHK(op1, _.b10000, _.b10000) && op2 == _.b1001) return ExecuteArm_SynchronizationPrimitives();
					if (!CHK(op1, _.b10010, _.b00010))
						if (op2 == _.b1011) return Execute_Unhandled("extra load/store instructions on page A5-14");
						else if (CHK(op2, _.b1011, _.b1101)) return Execute_Unhandled("extra load/store instructions on page A5-14");
					if (CHK(op1, _.b10010, _.b00010))
						if (op2 == _.b1011) return Execute_Unhandled("extra load/store instructions (unprivileged) on page A5-15");
						else if (CHK(op2, _.b1011, _.b1101)) return Execute_Unhandled("extra load/store instructions (unprivileged) on page A5-15");
					throw new InvalidOperationException("unexpected decoder fail");
				case 1:
					if (!CHK(op1, _.b11001, _.b10000))
						return ExecuteArm_DataProcessing_Immediate();
					if (op1 == _.b10000) return Execute_Unhandled("16-bit immediate load (MOV (immediate on page A8-193) //v6T2");
					if (op1 == _.b10100) return Execute_Unhandled("high halfword 16-bit immediate load (MOVT on page A8-200) //v6T2");
					if (CHK(op1, _.b11011, _.b10010)) return ExecuteArm_MSR_immediate_and_hints();
					throw new InvalidOperationException("unexpected decoder fail");

				default:
					throw new InvalidOperationException("totally impossible decoder fail");
			}
		}

		uint ExecuteArm_MSR_immediate_and_hints()
		{
			Bit op = _.BIT22(instruction);
			uint op1 = (instruction >> 16) & 0xF;
			uint op2 = instruction & 0xFF;
			if (op == 0)
			{
				switch(op1)
				{
					case _.b0000:
						switch (op2)
						{
							case 0: return Execute_NOP_A1();
							case 1: return Execute_Unhandled("yield");
							case 2: return Execute_Unhandled("wfe");
							case 3: return Execute_Unhandled("wfi");
							case 4: return Execute_Unhandled("sev");
							default: return Execute_Unhandled("DBG");
						}
					case _.b0100:
					case _.b1000: case _.b1100: 
						return Execute_Unhandled("MSR immediate");
					case _.b0001: case _.b0101:
					case _.b1001: case _.b1101:
						return Execute_Unhandled("MSR immediate");
					case _.b0010: case _.b0011:
					case _.b0110: case _.b0111:
					case _.b1010: case _.b1011:
					case _.b1110: case _.b1111:
						return Execute_Unhandled("MSR immediate");
					default: throw new InvalidOperationException("decode fail");
				}
			}
			else return Execute_Unhandled("MSR immediate");
		}

		uint Execute_NOP_A1()
		{
			//A8.6.110
			return ExecuteCore_NOP(Encoding.A1);
		}

		uint ExecuteArm_SynchronizationPrimitives()
		{
			uint op = (instruction >> 20) & 0xF;
			switch (op)
			{
				case _.b0000:
				case _.b0100: return Execute_Unhandled("ExecuteArm_SWP_SWPB();");
				case _.b1000: return Execute_STREX_A1();
				case _.b1001: return Execute_LDREX_A1();
				case _.b1010: return Execute_Unhandled("ExecuteArm_STREXD();");
				case _.b1011: return Execute_Unhandled("ExecuteArm_LDREXD();");
				case _.b1100: return Execute_Unhandled("ExecuteArm_STREXB();");
				case _.b1101: return Execute_Unhandled("ExecuteArm_LDREXB();");
				case _.b1110: return Execute_Unhandled("ExecuteArm_STREXH();");
				case _.b1111: return Execute_Unhandled("ExecuteArm_LDREXH();");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_STREX_A1()
		{
			//A8.6.202 STREX
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint t = Reg16(0);
			const uint imm32 = 0;
			if (d == 15 || t == 15 || n == 15) unpredictable = true;
			if (d == n || d == t) unpredictable = true;
			return ExecuteCore_STREX(Encoding.A1, d, n, t, imm32);
		}

		uint Execute_LDREX_A1()
		{
			//A8.6.69 LDREX
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm32 = 0;
			if (t == 15 || n == 15) unpredictable = true;
			return ExecuteCore_LDREX(Encoding.A1, n, t, imm32);
		}

		uint ExecuteArm_DataProcessing_Register()
		{
			//A5.2.1
			uint op1 = (instruction >> 20) & 0x1F;
			uint op2 = (instruction >> 7) & 0x1F;
			uint op3 = (instruction >> 5) & 3;

			switch (op1)
			{
				case _.b00000:
				case _.b00001: return Execute_Unhandled("arm and reg");
				case _.b00010:
				case _.b00011: return Execute_Unhandled("arm eor reg");
				case _.b00100:
				case _.b00101: return Execute_Unhandled("arm sub reg");
				case _.b00110:
				case _.b00111: return Execute_Unhandled("arm rsb reg");
				case _.b01000:
				case _.b01001: return Execute_ADD_register_A1();
				case _.b01010:
				case _.b01011: return Execute_Unhandled("arm adc reg");
				case _.b01100:
				case _.b01101: return Execute_Unhandled("arm sbc reg");
				case _.b01110:
				case _.b01111: return Execute_Unhandled("arm rsc reg");
				case _.b10001: return Execute_Unhandled("arm tst reg");
				case _.b10011: return Execute_Unhandled("arm teq reg");
				case _.b10101: return Execute_CMP_register_A1();
				case _.b10111: return Execute_Unhandled("arm cmn reg");
				case _.b11000:
				case _.b11001: return Execute_Unhandled("arm orr reg");
				case _.b11010:
				case _.b11011:
					if (op2 == _.b00000 && op3 == _.b00) return Execute_MOV_register_A1();
					if (op2 != _.b00000 && op3 == _.b00) return Execute_LSL_immediate_A1();
					if (op3 == _.b01) return Execute_LSR_immediate_A1();
					if (op3 == _.b10) return Execute_Unhandled("arm asr imm");
					if (op2 == _.b00000 && op3 == _.b11) return Execute_Unhandled("arm rrx");
					if (op2 != _.b00000 && op3 == _.b11) return Execute_Unhandled("arm ror imm");
					throw new InvalidOperationException("decode fail");
				case _.b11100:
				case _.b11101: return Execute_Unhandled("arm bic reg");
				case _.b11110:
				case _.b11111: return Execute_Unhandled("arm mvn reg");
				default:
					throw new InvalidOperationException("decode fail");
			}
		}

		uint Execute_LSR_immediate_A1()
		{
			//A8.6.90
			uint d = Reg16(12);
			uint m = Reg16(0);
			Bit S = _.BIT20(instruction);
			bool setflags = (S == 1);
			uint imm5 = (instruction >> 7) & 0x1F;
			_DecodeImmShift(_.b01, imm5);
			return ExecuteCore_LSR_immediate(Encoding.A1, d, m, setflags, shift_n);
		}

		uint Execute_LSL_immediate_A1()
		{
			//A8.6.87
			Bit S = _.BIT20(instruction);
			uint d = Reg16(12);
			uint imm5 = (instruction >> 7) & 0x1F;
			uint m = Reg16(0);
			Debug.Assert(imm5 != _.b00000); //should have been prevented by decoder
			bool setflags = (S == 1);
			_DecodeImmShift(_.b00, imm5);
			return ExecuteCore_LSL_immediate(Encoding.A1, d, m, setflags, shift_n);
		}

		uint Execute_MOV_register_A1()
		{
			//A8.6.97
			Bit S = _.BIT20(instruction);
			uint d = Reg16(12);
			uint m = Reg16(0);

			if (d == _.b1111 && S == 1) throw new NotSupportedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			return ExecuteCore_MOV_register(Encoding.A1, d, m, setflags);
		}

		uint Execute_ADD_register_A1()
		{
			//A8.6.6 ADD (register)
			uint m = Reg16(0);
			uint type = (instruction >> 4) & 3;
			uint imm5 = (instruction >> 7) & 0x1F;
			uint d = Reg16(12);
			uint n = Reg16(16);
			Bit s = _.BIT20(instruction);
			bool setflags = (s == 1);
			if (d == _.b1111 && s == 1) { throw new InvalidOperationException("see SUBS PC, LR and related instructions;"); }
			if (n == _.b1101) { throw new InvalidOperationException("see ADD (SP plus register);"); }
			_DecodeImmShift(type, imm5);

			return ExecuteCore_ADD_register(Encoding.A1, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_CMP_register_A1()
		{
			//A8.6.36
			//A8-82
			uint n = Reg16(16);
			uint imm5 = (instruction >> 7) & 0x1F;
			uint m = Reg16(0);
			uint type = (instruction >> 5) & 3;
			_DecodeImmShift(type, imm5);

			if (disassemble)
				return DISNEW("CMP<c>", "<Rn!>,<Rm!><,shift>", n, m, shift_t, shift_n);

			uint shifted = _Shift(r[m], shift_t, shift_n, APSR.C);
			_AddWithCarry32(r[n], ~shifted, 1);
			APSR.N = _.BIT31(alu_result_32);
			APSR.Z = alu_result_32 == 0;
			APSR.C = alu_carry_out;
			APSR.V = alu_overflow;

			return 0;
		}

		uint ExecuteArm_DataProcessing_Immediate()
		{
			//A5.2.3
			uint op = (instruction >> 20) & 0x1F;
			uint Rn = Reg16(16);
			switch (op)
			{
				case _.b00000:
				case _.b00001: return Execute_Unhandled("arm and imm");
				case _.b00010:
				case _.b00011: return Execute_Unhandled("arm eor imm");
				case _.b00100:
				case _.b00101:
					if (Rn != _.b1111) return Execute_SUB_immediate_arm_A1();
					else return Execute_Unhandled("arm adr");
				case _.b00110:
				case _.b00111: return Execute_Unhandled("arm rsb imm");
				case _.b01000:
				case _.b01001:
					if (Rn != _.b1111) return Execute_ADD_immedate_arm_A1();
					else return Execute_ADR_A1();
				case _.b01010:
				case _.b01011: return Execute_Unhandled("arm adc imm");
				case _.b01100:
				case _.b01101: return Execute_Unhandled("arm sbc imm");
				case _.b01110:
				case _.b01111: return Execute_Unhandled("arm rsc imm");
				case _.b10000:
				case _.b10010:
				case _.b10100:
				case _.b10110: return ExecuteArm_DataProcessing_Misc_Imm();
				case _.b10001: return Execute_Unhandled("arm tst imm");
				case _.b10011: return Execute_Unhandled("arm teq imm");
				case _.b10101: return Execute_CMP_immediate_A1();
				case _.b10111: return Execute_Unhandled("arm cmn imm");
				case _.b11000:
				case _.b11001: return Execute_ORR_immediate_A1();
				case _.b11010:
				case _.b11011: return Execute_MOV_immediate_A1();
				case _.b11100:
				case _.b11101: return Execute_Unhandled("arm bic imm");
				case _.b11110:
				case _.b11111: return Execute_Unhandled("arm mvn imm");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_SUB_immediate_arm_A1()
		{
			//A8.6.212
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;

			if (n == _.b1111 && S == 0) throw new NotImplementedException("SEE ADR");
			if (n == _.b1101) return Execute_SUB_SP_minus_immediate_A1();
			if (n == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_SUB_immediate_arm(Encoding.A1, setflags, n, d, imm32);
		}

		uint Execute_SUB_SP_minus_immediate_A1()
		{
			//A8.6.215
			uint d = Reg16(12);
			Bit S = _.BIT20(instruction);
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_SUB_SP_minus_immediate(Encoding.A1,d,setflags,imm32);
		}

		uint Execute_ADR_A1()
		{
			//A8.6.10
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			const bool add = true;
			return ExecuteCore_ADR(Encoding.A1, d, imm32, add);
		}

		uint Execute_ADD_immedate_arm_A1()
		{
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;

			if (n == _.b1111 && S == 0) { throw new NotImplementedException("SEE ADR"); }
			if (n == _.b1101) return Execute_ADD_SP_plus_immediate_A1();
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);

			return ExecuteCore_ADD_immediate_arm(Encoding.A1, setflags, n, d, imm32);
		}

		uint Execute_ADD_SP_plus_immediate_A1()
		{
			uint d = Reg16(12);
			Bit S = _.BIT20(instruction);
			uint imm12 = instruction & 0xFFF;
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);

			return ExecuteCore_ADD_SP_plus_immedate(Encoding.A1, d, setflags, imm32);
		}

		uint Execute_CMP_immediate_A1()
		{
			//A8.6.35
			uint Rn = Reg16(16);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_CMP_immediate(Encoding.A1, Rn, imm32);
		}

		uint Execute_ORR_immediate_A1()
		{
			//A8.6.113
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			Debug.Assert(!(d == _.b1111 && S == 1), "SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32;
			Bit carry;
			_ARMExpandImm_C(imm12, APSR.C, out imm32, out carry);
			return ExecuteCore_ORR_immediate(Encoding.A1, n, d, setflags, imm32, carry);
		}

		uint Execute_MOV_immediate_A1()
		{
			//A8.6.96
			uint Rd = Reg16(12);
			uint S = _.BIT20(instruction);
			uint imm12 = instruction & 0xFFF;
			if (Rd == _.b1111 && S == 1)
				throw new InvalidOperationException("see subs pc, lr and related instructions");
			bool setflags = (S == 1);
			uint imm32;
			Bit carry;
			_ARMExpandImm_C(imm12, APSR.C, out imm32, out carry);
			return ExecuteCore_MOV_immediate(Encoding.A1, Rd, setflags, imm32, carry);
		}

		uint ExecuteArm_DataProcessing_Misc_Imm()
		{
			//A5-4
			//TODO
			return Execute_Unhandled("ExecuteArm_DataProcessing_Misc_Imm");
		}

		uint ExecuteArm_DataProcessing_Misc()
		{
			//A5.2.12
			uint op = (instruction >> 21) & 0x3;
			uint op1 = (instruction >> 16) & 0xF;
			uint op2 = (instruction >> 4) & 0x7;

			switch (op2)
			{
				case _.b000:
					switch (op)
					{
						case _.b00:
						case _.b10:
							return Execute_Unhandled("MRS");
						case _.b01:
							switch (op1)
							{
								case _.b0000:
								case _.b0100:
								case _.b1000:
								case _.b1100: return Execute_Unhandled("MSR (register) application level");
								case _.b0001:
								case _.b0101:
								case _.b1001:
								case _.b1101:
								case _.b0010:
								case _.b0011:
								case _.b0110:
								case _.b0111:
								case _.b1010:
								case _.b1011:
								case _.b1110:
								case _.b1111:
									return Execute_Unhandled("MSR (register) system level");
								default:
									throw new InvalidOperationException("decoder fail");
							}
						case _.b11:
							return Execute_Unhandled("MSR (register) system level");
						default:
							throw new InvalidOperationException("decoder fail");
					}
				case _.b001:
					switch (op)
					{
						case _.b01: return Execute_BX_A1();
						case _.b11: return Execute_Unhandled("ExecuteArm_CLZ");
						default:
							return Execute_Undefined();
					}
				case _.b010:
					if (op == _.b01) return Execute_Unhandled("BXJ");
					else return Execute_Undefined();
				case _.b011:
					if (op == _.b01) return Execute_Unhandled("BLX (register) on page A8-60");
					else return Execute_Undefined();
				case _.b100: return Execute_Undefined();
				case _.b101: return Execute_Unhandled("saturating addition and subtraction on page A5-13");
				case _.b110: return Execute_Undefined();
				case _.b111:
					switch (op)
					{
						case _.b01: return Execute_Unhandled("BKPT on page A8-56");
						case _.b11: return Execute_Unhandled("SMC/SMI on page B6-18 //sec.ext");
						default: return Execute_Undefined();
					}
				default:
					throw new InvalidOperationException("decoder fail");
			} //switch(op2)
		}

		uint Execute_BX_A1()
		{
			//A8.6.25
			uint m = Reg16(0);
			return ExecuteCore_BX(Encoding.A1, m);
		}

		uint ExecuteArm_Media() { return Execute_Unhandled("ExecuteArm_Media"); }


		uint ExecuteArm_BranchAndTransfer()
		{
			uint op = (instruction >> 20) & 0x3F;
			uint Rn = Reg16(16);
			uint R = _.BIT15(instruction);

			//ArmBranchAndTransfer
			switch((op<<0)|(R<<6)|(Rn<<7)) {
			//op == #0000x0
			case 0: case 2: case 64: case 66: case 128: case 130: case 192: case 194: case 256: case 258: case 320: case 322: case 384: case 386: case 448: case 450: case 512: case 514: case 576: case 578: case 640: case 642: case 704: case 706: case 768: case 770: case 832: case 834: case 896: case 898: case 960: case 962: case 1024: case 1026: case 1088: case 1090: case 1152: case 1154: case 1216: case 1218: case 1280: case 1282: case 1344: case 1346: case 1408: case 1410: case 1472: case 1474: case 1536: case 1538: case 1600: case 1602: case 1664: case 1666: case 1728: case 1730: case 1792: case 1794: case 1856: case 1858: case 1920: case 1922: case 1984: case 1986: 
				Execute_Unhandled("STMDA/STMED on page A8-376");
				break;
			//op == #0000x1
			case 1: case 3: case 65: case 67: case 129: case 131: case 193: case 195: case 257: case 259: case 321: case 323: case 385: case 387: case 449: case 451: case 513: case 515: case 577: case 579: case 641: case 643: case 705: case 707: case 769: case 771: case 833: case 835: case 897: case 899: case 961: case 963: case 1025: case 1027: case 1089: case 1091: case 1153: case 1155: case 1217: case 1219: case 1281: case 1283: case 1345: case 1347: case 1409: case 1411: case 1473: case 1475: case 1537: case 1539: case 1601: case 1603: case 1665: case 1667: case 1729: case 1731: case 1793: case 1795: case 1857: case 1859: case 1921: case 1923: case 1985: case 1987: 
				Execute_Unhandled("LDMDA/LDMFA on page A8-112");
				break;
			//op == #0010x0
			case 8: case 10: case 72: case 74: case 136: case 138: case 200: case 202: case 264: case 266: case 328: case 330: case 392: case 394: case 456: case 458: case 520: case 522: case 584: case 586: case 648: case 650: case 712: case 714: case 776: case 778: case 840: case 842: case 904: case 906: case 968: case 970: case 1032: case 1034: case 1096: case 1098: case 1160: case 1162: case 1224: case 1226: case 1288: case 1290: case 1352: case 1354: case 1416: case 1418: case 1480: case 1482: case 1544: case 1546: case 1608: case 1610: case 1672: case 1674: case 1736: case 1738: case 1800: case 1802: case 1864: case 1866: case 1928: case 1930: case 1992: case 1994: 
				Execute_STM_STMIA_STMEA_A1();
				break;
			//op == #001001
			//op == #001011 && Rn != #1101
			case 9: case 11: case 73: case 75: case 137: case 139: case 201: case 203: case 265: case 267: case 329: case 331: case 393: case 395: case 457: case 459: case 521: case 523: case 585: case 587: case 649: case 651: case 713: case 715: case 777: case 779: case 841: case 843: case 905: case 907: case 969: case 971: case 1033: case 1035: case 1097: case 1099: case 1161: case 1163: case 1225: case 1227: case 1289: case 1291: case 1353: case 1355: case 1417: case 1419: case 1481: case 1483: case 1545: case 1547: case 1609: case 1611: case 1673: case 1737: case 1801: case 1803: case 1865: case 1867: case 1929: case 1931: case 1993: case 1995: 
				Execute_Unhandled("LDM/LDMIA/LDMFD on page A8-110");
				break;
			//op == #001011 && Rn == #1101
			case 1675: case 1739: 
				Execute_POP_A1();
				break;
			//op == #010000
			//op == #010010 && Rn != #1101
			case 16: case 18: case 80: case 82: case 144: case 146: case 208: case 210: case 272: case 274: case 336: case 338: case 400: case 402: case 464: case 466: case 528: case 530: case 592: case 594: case 656: case 658: case 720: case 722: case 784: case 786: case 848: case 850: case 912: case 914: case 976: case 978: case 1040: case 1042: case 1104: case 1106: case 1168: case 1170: case 1232: case 1234: case 1296: case 1298: case 1360: case 1362: case 1424: case 1426: case 1488: case 1490: case 1552: case 1554: case 1616: case 1618: case 1680: case 1744: case 1808: case 1810: case 1872: case 1874: case 1936: case 1938: case 2000: case 2002: 
				Execute_Unhandled("STMDB/STMFD on page A8-378");
				break;
			//op == #010010 && Rn == #1101
			case 1682: case 1746: 
				Execute_PUSH_A1();
				break;
			//op == #0100x1
			case 17: case 19: case 81: case 83: case 145: case 147: case 209: case 211: case 273: case 275: case 337: case 339: case 401: case 403: case 465: case 467: case 529: case 531: case 593: case 595: case 657: case 659: case 721: case 723: case 785: case 787: case 849: case 851: case 913: case 915: case 977: case 979: case 1041: case 1043: case 1105: case 1107: case 1169: case 1171: case 1233: case 1235: case 1297: case 1299: case 1361: case 1363: case 1425: case 1427: case 1489: case 1491: case 1553: case 1555: case 1617: case 1619: case 1681: case 1683: case 1745: case 1747: case 1809: case 1811: case 1873: case 1875: case 1937: case 1939: case 2001: case 2003: 
				Execute_Unhandled("LDMDB/LDMEA on page A8-114");
				break;
			//op == #0110x0
			case 24: case 26: case 88: case 90: case 152: case 154: case 216: case 218: case 280: case 282: case 344: case 346: case 408: case 410: case 472: case 474: case 536: case 538: case 600: case 602: case 664: case 666: case 728: case 730: case 792: case 794: case 856: case 858: case 920: case 922: case 984: case 986: case 1048: case 1050: case 1112: case 1114: case 1176: case 1178: case 1240: case 1242: case 1304: case 1306: case 1368: case 1370: case 1432: case 1434: case 1496: case 1498: case 1560: case 1562: case 1624: case 1626: case 1688: case 1690: case 1752: case 1754: case 1816: case 1818: case 1880: case 1882: case 1944: case 1946: case 2008: case 2010: 
				Execute_Unhandled("STMIB/STMFA on page A8-380");
				break;
			//op == #0110x1
			case 25: case 27: case 89: case 91: case 153: case 155: case 217: case 219: case 281: case 283: case 345: case 347: case 409: case 411: case 473: case 475: case 537: case 539: case 601: case 603: case 665: case 667: case 729: case 731: case 793: case 795: case 857: case 859: case 921: case 923: case 985: case 987: case 1049: case 1051: case 1113: case 1115: case 1177: case 1179: case 1241: case 1243: case 1305: case 1307: case 1369: case 1371: case 1433: case 1435: case 1497: case 1499: case 1561: case 1563: case 1625: case 1627: case 1689: case 1691: case 1753: case 1755: case 1817: case 1819: case 1881: case 1883: case 1945: case 1947: case 2009: case 2011: 
				Execute_Unhandled("LDMIB/LDMED on page A8-116");
				break;
			//op == #0xx1x0
			case 4: case 6: case 12: case 14: case 20: case 22: case 28: case 30: case 68: case 70: case 76: case 78: case 84: case 86: case 92: case 94: case 132: case 134: case 140: case 142: case 148: case 150: case 156: case 158: case 196: case 198: case 204: case 206: case 212: case 214: case 220: case 222: case 260: case 262: case 268: case 270: case 276: case 278: case 284: case 286: case 324: case 326: case 332: case 334: case 340: case 342: case 348: case 350: case 388: case 390: case 396: case 398: case 404: case 406: case 412: case 414: case 452: case 454: case 460: case 462: case 468: case 470: case 476: case 478: case 516: case 518: case 524: case 526: case 532: case 534: case 540: case 542: case 580: case 582: case 588: case 590: case 596: case 598: case 604: case 606: case 644: case 646: case 652: case 654: case 660: case 662: case 668: case 670: case 708: case 710: case 716: case 718: case 724: case 726: case 732: case 734: case 772: case 774: case 780: case 782: case 788: case 790: case 796: case 798: case 836: case 838: case 844: case 846: case 852: case 854: case 860: case 862: case 900: case 902: case 908: case 910: case 916: case 918: case 924: case 926: case 964: case 966: case 972: case 974: case 980: case 982: case 988: case 990: case 1028: case 1030: case 1036: case 1038: case 1044: case 1046: case 1052: case 1054: case 1092: case 1094: case 1100: case 1102: case 1108: case 1110: case 1116: case 1118: case 1156: case 1158: case 1164: case 1166: case 1172: case 1174: case 1180: case 1182: case 1220: case 1222: case 1228: case 1230: case 1236: case 1238: case 1244: case 1246: case 1284: case 1286: case 1292: case 1294: case 1300: case 1302: case 1308: case 1310: case 1348: case 1350: case 1356: case 1358: case 1364: case 1366: case 1372: case 1374: case 1412: case 1414: case 1420: case 1422: case 1428: case 1430: case 1436: case 1438: case 1476: case 1478: case 1484: case 1486: case 1492: case 1494: case 1500: case 1502: case 1540: case 1542: case 1548: case 1550: case 1556: case 1558: case 1564: case 1566: case 1604: case 1606: case 1612: case 1614: case 1620: case 1622: case 1628: case 1630: case 1668: case 1670: case 1676: case 1678: case 1684: case 1686: case 1692: case 1694: case 1732: case 1734: case 1740: case 1742: case 1748: case 1750: case 1756: case 1758: case 1796: case 1798: case 1804: case 1806: case 1812: case 1814: case 1820: case 1822: case 1860: case 1862: case 1868: case 1870: case 1876: case 1878: case 1884: case 1886: case 1924: case 1926: case 1932: case 1934: case 1940: case 1942: case 1948: case 1950: case 1988: case 1990: case 1996: case 1998: case 2004: case 2006: case 2012: case 2014: 
				Execute_Unhandled("STM (user registers) on page B6-22");
				break;
			//op == #0xx1x1 && R==0
			case 5: case 7: case 13: case 15: case 21: case 23: case 29: case 31: case 133: case 135: case 141: case 143: case 149: case 151: case 157: case 159: case 261: case 263: case 269: case 271: case 277: case 279: case 285: case 287: case 389: case 391: case 397: case 399: case 405: case 407: case 413: case 415: case 517: case 519: case 525: case 527: case 533: case 535: case 541: case 543: case 645: case 647: case 653: case 655: case 661: case 663: case 669: case 671: case 773: case 775: case 781: case 783: case 789: case 791: case 797: case 799: case 901: case 903: case 909: case 911: case 917: case 919: case 925: case 927: case 1029: case 1031: case 1037: case 1039: case 1045: case 1047: case 1053: case 1055: case 1157: case 1159: case 1165: case 1167: case 1173: case 1175: case 1181: case 1183: case 1285: case 1287: case 1293: case 1295: case 1301: case 1303: case 1309: case 1311: case 1413: case 1415: case 1421: case 1423: case 1429: case 1431: case 1437: case 1439: case 1541: case 1543: case 1549: case 1551: case 1557: case 1559: case 1565: case 1567: case 1669: case 1671: case 1677: case 1679: case 1685: case 1687: case 1693: case 1695: case 1797: case 1799: case 1805: case 1807: case 1813: case 1815: case 1821: case 1823: case 1925: case 1927: case 1933: case 1935: case 1941: case 1943: case 1949: case 1951: 
				Execute_Unhandled("LDM (user registers on page B6-5");
				break;
			//op == #0xx1x1 && R==1
			case 69: case 71: case 77: case 79: case 85: case 87: case 93: case 95: case 197: case 199: case 205: case 207: case 213: case 215: case 221: case 223: case 325: case 327: case 333: case 335: case 341: case 343: case 349: case 351: case 453: case 455: case 461: case 463: case 469: case 471: case 477: case 479: case 581: case 583: case 589: case 591: case 597: case 599: case 605: case 607: case 709: case 711: case 717: case 719: case 725: case 727: case 733: case 735: case 837: case 839: case 845: case 847: case 853: case 855: case 861: case 863: case 965: case 967: case 973: case 975: case 981: case 983: case 989: case 991: case 1093: case 1095: case 1101: case 1103: case 1109: case 1111: case 1117: case 1119: case 1221: case 1223: case 1229: case 1231: case 1237: case 1239: case 1245: case 1247: case 1349: case 1351: case 1357: case 1359: case 1365: case 1367: case 1373: case 1375: case 1477: case 1479: case 1485: case 1487: case 1493: case 1495: case 1501: case 1503: case 1605: case 1607: case 1613: case 1615: case 1621: case 1623: case 1629: case 1631: case 1733: case 1735: case 1741: case 1743: case 1749: case 1751: case 1757: case 1759: case 1861: case 1863: case 1869: case 1871: case 1877: case 1879: case 1885: case 1887: case 1989: case 1991: case 1997: case 1999: case 2005: case 2007: case 2013: case 2015: 
				Execute_Unhandled("LDM (exception return) on page B6-5");
				break;
			//op == #10xxxx
			case 32: case 33: case 34: case 35: case 36: case 37: case 38: case 39: case 40: case 41: case 42: case 43: case 44: case 45: case 46: case 47: case 96: case 97: case 98: case 99: case 100: case 101: case 102: case 103: case 104: case 105: case 106: case 107: case 108: case 109: case 110: case 111: case 160: case 161: case 162: case 163: case 164: case 165: case 166: case 167: case 168: case 169: case 170: case 171: case 172: case 173: case 174: case 175: case 224: case 225: case 226: case 227: case 228: case 229: case 230: case 231: case 232: case 233: case 234: case 235: case 236: case 237: case 238: case 239: case 288: case 289: case 290: case 291: case 292: case 293: case 294: case 295: case 296: case 297: case 298: case 299: case 300: case 301: case 302: case 303: case 352: case 353: case 354: case 355: case 356: case 357: case 358: case 359: case 360: case 361: case 362: case 363: case 364: case 365: case 366: case 367: case 416: case 417: case 418: case 419: case 420: case 421: case 422: case 423: case 424: case 425: case 426: case 427: case 428: case 429: case 430: case 431: case 480: case 481: case 482: case 483: case 484: case 485: case 486: case 487: case 488: case 489: case 490: case 491: case 492: case 493: case 494: case 495: case 544: case 545: case 546: case 547: case 548: case 549: case 550: case 551: case 552: case 553: case 554: case 555: case 556: case 557: case 558: case 559: case 608: case 609: case 610: case 611: case 612: case 613: case 614: case 615: case 616: case 617: case 618: case 619: case 620: case 621: case 622: case 623: case 672: case 673: case 674: case 675: case 676: case 677: case 678: case 679: case 680: case 681: case 682: case 683: case 684: case 685: case 686: case 687: case 736: case 737: case 738: case 739: case 740: case 741: case 742: case 743: case 744: case 745: case 746: case 747: case 748: case 749: case 750: case 751: case 800: case 801: case 802: case 803: case 804: case 805: case 806: case 807: case 808: case 809: case 810: case 811: case 812: case 813: case 814: case 815: case 864: case 865: case 866: case 867: case 868: case 869: case 870: case 871: case 872: case 873: case 874: case 875: case 876: case 877: case 878: case 879: case 928: case 929: case 930: case 931: case 932: case 933: case 934: case 935: case 936: case 937: case 938: case 939: case 940: case 941: case 942: case 943: case 992: case 993: case 994: case 995: case 996: case 997: case 998: case 999: case 1000: case 1001: case 1002: case 1003: case 1004: case 1005: case 1006: case 1007: case 1056: case 1057: case 1058: case 1059: case 1060: case 1061: case 1062: case 1063: case 1064: case 1065: case 1066: case 1067: case 1068: case 1069: case 1070: case 1071: case 1120: case 1121: case 1122: case 1123: case 1124: case 1125: case 1126: case 1127: case 1128: case 1129: case 1130: case 1131: case 1132: case 1133: case 1134: case 1135: case 1184: case 1185: case 1186: case 1187: case 1188: case 1189: case 1190: case 1191: case 1192: case 1193: case 1194: case 1195: case 1196: case 1197: case 1198: case 1199: case 1248: case 1249: case 1250: case 1251: case 1252: case 1253: case 1254: case 1255: case 1256: case 1257: case 1258: case 1259: case 1260: case 1261: case 1262: case 1263: case 1312: case 1313: case 1314: case 1315: case 1316: case 1317: case 1318: case 1319: case 1320: case 1321: case 1322: case 1323: case 1324: case 1325: case 1326: case 1327: case 1376: case 1377: case 1378: case 1379: case 1380: case 1381: case 1382: case 1383: case 1384: case 1385: case 1386: case 1387: case 1388: case 1389: case 1390: case 1391: case 1440: case 1441: case 1442: case 1443: case 1444: case 1445: case 1446: case 1447: case 1448: case 1449: case 1450: case 1451: case 1452: case 1453: case 1454: case 1455: case 1504: case 1505: case 1506: case 1507: case 1508: case 1509: case 1510: case 1511: case 1512: case 1513: case 1514: case 1515: case 1516: case 1517: case 1518: case 1519: case 1568: case 1569: case 1570: case 1571: case 1572: case 1573: case 1574: case 1575: case 1576: case 1577: case 1578: case 1579: case 1580: case 1581: case 1582: case 1583: case 1632: case 1633: case 1634: case 1635: case 1636: case 1637: case 1638: case 1639: case 1640: case 1641: case 1642: case 1643: case 1644: case 1645: case 1646: case 1647: case 1696: case 1697: case 1698: case 1699: case 1700: case 1701: case 1702: case 1703: case 1704: case 1705: case 1706: case 1707: case 1708: case 1709: case 1710: case 1711: case 1760: case 1761: case 1762: case 1763: case 1764: case 1765: case 1766: case 1767: case 1768: case 1769: case 1770: case 1771: case 1772: case 1773: case 1774: case 1775: case 1824: case 1825: case 1826: case 1827: case 1828: case 1829: case 1830: case 1831: case 1832: case 1833: case 1834: case 1835: case 1836: case 1837: case 1838: case 1839: case 1888: case 1889: case 1890: case 1891: case 1892: case 1893: case 1894: case 1895: case 1896: case 1897: case 1898: case 1899: case 1900: case 1901: case 1902: case 1903: case 1952: case 1953: case 1954: case 1955: case 1956: case 1957: case 1958: case 1959: case 1960: case 1961: case 1962: case 1963: case 1964: case 1965: case 1966: case 1967: case 2016: case 2017: case 2018: case 2019: case 2020: case 2021: case 2022: case 2023: case 2024: case 2025: case 2026: case 2027: case 2028: case 2029: case 2030: case 2031: 
				Execute_B_A1();
				break;
			//op == #11xxxx
			case 48: case 49: case 50: case 51: case 52: case 53: case 54: case 55: case 56: case 57: case 58: case 59: case 60: case 61: case 62: case 63: case 112: case 113: case 114: case 115: case 116: case 117: case 118: case 119: case 120: case 121: case 122: case 123: case 124: case 125: case 126: case 127: case 176: case 177: case 178: case 179: case 180: case 181: case 182: case 183: case 184: case 185: case 186: case 187: case 188: case 189: case 190: case 191: case 240: case 241: case 242: case 243: case 244: case 245: case 246: case 247: case 248: case 249: case 250: case 251: case 252: case 253: case 254: case 255: case 304: case 305: case 306: case 307: case 308: case 309: case 310: case 311: case 312: case 313: case 314: case 315: case 316: case 317: case 318: case 319: case 368: case 369: case 370: case 371: case 372: case 373: case 374: case 375: case 376: case 377: case 378: case 379: case 380: case 381: case 382: case 383: case 432: case 433: case 434: case 435: case 436: case 437: case 438: case 439: case 440: case 441: case 442: case 443: case 444: case 445: case 446: case 447: case 496: case 497: case 498: case 499: case 500: case 501: case 502: case 503: case 504: case 505: case 506: case 507: case 508: case 509: case 510: case 511: case 560: case 561: case 562: case 563: case 564: case 565: case 566: case 567: case 568: case 569: case 570: case 571: case 572: case 573: case 574: case 575: case 624: case 625: case 626: case 627: case 628: case 629: case 630: case 631: case 632: case 633: case 634: case 635: case 636: case 637: case 638: case 639: case 688: case 689: case 690: case 691: case 692: case 693: case 694: case 695: case 696: case 697: case 698: case 699: case 700: case 701: case 702: case 703: case 752: case 753: case 754: case 755: case 756: case 757: case 758: case 759: case 760: case 761: case 762: case 763: case 764: case 765: case 766: case 767: case 816: case 817: case 818: case 819: case 820: case 821: case 822: case 823: case 824: case 825: case 826: case 827: case 828: case 829: case 830: case 831: case 880: case 881: case 882: case 883: case 884: case 885: case 886: case 887: case 888: case 889: case 890: case 891: case 892: case 893: case 894: case 895: case 944: case 945: case 946: case 947: case 948: case 949: case 950: case 951: case 952: case 953: case 954: case 955: case 956: case 957: case 958: case 959: case 1008: case 1009: case 1010: case 1011: case 1012: case 1013: case 1014: case 1015: case 1016: case 1017: case 1018: case 1019: case 1020: case 1021: case 1022: case 1023: case 1072: case 1073: case 1074: case 1075: case 1076: case 1077: case 1078: case 1079: case 1080: case 1081: case 1082: case 1083: case 1084: case 1085: case 1086: case 1087: case 1136: case 1137: case 1138: case 1139: case 1140: case 1141: case 1142: case 1143: case 1144: case 1145: case 1146: case 1147: case 1148: case 1149: case 1150: case 1151: case 1200: case 1201: case 1202: case 1203: case 1204: case 1205: case 1206: case 1207: case 1208: case 1209: case 1210: case 1211: case 1212: case 1213: case 1214: case 1215: case 1264: case 1265: case 1266: case 1267: case 1268: case 1269: case 1270: case 1271: case 1272: case 1273: case 1274: case 1275: case 1276: case 1277: case 1278: case 1279: case 1328: case 1329: case 1330: case 1331: case 1332: case 1333: case 1334: case 1335: case 1336: case 1337: case 1338: case 1339: case 1340: case 1341: case 1342: case 1343: case 1392: case 1393: case 1394: case 1395: case 1396: case 1397: case 1398: case 1399: case 1400: case 1401: case 1402: case 1403: case 1404: case 1405: case 1406: case 1407: case 1456: case 1457: case 1458: case 1459: case 1460: case 1461: case 1462: case 1463: case 1464: case 1465: case 1466: case 1467: case 1468: case 1469: case 1470: case 1471: case 1520: case 1521: case 1522: case 1523: case 1524: case 1525: case 1526: case 1527: case 1528: case 1529: case 1530: case 1531: case 1532: case 1533: case 1534: case 1535: case 1584: case 1585: case 1586: case 1587: case 1588: case 1589: case 1590: case 1591: case 1592: case 1593: case 1594: case 1595: case 1596: case 1597: case 1598: case 1599: case 1648: case 1649: case 1650: case 1651: case 1652: case 1653: case 1654: case 1655: case 1656: case 1657: case 1658: case 1659: case 1660: case 1661: case 1662: case 1663: case 1712: case 1713: case 1714: case 1715: case 1716: case 1717: case 1718: case 1719: case 1720: case 1721: case 1722: case 1723: case 1724: case 1725: case 1726: case 1727: case 1776: case 1777: case 1778: case 1779: case 1780: case 1781: case 1782: case 1783: case 1784: case 1785: case 1786: case 1787: case 1788: case 1789: case 1790: case 1791: case 1840: case 1841: case 1842: case 1843: case 1844: case 1845: case 1846: case 1847: case 1848: case 1849: case 1850: case 1851: case 1852: case 1853: case 1854: case 1855: case 1904: case 1905: case 1906: case 1907: case 1908: case 1909: case 1910: case 1911: case 1912: case 1913: case 1914: case 1915: case 1916: case 1917: case 1918: case 1919: case 1968: case 1969: case 1970: case 1971: case 1972: case 1973: case 1974: case 1975: case 1976: case 1977: case 1978: case 1979: case 1980: case 1981: case 1982: case 1983: case 2032: case 2033: case 2034: case 2035: case 2036: case 2037: case 2038: case 2039: case 2040: case 2041: case 2042: case 2043: case 2044: case 2045: case 2046: case 2047: 
				Execute_BL_A1();
				break;
			default: throw new InvalidOperationException("unhandled case for switch ArmBranchAndTransfer");
			}



			return 1;
		}


		uint Execute_POP_A1()
		{
			uint register_list = instruction & 0xFFFF;
			if (_.BitCount(register_list) < 2) return Execute_LDM_LDMIA_LDMFD_A1();
			bool UnalignedAllowed = false;
			return ExecuteCore_POP(Encoding.A1, register_list, UnalignedAllowed);
		}

		uint Execute_PUSH_A1()
		{
			uint register_list = instruction & 0xFFFF;
			if (_.BitCount(register_list) < 2) return Execute_STMDB_STMFD_A1();
			bool UnalignedAllowed = false;
			return ExecuteCore_PUSH(Encoding.A1, register_list, UnalignedAllowed);
		}

		uint Execute_LDM_LDMIA_LDMFD_A1()
		{
			//A8.6.53 LDM/LDMIA/LDMFD
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			bool wback = (W == 1);
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			if (wback && _.BITN((int)n, register_list) == 1 && _ArchVersion() >= 7) unpredictable = true;
			return ExecuteCore_LDM_LDMIA_LDMFD(Encoding.A1, wback, n, register_list);
		}


		uint Execute_STM_STMIA_STMEA_A1()
		{
			//A8.6.189 STM/STMIA/STMEA
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			bool wback = W == 1;
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			return ExecuteCore_STM_STMIA_STMEA(Encoding.A1, wback, n, register_list);
		}

		uint Execute_STMDB_STMFD_A1()
		{
			//A8.6.191 STMDB/STMFD
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			if (W == 1 && n == _.b1101 && _.BitCount(register_list) >= 2) return Execute_PUSH_A1();
			bool wback = W == 1;
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			return ExecuteCore_STMDB_STMFD(Encoding.A1, wback, n, register_list);
		}

		uint Execute_BL_A1()
		{
			//A8.6.23
			//A8-58
			uint imm24 = instruction & 0xFFFFFF;
			int imm32 = _SignExtend_32(26, imm24 << 2);
			return ExecuteCore_BL_BLX_immediate(Encoding.A1, EInstrSet.ARM, imm32, false);
		}

		uint Execute_B_A1()
		{
			uint imm24 = instruction & 0xFFFFFF;
			int imm32 = _SignExtend_32(26, imm24 << 2);

			return ExecuteCore_B(Encoding.A1, imm32);
		}

		uint ExecuteArm_SVCAndCP()
		{
			uint op1 = (instruction >> 20) & 0x3F;
			uint Rn = Reg16(16);
			uint coproc = (instruction >> 8) & 0xF;
			uint op = _.BIT4(instruction);
			uint coproc_special = (coproc == _.b1010 || coproc == _.b1011) ? 1U : 0U;
			uint rn_is_15 = (Rn == 15) ? 1U : 0U;

			//ArmSVCAndCP
			switch((op1<<0)|(op<<6)|(coproc_special<<7)|(rn_is_15<<8)) {
			//op1==#0xxxxx && op1!=#000x0x && coproc_special==#1
			case 130: case 131: case 134: case 135: case 136: case 137: case 138: case 139: case 140: case 141: case 142: case 143: case 144: case 145: case 146: case 147: case 148: case 149: case 150: case 151: case 152: case 153: case 154: case 155: case 156: case 157: case 158: case 159: case 194: case 195: case 198: case 199: case 200: case 201: case 202: case 203: case 204: case 205: case 206: case 207: case 208: case 209: case 210: case 211: case 212: case 213: case 214: case 215: case 216: case 217: case 218: case 219: case 220: case 221: case 222: case 223: case 386: case 387: case 390: case 391: case 392: case 393: case 394: case 395: case 396: case 397: case 398: case 399: case 400: case 401: case 402: case 403: case 404: case 405: case 406: case 407: case 408: case 409: case 410: case 411: case 412: case 413: case 414: case 415: case 450: case 451: case 454: case 455: case 456: case 457: case 458: case 459: case 460: case 461: case 462: case 463: case 464: case 465: case 466: case 467: case 468: case 469: case 470: case 471: case 472: case 473: case 474: case 475: case 476: case 477: case 478: case 479: 
				Execute_ExtensionRegister_LoadStore();
				break;
			//op1==#0xxxx0 && op1!=#000x0x && coproc_special==#0
			case 2: case 6: case 8: case 10: case 12: case 14: case 16: case 18: case 20: case 22: case 24: case 26: case 28: case 30: case 66: case 70: case 72: case 74: case 76: case 78: case 80: case 82: case 84: case 86: case 88: case 90: case 92: case 94: case 258: case 262: case 264: case 266: case 268: case 270: case 272: case 274: case 276: case 278: case 280: case 282: case 284: case 286: case 322: case 326: case 328: case 330: case 332: case 334: case 336: case 338: case 340: case 342: case 344: case 346: case 348: case 350: 
				Execute_Unhandled("STC,STC2");
				break;
			//op1==#0xxxx1 && op1!=#000x0x && coproc_special==#0 && rn_is_15==#0
			case 3: case 7: case 9: case 11: case 13: case 15: case 17: case 19: case 21: case 23: case 25: case 27: case 29: case 31: case 67: case 71: case 73: case 75: case 77: case 79: case 81: case 83: case 85: case 87: case 89: case 91: case 93: case 95: 
				Execute_Unhandled("LDC,LDC2(immediate)");
				break;
			//op1==#0xxxx1 && op1!=#000x0x && coproc_special==#0 && rn_is_15==#1
			case 259: case 263: case 265: case 267: case 269: case 271: case 273: case 275: case 277: case 279: case 281: case 283: case 285: case 287: case 323: case 327: case 329: case 331: case 333: case 335: case 337: case 339: case 341: case 343: case 345: case 347: case 349: case 351: 
				Execute_Unhandled("LDC,LDC2(literal)");
				break;
			//op1==#00000x
			case 0: case 1: case 64: case 65: case 128: case 129: case 192: case 193: case 256: case 257: case 320: case 321: case 384: case 385: case 448: case 449: 
				Execute_Undefined();
				break;
			//op1==#00010x && coproc_special==#1
			case 132: case 133: case 196: case 197: case 388: case 389: case 452: case 453: 
				Execute_Unhandled("ExecuteArm_SIMD_VFP_64bit_xfer");
				break;
			//op1==#000100 && coproc_special==#0
			case 4: case 68: case 260: case 324: 
				Execute_Unhandled("MCRR,MCRR2");
				break;
			//op1==#000101 && coproc_special==#0
			case 5: case 69: case 261: case 325: 
				Execute_Unhandled("MRRC,MRRC2");
				break;
			//op1==#10xxxx && op==0 && coproc_special==#1
			case 160: case 161: case 162: case 163: case 164: case 165: case 166: case 167: case 168: case 169: case 170: case 171: case 172: case 173: case 174: case 175: case 416: case 417: case 418: case 419: case 420: case 421: case 422: case 423: case 424: case 425: case 426: case 427: case 428: case 429: case 430: case 431: 
				ExecuteArm_VFP_DataProcessing();
				break;
			//op1==#10xxxx && op==0 && coproc_special==#0
			case 32: case 33: case 34: case 35: case 36: case 37: case 38: case 39: case 40: case 41: case 42: case 43: case 44: case 45: case 46: case 47: case 288: case 289: case 290: case 291: case 292: case 293: case 294: case 295: case 296: case 297: case 298: case 299: case 300: case 301: case 302: case 303: 
				Execute_Unhandled("CDP,CDP2 on page A8-68");
				break;
			//op1==#10xxxx && op==1 && coproc_special==#1
			case 224: case 225: case 226: case 227: case 228: case 229: case 230: case 231: case 232: case 233: case 234: case 235: case 236: case 237: case 238: case 239: case 480: case 481: case 482: case 483: case 484: case 485: case 486: case 487: case 488: case 489: case 490: case 491: case 492: case 493: case 494: case 495: 
				ExecuteArm_ShortVFPTransfer();
				break;
			//op1==#10xxx0 && op==1 && coproc_special==#0
			case 96: case 98: case 100: case 102: case 104: case 106: case 108: case 110: case 352: case 354: case 356: case 358: case 360: case 362: case 364: case 366: 
				Execute_Unhandled("MCR,MCR2 on pageA8-186");
				break;
			//op1==#10xxx1 && op==1 && coproc_special==#0
			case 97: case 99: case 101: case 103: case 105: case 107: case 109: case 111: case 353: case 355: case 357: case 359: case 361: case 363: case 365: case 367: 
				Execute_MRC_MRC2_A1();
				break;
			//op1==#110000
			case 48: case 112: case 176: case 240: case 304: case 368: case 432: case 496: 
				Execute_SVC_A1();
				break;
			default: throw new InvalidOperationException("unhandled case for switch ArmSVCAndCP");
			}

			return 1;
		}

		uint ExecuteArm_VFP_DataProcessing()
		{
			//A7.5
			uint opc1 = (instruction >> 20) & 0xF;
			uint opc2 = (instruction >> 16) & 0xF;
			uint opc3 = (instruction >> 6) & 3;

			if (opc1 == _.b0000 || opc1 == _.b0100) return Execute_Unhandled("VML, VMLS (floating point)");
			if (opc1 == _.b0001 || opc1 == _.b0101) return Execute_Unhandled("VNMLA, VNMLS, VNMUL");
			if (opc1 == _.b0010 || opc1 == _.b0110)
				if (opc3 == _.b01 || opc3 == _.b11) return Execute_Unhandled("VMUL (floating point)");
				else if (opc3 == _.b00 || opc3 == _.b10) return Execute_Unhandled("VMUL (floating point)");
				else throw new InvalidOperationException("decode fail");
			if (opc1 == _.b0011 || opc1 == _.b0111)
				if (opc3 == _.b00 || opc3 == _.b10) return Execute_VADD_floating_point_A2();
				else if (opc3 == _.b01 || opc3 == _.b11) return Execute_Unhandled("VSUB (floating point)");
			if (opc1 == _.b1000 || opc1 == _.b1100)
				if (opc3 == _.b00 || opc3 == _.b10) return Execute_Unhandled("VDIV");
				else throw new InvalidOperationException("unhandled opcode space..");
			if (opc1 == _.b1011 || opc1 == _.b1111)
			{
				if (opc3 == _.b00 || opc3 == _.b10) return Execute_Unhandled("VMOV (immediate)");
				if (opc2 == _.b0000 && opc3 == _.b01) return Execute_VMOV_register_A2();
				if (opc2 == _.b0000 && opc3 == _.b11) return Execute_Unhandled("VABS");
				if (opc2 == _.b0001 && opc3 == _.b01) return Execute_Unhandled("VNEG");
				if (opc2 == _.b0001 && opc3 == _.b11) return Execute_Unhandled("VSQRT");
				if (opc2 == _.b0010 || opc2 == _.b0011) return Execute_Unhandled("VCVTB, VCVTT (between half-precision and single precision)");
				if (opc2 == _.b0100 || opc2 == _.b0101) return Execute_Unhandled("VCMP, VCMPE");
				if (opc2 == _.b0111) return Execute_Unhandled("VCVT (between double precision and single precision)");
				if (opc2 == _.b1000) return Execute_Unhandled("VCVT, VCVTR (between floating point and integer)");
				if (opc2 == _.b1010 || opc2 == _.b1011) return Execute_Unhandled("VCVT (between floating point and fixed point)");
				if (opc2 == _.b1100 || opc2 == _.b1101) return Execute_Unhandled("VCVT, VCVTR (between floating point and integer)");
				if (opc2 == _.b1110 || opc2 == _.b1111) return Execute_Unhandled("VCVT (between floating point and fixed point)");
			}
		
			throw new InvalidOperationException("decoder fail");
		}

		uint Execute_VADD_floating_point_A2()
		{
			//A8.6.272
			if (_FPSCR_LEN() != _.b000 || _FPSCR_STRIDE() != _.b00) throw new NotImplementedException("see VFP vectors");
			const bool advsimd = false;
			Bit sz = _.BIT8(instruction);
			bool dp_operation = (sz == 1);
			uint D = _.BIT22(instruction);
			uint M = _.BIT5(instruction);
			uint N = _.BIT7(instruction);
			uint Vm = Reg16(0);
			uint Vd = Reg16(12);
			uint Vn = Reg16(16);
			uint d = dp_operation ? ((D << 4) | Vd) : ((Vd << 1) | D);
			uint n = dp_operation ? ((N << 4) | Vn) : ((Vn << 1) | N);
			uint m = dp_operation ? ((M << 4) | Vm) : ((Vm << 1) | M);
			return ExecuteCore_VADD_floating_point(Encoding.A2, dp_operation, advsimd, d, n, m);
		}

		uint Execute_VMOV_register_A2()
		{
			//A8.6.327
			if (_FPSCR_LEN() != _.b000 || _FPSCR_STRIDE() != _.b00) throw new NotImplementedException("see VFP vectors");
			Bit sz = _.BIT8(instruction);
			bool single_register = (sz == 0);
			const bool advsimd = false;
			uint d,m;
			uint Vd = Reg16(12);
			uint Vm = Reg16(0);
			uint D = _.BIT22(instruction);
			uint M = _.BIT5(instruction);
			uint regs = 0;
			if (single_register)
			{
				d = (Vd << 1) | D;
				m = (Vm << 1) | M;
				
			}
			else
			{
				d = (D << 4) | Vd;
				m = (M << 4) | Vm;
				regs = 1;
			}

			return ExecuteCore_VMOV_register(Encoding.A2, single_register, advsimd, d, m, regs);
		}

		uint Execute_VMOV_immediate_A2()
		{
			////A8.6.326
			//if (_FPSCR_LEN() != _.b000 || _FPSCR_STRIDE() != _.b00) throw new NotImplementedException("see VFP vectors");
			//Bit sz = _.BIT8(instruction);
			//bool single_register = (sz == 0);
			//const bool advsimd = false;
			//uint d;
			//uint Vd = Reg16(12);
			//uint D = _.BIT22(instruction);
			//uint imm32 = 0;
			//ulong imm64 = 0;
			//uint imm4H = (instruction>>16)&0xF;
			//uint imm4L = (instruction&0xF);
			//uint regs = 0;
			//if (single_register)
			//{
			//    d = (Vd << 1) | D;
			//    imm32 = _VFPExpandImm32((imm4H << 4) | imm4L);
			//}
			//else
			//{
			//    d = (D << 4) | Vd;
			//    imm64 = _VFPExpandImm64((imm4H << 4) | imm4L);
			//    regs = 1;
			//}

			//return ExecuteCore_VMOV(Encoding.A2, single_register, advsimd, d, regs, imm32, imm64);

			return 1;
		}

		uint Execute_ExtensionRegister_LoadStore()
		{
			//A7.6 Extension register load/store instructions
			uint opcode = (instruction >> 20) & 0x1F;
			uint n = Reg16(16);
			bool bit8 = _.BIT8(instruction)==1;
			switch (opcode)
			{
				case _.b00100: case _.b00101: return Execute_Unhandled("64-bit transfers between ARM core and extension registers");
				case _.b01000: case _.b01100: return Execute_Unhandled("VSTM");
				case _.b01010: case _.b01110: return Execute_Unhandled("VSTM");
				case _.b10000: case _.b10100: case _.b11000: case _.b11100: return Execute_Unhandled("VSTR");
				case _.b10010: case _.b10110:
					if (n != _.b1101) return Execute_Unhandled("VSTM");
					else return bit8?Execute_VPUSH_A1():Execute_VPUSH_A2();
				case _.b01001: case _.b01101: return Execute_Unhandled("VLDM");
				case _.b01011: case _.b01111:
					if (n != _.b1101) return Execute_Unhandled("VLDM");
					else return Execute_Unhandled("VPOP");
				case _.b10001: case _.b10101: case _.b11001: case _.b11101:
					return bit8 ? Execute_VLDR_A1() : Execute_VLDR_A2();
				case _.b10011: case _.b10111: return Execute_Unhandled("VLDM");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_VLDR_A1()
		{
			//A8.6.320
			throw new NotSupportedException("Execute_VLDR_A1");
		}

		uint Execute_VLDR_A2()
		{
			//A8.6.320
			const bool single_reg = true;
			Bit U = _.BIT23(instruction);
			Bit D = _.BIT22(instruction);
			bool add = (U == 1);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint Vd = Reg16(12);
			uint n = Reg16(16);
			uint d = (Vd << 1) | D;
			return ExecuteCore_VLDR(Encoding.A1, single_reg, add, d, n, imm32);
		}

		uint Execute_VPUSH_A1()
		{
			//A8.6.355
			const bool single_regs = false;
			Bit D = _.BIT22(instruction);
			uint d = ((uint)D << 4) | Reg16(12);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint regs = imm8 / 2;
			Debug.Assert(!((imm8&1)==1),"see FSTMX");
			if(regs==0 || regs>16 || (d+regs)>32) _FlagUnpredictable();
			return ExecuteCore_VPUSH(Encoding.A1, single_regs, d, regs, imm32);

		}

		uint Execute_VPUSH_A2()
		{
			//A8.6.355
			const bool single_regs = true;
			Bit D = _.BIT22(instruction);
			uint d = (Reg16(12)<<1)|D;
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint regs = imm8;
			if (regs == 0 || regs > 16 || (d + regs) > 32) _FlagUnpredictable();
			return ExecuteCore_VPUSH(Encoding.A2, single_regs, d, regs, imm32);
		}

		uint Execute_MRC_MRC2_A1()
		{
			//ignoring admonition to see "advanced SIMD and VFP" which has been handled by decode earlier
			//TODO - but i should assert anyway
			uint t = Reg16(12);
			uint cp = Reg16(8);
			uint opc1 = Reg8(21);
			uint crn = Reg16(16);
			uint opc2 = Reg8(5);
			uint crm = Reg16(0);
			if (t == 13 && _CurrentInstrSet() != EInstrSet.ARM) unpredictable = true;
			return ExecuteCore_MRC_MRC2(Encoding.A1, cp, opc1, t, crn, crm, opc2);
		}

		uint ExecuteArm_ShortVFPTransfer()
		{
			//A7.8
			uint A = (instruction >> 21) & _.b111;
			uint L = _.BIT20(instruction);
			uint C = _.BIT8(instruction);
			uint B = (instruction >> 5) & _.b11;

			//Arm_ShortVFPTransfer
			switch((A<<0)|(L<<3)|(C<<4)|(B<<5)) {
			//L==0 && C==0 && A==#000
			//L==1 && C==0 && A==#000
			case 0: case 8: case 32: case 40: case 64: case 72: case 96: case 104: 
				Execute_Unhandled("VMOV (between ARM core register and single-precision register) on page A8-648");
				break;
			//L==0 && C==0 && A==#111
			case 7: case 39: case 71: case 103: 
				Execute_VMSR_A1();
				break;
			//L==0 && C==1 && A==#0xx
			case 16: case 17: case 18: case 19: case 48: case 49: case 50: case 51: case 80: case 81: case 82: case 83: case 112: case 113: case 114: case 115: 
				Execute_Unhandled("VMOV (ARM core register to scalar) on page A8-644");
				break;
			//L==0 && C==1 && A==#1xx && B==#0x
			case 20: case 21: case 22: case 23: case 52: case 53: case 54: case 55: 
				Execute_Unhandled("VDUP (ARM core register) on page A8-594");
				break;
			//L==1 && C==0 && A==#111
			case 15: case 47: case 79: case 111: 
				Execute_Unhandled("VMRS on page A8-658 or page B6-27");
				break;
			//L==1 && C==1
			case 24: case 25: case 26: case 27: case 28: case 29: case 30: case 31: case 56: case 57: case 58: case 59: case 60: case 61: case 62: case 63: case 88: case 89: case 90: case 91: case 92: case 93: case 94: case 95: case 120: case 121: case 122: case 123: case 124: case 125: case 126: case 127: 
				Execute_Unhandled("VMOV (scalar to ARM core register) on page A8-646");
				break;
			default: throw new InvalidOperationException("unhandled case for switch Arm_ShortVFPTransfer");
			}

			return 1;
		}

		//TODO - move disasm to executecore
		uint Execute_VMSR_A1()
		{
			uint t = Reg16(12);
			if (t == 15 || (t == 13 && _CurrentInstrSet() != EInstrSet.ARM)) return _UNPREDICTABLE();

			if (disassemble)
				if (nstyle)
					return DISNEW("FMXR<c>", "<fpscr>,<Rt>", t);
				else
					return DISNEW("VMSR<c>", "<fpscr>, <Rt>", t);

			return ExecuteCore_VMSR(t);
		}

		uint Execute_SVC_A1()
		{
			//A8.6.218
			//A8-430
			uint imm24 = instruction & 0xFFFFFF;
			uint imm32 = imm24;

			return ExecuteCore_SVC(Encoding.A1, imm32);
		}


	} //class ARM11
}