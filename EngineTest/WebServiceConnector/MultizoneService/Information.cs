using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Information
    {

        #region Fenster
        //public const String zone_unit = null;
        //public const String zone_description = "Bezeichner der Zone, der das Fenster zugeordnet ist";
            
        private const String breite_unit = "m";
        private const String breite_description = "Breite des Fensters";

        private const String hoehe_unit = "m";
        private const String hoehe_description = "Höhe des Fensters";

        private const String u_unit = "W/m²K";
        private const String u_description = "Effektiver U-Wert";

        private const String g_unit = null;
        private const String g_description = "Gesamtenergiedurchlassgrad";

        private const String svf_unit = null;
        private const String svf_description = "Sichtfaktor (bezogen auf den Himmel)";

        #endregion

        #region Last

        private const String zeit_unit = "h";
        private const String zeit_description = "Einwirkungszeitpunkt";

        private const String aussentemp_unit = "°C";
        private const String aussentemp_description = "Außenlufttemperatur";

        private const String strahlung_unit = "W";
        private const String strahlung_description = "Strahlungseinwirkung auf eine spezielle Zone";

        private const String strahlung2_unit = "W";
        private const String strahlung2_description = "Strahlungseinwirkung auf eine spezielle Zone bei Verschattung";

        private const String temperatur_unit = "°C";
        private const String temperatur_description = "Äquivalente Temperatur an einer Schicht";

        private const String raumlueftung_unit = "m³/h";
        private const String raumlueftung_description = "Mechanisch herbeigeführter Luftwechsel in einer Zone";

        private const String infiltration_unit = "m³/h";
        private const String infiltration_description = "Infiltration in einer Zone";

        private const String ilastenGeraete_unit = "W";
        private const String ilastenGeraete_description = "Innere Lasten durch Geräte in der Zone";

        private const String ilastenPersonen_unit = "W";
        private const String ilastenPersonen_description = "Innere Lasten durch Personen in der Zone";

        private const String lueftung_unit = null;
        private const String lueftung_description = "Fensteröffnungen 0: geschlossen 1:geöffnet";

        private const String warmwasserverbrauch_unit = "l";
        private const String warmwasserverbrauch_description = "Warmwasserverbrauch pro Speicher";
                    
        #endregion

        #region Zone
        private const String zone_unit = null;
        private const String zone_description = "Eindeutiger Bezeichner einer Zone";

        private const String grundflaeche_unit = "m²";
        private const String grundflaeche_description = "Grundfläche";

        private const String forceIdeal_unit = null;
        private const String forceIdeal_description = "Parameter, um einen konstanten Lufttemperaturverlauf zu erzwingen";

        private const String heizung_unit = null;
        private const String heizung_description = "Parameter, um die Heizung ein- oder auszuschalten";

        private const String heizTemp_unit = "°C";
        private const String heizTemp_description = "Temperatur bei deren Unterschreitung geheizt werden soll";

        private const String heizungIdeal_unit = null;
        private const String heizungIdeal_description = "Parameter, um festzulegen, ob die Temperatur nicht unter die Heiztemperatur fallen, aber sehr wohl darüber steigen darf";

        private const String heizlastKonstant_unit = "W";
        private const String heizlastKonstant_description = "Konstante Heizlast, wenn die Heizung eingeschaltet ist";

        private const String kuehlung_unit = null;
        private const String kuehlung_description = "Parameter, um die Kühlung ein- oder auszuschalten";

        private const String kuehlTemp_unit = "°C";
        private const String kuehlTemp_description = "Temperatur bei deren Überschreitung gekühlt werden soll";

        private const String kuehlungIdeal_unit = null;
        private const String kuehlungIdeal_description = "Parameter, um festzulegen, ob die Temperatur nicht über die Kühltemperatur steigen, aber sehr wohl darunter fallen darf";

        private const String kuehllastKonstant_unit = "W";
        private const String kuehllastKonstant_description = "Konstante Kühllast, wenn die Kühlung eingeschaltet ist";

        private const String fensterAufTemp_unit = "°C";
        private const String fensterAufTemp_description = "Lufttemperatur der Zone, ab der die Fenster aufgehen sollen, wenn die Luftaußentemperatur unter der Lufttemperatur der Zone liegt";

        private const String startTemp_unit = "°C";
        private const String startTemp_description = "Temperatur der Luft und sämtlicher Bauteile zu Beginn der Simulation";

        private const String kapazitaetEinrichtung_unit = "J/K";
        private const String kapazitaetEinrichtung_description = "Wirksame Wärmespeicherkapazität der Einrichtung der Zone";

        #endregion

        #region Wand
        private const String name_id_unit = null;
        private const String name_id_description = "Eindeutiger Bezeichner der Schicht";

        private const String seite1_id_unit = null;
        private const String seite1_id_description = "Eindeutiger Bezeichner der einen angrenzenden Schicht bzw. Zone; e für Außen";

        private const String seite2_id_unit = null;
        private const String seite2_id_description = "Eindeutiger Bezeichner der anderen angrenzenden Schicht bzw. Zone; e für Außen";

        private const String flaeche_unit = "m²";
        private const String flaeche_description = "Fläche der Schicht; nur für bauteilaktivierte Schichten relevant";

        private const String waermekapazitaet_unit = "J/(m²K)";
        private const String waermekapazitaet_description = "Massenspezifische Wärmespeicherkapazität";

        private const String dicke_unit = "m";
        private const String dicke_description = "Dicke der Schicht";

        private const String anzahlElemente_unit = null;
        private const String anzahlElemente_description = "Anzahl der Elemente, in die eine Schicht geteilt werden soll";

        private const String waermeleitzahl_unit = "W/mK";
        private const String waermeleitzahl_description = "Wärmeleitzahl der Schicht";

        private const String dichte_unit="kg/m³";
        private const String dichte_description="Dichte der Schicht";

        private const String speicher_id_unit = null;
        private const String speicher_id_description = "Eindeutiger Bezeichner des Speichers; nur bei Bauteilaktivierung relevant";

        private const String zone_id_unit = null;
        private const String zone_id_description = "Eindeutiger Bezeichner der Zone; nur bei Bauteilaktivierung relevant";

        private const String isBoden_unit=null;
        private const String isBoden_description="Handelt es sich um eine Bodenschicht?";

        private const String isDecke_unit=null;
        private const String isDecke_description="Handelt es sich um eine Deckenschicht?";

        #endregion
        #region params
        private const String diffTsky_unit = "K";
        private const String diffTsky_description = "Differenz zwischen Himmels- und Lufttemperatur";

        private const String uebergangskoeffizientAußen_unit = "W/m²K";
        private const String uebergangskoeffizientAußen_description = "Kombinierter Übergangskoeffizient Außen";

        private const String uebergangskoeffizientBoden_unit = "W/m²K";
        private const String uebergangskoeffizientBoden_description = "Kombinierter Übergangskoeffizient Boden";

        private const String uebergangskoeffizientInnen_unit = "W/m²K";
        private const String uebergangskoeffizientInnen_description = "Konvektiver Übergangskoeffizient Innen (horizontal)";

        private const String uebergangskoeffizientInnenUp_unit = "W/m²K";
        private const String uebergangskoeffizientInnenUp_description = "Konvektiver Übergangskoeffizient Innen (von unten nach oben)";

        private const String uebergangskoeffizientInnenDown_unit = "W/m²K";
        private const String uebergangskoeffizientInnenDown_description = "Konvektiver Übergangskoeffizient Innen (von oben nach unten)";

        private const String uebergangskoeffizientInnenStrahlung_unit = "W/m²K";
        private const String uebergangskoeffizientInnenStrahlung_description = "Strahlungsübergangskoeffizient innen";

        private const String konvektiverAnteilSolar_unit = null;
        private const String konvektiverAnteilSolar_description = "Konvektiver Anteil solarer Einträge";

        private const String konvektiverAnteilGeraete_unit = null;
        private const String konvektiverAnteilGeraete_description = "Konvektiver Anteil von Einträgen von Geräten";

        private const String konvektiverAnteilPersonen_unit = null;
        private const String konvektiverAnteilPersonen_description = "Konvektiver Anteil von Einträgen von Personen";

        private const String konvektiverAnteilHeizung_unit = null;
        private const String konvektiverAnteilHeizung_description = "Konvektiver Anteil von Einträgen durch das Heizsystem";
            
        private const String temperaturkorrekturfaktor_unit = null;
        private const String temperaturkorrekturfaktor_description = "Temperaturkorrekturfaktor bodenberührter Bauteile";

        private const String absorptionsgradStrahlung_unit = null;
        private const String absorptionsgradStrahlung_description = "Absorptionsgrad für Strahlung (nur relevant, wenn Klimadaten verarbeitet werden)";

        private const String waermespeicherkapazitaetWasser_unit = "J/kgK";
        private const String waermespeicherkapazitaetWasser_description = "Wärmespeicherkapazität Wasser";

        private const String waermerueckgewinnungsgrad_unit = null;
        private const String waermerueckgewinnungsgrad_description = "Wärmerückgewinnungsgrad";

        private const String bodenreflexionsgrad_unit = null;
        private const String bodenreflexionsgrad_description = "Bodenreflexionsgrad (nur relevant, wenn Klimadaten verarbeitet werden)";

        private const String temperaturdifferenz_unit = "K";
        private const String temperaturdifferenz_description = "Temperaturdifferenz zwischen Einschalt- und Ausschaltpunkt der Heiz- bzw. Kühlsysteme (z.B. 2 Einschlaten der Heizun bei Heizpunkt (z.B. 20°C), Ausschalten bei 22°C)";

        private const String anzahlStunden_unit = "h";
        private const String anzahlStunden_description = "Anzahl der simulierenden Stunden";

        /*
        private const String ausgabeintervall_unit = "s";
        private const String ausgabeintervall_description = "Ausgabeintervall in Sekunden";

        
        private const String status_unit = null;
        private const String status_description = "Information über den Zustand des Servers";

        private const String fortschritt_unit = null;
        private const String fortschritt_description = "Information über den Fortschritt der Simulation";

        private const String klimafile_unit = null;
        private const String klimafile_description = "Verwendung eines Klimafiles?";

        private const String fensterlueftung_unit = null;
        private const String fensterlueftung_description = "Fenster öffnen unabhängig von der Außentemperatur?";

        private const String sommerLueftung_unit = null;
        private const String sommerLueftung_description = "Fenster öffnen, wenn die Außentemperatur unter der Innentemperatur, die Innentemperatur über der eingestellten Temperatur der Zone liegt und der Lüftungswert aktiviert ist";

        private const String genauigkeit_unit = null;
        private const String genauigkeit_description = "Genauigkeit des Solvers";

        private const String schrittweite_unit = "s";
        private const String schrittweite_description = "maximale Schrittweite des Solvers in Sekunden";

        private const String solver_unit = null;
        private const String solver_description = "Typ des Solvers der verwendet werden soll (Standard: Isoda)";
        */
        #endregion

        #region Speicher
        /*
         *             String name_id, double kapazitaet, double heizlast, double heiztemperatur, double tempKonstant, String lage, int anzahlSchichten,
double hoehe, double grundflaeche, double effektiveWaermeleitfaehigkeit, double vorlaufTemperatur, double temperaturOben, double temperaturUnten,
double temperaturKaltwasser, double warmwasserbedarf, double fbw, double parameterA, double parameterB, double parameterC, double parameterD,
double parameterBa, double parameterZa, double puffervolumen, double useUaPuffer, double waermeverluste, double qrho1, double l1, String z1_id, double fero1, double qrho2,
double l2, String z2_id, double fero2, double qrho3, double l3, String z3_id, double fero3, double da, double dr, double vf, double lf, double lb, double lr, double rhof,
double dx, double l, double mp, double neigung, double ausrichtung, double fRegelung, double dTSolar, double fWaermetauscher, double f0, double aKoll, double fs, double kd, double kb50, 
double c1, double c2, double tempGrenzKollektor, double tempGrenzPuffer, double mpKoll, double qrho1Koll, double l1Koll, double z1Koll, double fero1Koll, double qrho2Koll, double l2Koll,
double z2Koll, double fero2Koll, double qrho3Koll, double l3Koll, double z3Koll, double fero3Koll

         * 
         * Name.Name = "name";
            Name.Unit = null;
            Name.Value = name;

            Kap.Name = "kap";
            Kap.Unit = "J/K";
            Kap.Value = kap;

            StartTemp.Name = "startTemp";
            StartTemp.Unit = "°C";
            StartTemp.Value = startTemp;

            Heizung.Name = "heizung";
            Heizung.Unit = null;
            Heizung.Value = heizung;

            HeizTemp.Name = "heizTemp";
            HeizTemp.Unit = "°C";
            HeizTemp.Value = heizTemp;

            HeizlastKonstant.Name = "heizlastKonstant";
            HeizlastKonstant.Unit = "W";
            HeizlastKonstant.Value = heizlastKonstant;

            TempKonstant.Name = "tempKonstant";
            TempKonstant.Unit = null;
            TempKonstant.Value = tempKonstant;

            VorlaufTemp.Name = "vorlaufTemp";
            VorlaufTemp.Unit = "°C";
            VorlaufTemp.Value = vorlaufTemp;

            Lage.Name = "lage";
            Lage.Unit = null;
            Lage.Value = lage;

            UseUaPuffer.Name = "useUaPuffer";
            UseUaPuffer.Unit = null;
            UseUaPuffer.Value = useUaPuffer;

            UaPuffer.Name = "uaPuffer";
            UaPuffer.Unit = "W/K";
            UaPuffer.Value = uaPuffer;

            Qrho1.Name = "qrho1";
            Qrho1.Unit = "W/(mK)";
            Qrho1.Value = qrho1;

            L1.Name = "l1";
            L1.Unit = "m";
            L1.Value = l1;

            Z1.Name = "z1";
            Z1.Unit = null;
            Z1.Value = z1;

            Fero1.Name = "fero1";
            Fero1.Unit = null;
            Fero1.Value = fero1;

            Qrho2.Name = "qrho2";
            Qrho2.Unit = "W/(mK)";
            Qrho2.Value = qrho2;

            L2.Name = "l2";
            L2.Unit = "m";
            L2.Value = l2;

            Z2.Name = "z2";
            Z2.Unit = null;
            Z2.Value = z2;

            Fero2.Name = "fero2";
            Fero2.Unit = null;
            Fero2.Value = fero2;

            Qrho3.Name = "qrho3";
            Qrho3.Unit = "W/(mK)";
            Qrho3.Value = qrho3;

            L3.Name = "l3";
            L3.Unit = "m";
            L3.Value = l3;

            Z3.Name = "z3";
            Z3.Unit = null;
            Z3.Value = z3;

            Fero3.Name = "fero3";
            Fero3.Unit = null;
            Fero3.Value = fero3;

            Qrho1Rueck.Name = "qrho1Rueck";
            Qrho1Rueck.Unit = "W/(mK)";
            Qrho1Rueck.Value = qrho1Rueck;

            L1Rueck.Name = "l1Rueck";
            L1Rueck.Unit = "m";
            L1Rueck.Value = l1Rueck;

            Z1Rueck.Name = "z1Rueck";
            Z1Rueck.Unit = null;
            Z1Rueck.Value = z1Rueck;

            Fero1Rueck.Name = "fero1Rueck";
            Fero1Rueck.Unit = null;
            Fero1Rueck.Value = fero1Rueck;

            Qrho2Rueck.Name = "qrho2Rueck";
            Qrho2Rueck.Unit = "W/(mK)";
            Qrho2Rueck.Value = qrho2Rueck;

            L2Rueck.Name = "l2Rueck";
            L2Rueck.Unit = "m";
            L2Rueck.Value = l2Rueck;

            Z2Rueck.Name = "z2Rueck";
            Z2Rueck.Unit = null;
            Z2Rueck.Value = z2Rueck;

            Fero2Rueck.Name = "fero2Rueck";
            Fero2Rueck.Unit = null;
            Fero2Rueck.Value = fero2Rueck;

            Qrho3Rueck.Name = "qrho3Rueck";
            Qrho3Rueck.Unit = "W/(mK)";
            Qrho3Rueck.Value = qrho3Rueck;

            L3Rueck.Name = "l3Rueck";
            L3Rueck.Unit = "m";
            L3Rueck.Value = l3Rueck;

            Z3Rueck.Name = "z3Rueck";
            Z3Rueck.Unit = null;
            Z3Rueck.Value = z3Rueck;

            Fero3Rueck.Name = "fero3Rueck";
            Fero3Rueck.Unit = null;
            Fero3Rueck.Value = fero3Rueck;

            Da.Name = "da";
            Da.Unit = "m";
            Da.Value = da;

            Dr.Name = "dr";
            Dr.Unit = "m";
            Dr.Value = dr;

            Vf.Name = "vf";
            Vf.Unit = "mm²/s";
            Vf.Value = vf;

            Lf.Name = "lf";
            Lf.Unit = "W/(mK)";
            Lf.Value = lf;

            Lb.Name = "lb";
            Lb.Unit = "W/(mK)";
            Lb.Value = lb;

            Lr.Name = "lr";
            Lr.Unit = "W/(mK)";
            Lr.Value = lr;

            Rhof.Name = "rhof";
            Rhof.Unit = "kg/m³";
            Rhof.Value = rhof;

            Dx.Name = "dx";
            Dx.Unit = "m";
            Dx.Value = dx;

            L.Name = "l";
            L.Unit = "m";
            L.Value = l;

            Mp.Name = "mp";
            Mp.Unit = "kg/s";
            Mp.Value = mp;

            Print.Name = "print";
            Print.Unit = null;
            Print.Value = print;

         * */
        #endregion

    }
}
