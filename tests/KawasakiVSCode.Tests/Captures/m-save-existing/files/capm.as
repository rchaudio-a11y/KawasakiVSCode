.***************************************************************************
.*=== AS GROUP ===         : ASE_010300Z5P 2022/09/05 14:24
.*USER IF AS               : UASE010300Z5P 2022/09/05 14:24
.*USER IF TP               : UTPE010300Z5P 2022/09/05 14:09
.*ARM CONTROL AS           : AASE010300Z5P 2022/09/05 14:50
.*USER IF AS MESSAGE FILE  : MASE0100Z5PEN 2022/09/05 13:58
.*USER IF TP MESSAGE FILE  : MTPE0100Z5PEN 2022/09/05 13:47
.*ARM DATA FILE            : ARME010300Z5P 2022/09/05 14:09
.*=== SERVO GROUP ===      : 
.*ARM CONTROL SERVO        : ASVE080000069 2020/08/28 14:43
.*SRV DATA FILE            : 
.*ARM CONTROL SERVO FPGA   : ASFE08000000A 2015/04/01 09:29
.*   [Shipment setting data] 
.*	default.as
.***************************************************************************
.NETCONF     0.0.0.0,"",0.0.0.0,0.0.0.0,0.0.0.0,0.0.0.0,""
.NETCONF2     0.0.0.0,0.0.0.0,0.0.0.0
.PROGRAM capb() #0
; khi capture (i): the same program name as capture (b), different content
  TYPE "capture i"
.END
