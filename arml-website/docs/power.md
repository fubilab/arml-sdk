# Powering the AR Magic Lantern

The ARML can be powered by a lithium ion battery pack (see [specifications](#battery-specifications)) or with an external power supply that has a USB Type-C connector and is compatible with the USB PD (Power Delivery) standard.

## Using USB-C power

When an appropirate USB-C PD power supply is connected to the back of the lantern, it will provide power all components of the system except the projector. Therefore, a common use case for powering it this way is when there is also a monitor, keyboard and mouse attached to the device, by way of a USB hub (see [Building and Running](workflow.md#4-copy-application-files-to-arml-device)).

The USB-C PD power supply should support 12V, and should be rated to deliver at least 45W of power (recommended rating is 60W or higher). Adapters designed for laptops that support a USB-C PD power supply will probably work. [Example on Amazon](https://www.amazon.es/dp/B0D48H3TR4)

## Using a 12V power supply

You can power the ARML system, including the projector, by substituting the battery for an appropriate 12V power supply. The power supply should support at least 5A of current. You will need to adapt the plug of the power supply (the standard is 5.5mm barrel jack) to the Tamiya connector in the battery compartment of the ARML.

![](images/ARML-external-power.jpg)
*Photo showing an external power supply connected to the ARML using an adaptor from 5.5mm barrel jack to Tamiya connector*

## Battery specifications

The battery used in the AR Magic lantern is a custom fabrication that includes a built in BMS (Battery Management System) to prevent over-charging and over-discharging, as well as simple fuel gauge mounted to the front of the pack.

![](images/battery-overview.jpg)
*Photo of ARML battery pack, showing the Tamiya plug that connects it to the ARML.*

![](images/battery-gauge.jpg)
*Close-up photo of the front of the ARML battery pack, showing the fuel gauge after the "TEST" button has been pressed*

| Attribute                 | Value                 |
|---------------------------|-----------------------|
| Dimensions                | 135x75x25mm           |
| Weight                    | 300g                  |
| Nominal capacity          | 10Ah                  | 
| Nominal voltage           | 12.6V                 |
| Quantity of cells         | 6 cells               |
| Cell specification        | 3.7V 5Ah 21700        |
| Cell combination          | 1 series / 3 parallel | 
| Cell size                 | 140x74x25 mm          |
| Discharge speed           | 1C                    |
| BMS current rating        | 40A                   |
| Discharge cut-off voltage | 8.4V                  |
| Charging cut-off voltage  | 12.6V                 |
| Nominal current rating    | 20A                   |
| Peak current rating       | 20A                   |
| Continuous current rating | 30A                   |
| Max charge current        | 5A                    |
| Charging mode             | CC/CV                 |
| Power rating              | 120W                  |
| Charge time (2A)          | 12 hours              |
| Rapid charge time (5A)    | 3 hours               |
| Lifespan                  | Up to 800 cycles      |
| Temperature range         | -20 to 60 deg C       |