# LEDController
That was the subject of my MSc diploma thesis as an electrical engineer.

This application searches for a PIC32 participant on Ethernet with UDP broadcast messages which is prepared to control a power LED brightness,
number of turns of the cooling fan, and to measure temperature of the heatsink.
After searching it connects to the PIC32 trought TPC connection to give the setpoints and to get actual values.
